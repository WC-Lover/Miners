using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;

public class GameMultiplayerManager : NetworkBehaviour
{
    public const int MAX_PLAYER_AMOUNT = 8;
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerName";

    public static GameMultiplayerManager Instance { get; private set; }

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler<OnPlayerDataNetworkListChangedArgs> OnPlayerDataNetworkListChanged;
    public class OnPlayerDataNetworkListChangedArgs : EventArgs
    {
        public List<PlayerData> playerDataList;
    }

    private NetworkList<PlayerData> playerDataNetworkList;
    private string playerName;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(0, 100));

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        List<PlayerData> playerDataList = new List<PlayerData>();
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            playerDataList.Add(playerDataNetworkList[i]);
        }

        OnPlayerDataNetworkListChanged?.Invoke(this, new OnPlayerDataNetworkListChangedArgs
        {
            playerDataList = playerDataList
        });
    }

    public NetworkList<PlayerData> GetPlayerDataNetworkList()
    {
        return playerDataNetworkList;
    }

    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Server_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                // Disconnected
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    private void NetworkManager_Server_OnClientConnectedCallback(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            playerDataNetworkList.Add(new PlayerData
            {
                clientId = clientId,
                playerName = GetPlayerName(),
                playerId = AuthenticationService.Instance.PlayerId,
                playerReady = false
            });
        }
        else 
        {
            playerDataNetworkList.Add(new PlayerData
            {
                clientId = clientId,
                playerReady = false
            });
        }
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (SceneManager.GetActiveScene().name != Loader.Scene.LobbyScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = true;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = true;

    }

    public void StartClient()
    {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        if (NetworkManager.Singleton.IsConnectedClient)
        {
            // If already connected, attempt to reset or clean up the NetworkManager
            NetworkManager.Singleton.Shutdown();
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == serverRpcParams.Receive.SenderClientId)
            {
                playerData.playerName = playerName;
                playerDataNetworkList[i] = playerData;
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == serverRpcParams.Receive.SenderClientId)
            {
                playerData.playerId = playerId;
                playerDataNetworkList[i] = playerData;
                break;
            }
        }
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Loader.Load(Loader.Scene.MainMenuScene);
        }
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        //NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }
}
