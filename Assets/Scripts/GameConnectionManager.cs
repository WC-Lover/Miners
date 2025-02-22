using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameConnectionManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerBuildingPrefab;
    [SerializeField] private GameObject neutralBuildingPrefab;
    [SerializeField] private GameObject resourceSpawnerPrefab;

    private Vector3[] buildingSpawnPos;
    private Vector3[] buildingUnitSpawnPos;
    private Vector3[] neutralBuildingSpawnPos;
    private Vector3[] neutralBuildingUnitSpawnPos;
    private static int counter;


    public override void OnNetworkSpawn()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

        if (!IsServer) return;
        counter = -1;

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
        if (next.name == Loader.Scene.GameScene.ToString() && IsOwner)
        {
            // As new player connects to the scene, spawn base for it
            if (IsServer)
            {
                CreateBuildingForConnectedPlayer(NetworkManager.Singleton.LocalClientId);
                AttachNeutralBuildingsOnGameStart();
                SpawnResourceSpawner();
            }
            else
            {
                CreateBuildingForConnectedPlayerServerRpc();
            }
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
            GameObject neutralBuilding = Instantiate(neutralBuildingPrefab, neutralBuildingSpawnPos[i], Quaternion.identity);
            neutralBuilding.SetActive(true);
            Building building = neutralBuilding.GetComponent<Building>();
            building.PreSpawnUnits(neutralBuildingUnitSpawnPos[i]);
            building.NetworkObject.Spawn();
            building.AttachBuildingToPlayerRpc(neutralBuildingSpawnPos[i], true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreateBuildingForConnectedPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        CreateBuildingForConnectedPlayer(serverRpcParams.Receive.SenderClientId);
    }

    private void CreateBuildingForConnectedPlayer(ulong clientId)
    {
        counter++;
        GameObject playerBuilding = Instantiate(playerBuildingPrefab, buildingSpawnPos[counter], Quaternion.identity);
        playerBuilding.SetActive(true);
        Building building = playerBuilding.GetComponent<Building>();
        building.PreSpawnUnits(buildingUnitSpawnPos[counter]);
        building.NetworkObject.SpawnWithOwnership(clientId);
        building.AttachBuildingToPlayerRpc(buildingSpawnPos[counter], false);
    }

}
