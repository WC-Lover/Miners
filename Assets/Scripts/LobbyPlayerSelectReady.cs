using System.Collections.Generic;
using Unity.Netcode;
using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPlayerSelectReady : NetworkBehaviour
{

    public EventHandler<OnReadyChangedArgs> OnReadyChanged;
    public class OnReadyChangedArgs : EventArgs
    {
        public ulong clientId;
    }
    public static LobbyPlayerSelectReady Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ChangePlayerReady()
    {
        ChangePlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        NetworkList<PlayerData> playersData = GameMultiplayerManager.Instance.GetPlayerDataNetworkList();

        for (int i = 0; i < playersData.Count; i++)
        {
            if (playersData[i].clientId == serverRpcParams.Receive.SenderClientId)
            {
                PlayerData playerToChangeReady = playersData[i];
                playerToChangeReady.playerReady = !playerToChangeReady.playerReady;
                playersData[i] = playerToChangeReady;
            }
        }
    }



}
