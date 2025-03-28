using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public NetworkVariable<float> bonusSelectTimer = new NetworkVariable<float>();
    private NetworkList<PlayerHolyResourceData> playerHolyResourceDataNetworkList;

    [SerializeField] private GameObject playerBuildingPrefab;
    [SerializeField] private GameObject neutralBuildingPrefab;
    [SerializeField] private GameObject resourceSpawnerPrefab;

    private Vector3[] buildingSpawnPos;
    private Vector3[] buildingUnitSpawnPos;
    private Vector3[] neutralBuildingSpawnPos;
    private Vector3[] neutralBuildingUnitSpawnPos;
    private Vector3[] cameraPositions;

    private static int counter;

    public EventHandler<OnGameFinishedEventArgs> OnGameFinished;
    public class OnGameFinishedEventArgs: EventArgs
    {
        public List<PlayerHolyResourceData> playerHolyResourceDataList;
    }
    public Action OnGameReady;
    public EventHandler<OnPlayerHolyResourceDataListChangedArgs> OnPlayerHolyResourceDataListChanged;
    public class OnPlayerHolyResourceDataListChangedArgs : EventArgs
    {
        public List<PlayerHolyResourceData> playerHolyResourceDataList;
    }

    private void Awake()
    {
        playerHolyResourceDataNetworkList = new NetworkList<PlayerHolyResourceData>();
    }

    private void PlayerHolyResourceDataList_OnListChanged(NetworkListEvent<PlayerHolyResourceData> changeEvent)
    {
        List<PlayerHolyResourceData> playerHolyResourceDataList = new List<PlayerHolyResourceData>();
        for (int i = 0; i < playerHolyResourceDataNetworkList.Count; i++)
        {
            playerHolyResourceDataList.Add(playerHolyResourceDataNetworkList[i]);
        }

        OnPlayerHolyResourceDataListChanged?.Invoke(this, new OnPlayerHolyResourceDataListChangedArgs
        {
            playerHolyResourceDataList = playerHolyResourceDataList
        });
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwnedByServer)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (IsServer)
        {
            NetworkObject.Despawn(true);
            return;
        }

        playerHolyResourceDataNetworkList.OnListChanged += PlayerHolyResourceDataList_OnListChanged;

        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

        counter = 0;

        cameraPositions = new Vector3[]
        {
            new Vector3(0, 9f, -7.5f), new Vector3(-7.5f, 9f, 0), new Vector3(0, 9f, 7.5f), new Vector3(7.5f, 9f, 0)
        };

        if (!IsServer) return;

        buildingSpawnPos = new Vector3[4]
        {
            new Vector3(-4.5f, 0, -4.5f), new Vector3(-4.5f, 0, 4.5f), new Vector3(4.5f, 0, 4.5f), new Vector3(4.5f, 0, -4.5f)
        };

        buildingUnitSpawnPos = new Vector3[4]
        {
            new Vector3(-3.75f, 0, -3.75f), new Vector3(-3.75f, 0, 3.75f), new Vector3(3.75f, 0, 3.75f), new Vector3(3.75f, 0, -3.75f)
        };

        neutralBuildingSpawnPos = new Vector3[4]
        {
            new Vector3(0, 0, 4.5f), new Vector3(4.5f, 0, 0), new Vector3(0, 0, -4.5f), new Vector3(-4.5f, 0, 0)
        };

        neutralBuildingUnitSpawnPos = new Vector3[4]
        {
            new Vector3(0, 0, 3.75f), new Vector3(3.75f, 0, 0), new Vector3(0, 0, -3.75f), new Vector3(-3.75f, 0, 0)
        };
    }

    private void SceneManager_activeSceneChanged(Scene prev, Scene next)
    {
        if (next.name == Loader.Scene.GameScene.ToString())
        {
            // As new player connects to the scene, spawn base for it
            if (IsServer)
            {
                AttachNeutralBuildingsOnGameStart();
                CreateBuildingForConnectedPlayer(NetworkManager.Singleton.LocalClientId);
                SpawnResourceSpawner();
            }
            else
            {
                CreateBuildingForConnectedPlayerServerRpc();
            }
        }
    }

    private void FillPlayerHolyResourceDataNetworkList()
    {
        foreach (PlayerData playerData in GameMultiplayerManager.Instance.GetPlayerDataNetworkList())
        {
            playerHolyResourceDataNetworkList.Add(new PlayerHolyResourceData
            {
                clientId = playerData.clientId,
                playerName = playerData.playerName,
                holyResourceGathered = 0
            });
        }
    }

    private void SpawnResourceSpawner()
    {
        Instantiate(resourceSpawnerPrefab);
    }

    private void AttachNeutralBuildingsOnGameStart()
    {
        for (int i = 0; i < neutralBuildingSpawnPos.Length; i++)
        {
            // Create neutral building
            GameObject neutralBuilding = Instantiate(neutralBuildingPrefab, neutralBuildingSpawnPos[i], Quaternion.identity);
            neutralBuilding.SetActive(true);

            // Create Units for Object Pooling
            Building building = neutralBuilding.GetComponent<Building>();
            building.PreCreateUnits(neutralBuildingUnitSpawnPos[i]);

            building.NetworkObject.Spawn();

            // Set material and building position for units to return back
            building.SetBuildingOwnerRpc(neutralBuildingSpawnPos[i]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreateBuildingForConnectedPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        CreateBuildingForConnectedPlayer(serverRpcParams.Receive.SenderClientId);
    }

    private void CreateBuildingForConnectedPlayer(ulong clientId)
    {
        // Set camera so each players' building is in left bottom corner
        SetCameraEveryoneRpc(counter, clientId);

        // Create Building for connected player
        GameObject playerBuilding = Instantiate(playerBuildingPrefab, buildingSpawnPos[counter], Quaternion.identity);
        playerBuilding.SetActive(true);

        // Create Units for Object Pooling
        Building building = playerBuilding.GetComponent<Building>();
        building.PreCreateUnits(buildingUnitSpawnPos[counter]);

        // Set ownership
        building.NetworkObject.SpawnWithOwnership(clientId);

        // Set building position for units to return back
        building.SetBuildingOwnerRpc(buildingSpawnPos[counter]);

        counter++;

        if (counter == NetworkManager.Singleton.ConnectedClients.Count)
        {
            // GameLoadingUI needs time to subscribe.
            FillPlayerHolyResourceDataNetworkList();
            StartCoroutine(GameStartDelay());
        }
    }

    IEnumerator GameStartDelay()
    {
        yield return new WaitForSeconds(1.5f);
        while (OnGameReady == null)
        {
            yield return new WaitForSeconds(1f);
        }
        GameReadyEveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void SetCameraEveryoneRpc(int index, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        Camera.main.transform.position = cameraPositions[index];
        Camera.main.transform.Rotate(50, 90 * index, 0);
    }

    public void GameHasFinished()
    {
        GameHasFinishedEveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void GameHasFinishedEveryoneRpc()
    {
        List<PlayerHolyResourceData> playerHolyResourceDataList = new List<PlayerHolyResourceData>();

        foreach (PlayerHolyResourceData phrd in playerHolyResourceDataNetworkList)
        {
            playerHolyResourceDataList.Add(phrd);
        }

        OnGameFinished?.Invoke(this, new OnGameFinishedEventArgs { playerHolyResourceDataList = playerHolyResourceDataList });

        Time.timeScale = 0f;

        CleanSubscriptions();
    }

    private void CleanSubscriptions()
    {
        OnGameReady = null;
        OnPlayerHolyResourceDataListChanged = null;
        OnGameFinished = null;
    }

    public void UpdatePlayerHolyResourceData(float holyResourceGathered, ulong clientId)
    {
        for (int i = 0; i < playerHolyResourceDataNetworkList.Count; i++)
        {
            PlayerHolyResourceData playerHolyResourceData = playerHolyResourceDataNetworkList[i];
            if (playerHolyResourceData.clientId == clientId)
            {
                playerHolyResourceData.holyResourceGathered += holyResourceGathered;
                playerHolyResourceDataNetworkList[i] = playerHolyResourceData;
                break;
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void GameReadyEveryoneRpc()
    {
        OnGameReady?.Invoke();
    }
}
