using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Building : NetworkBehaviour
{
    // PLAYER STATS
    private float unitSpawnTime;
    private float unitSpawnTimeMax;
    private float buildingXP;
    private float buildingXPMax;
    private int buildingLevel;
    private const int buildingLevelMax = 15;
    private Vector3 baseOfOriginPosition;
    private const int amoutOfMinersMax = 20;
    private int amoutOfMinersSpawned;
    // SERVER STATS
    [SerializeField] private Transform unitPrefab;
    private Vector3 unitSpawnPoint;
    private List<int> disabledMinersIndexes;
    private List<Transform> prespawnedUnits;
    // NEUTRAL BUILDING
    public Occupation occupationStatus;
    public bool isNeutralBuilding;
    private ulong claimedByPlayerWithClientId;
    private float claimingPercentage;
    // MATERIALS
    [SerializeField] private Material playerBuildingMaterial;
    [SerializeField] private Material defaultBuildingMaterial;

    public enum Occupation
    {
        Occupied,
        Empty
    }

    [Rpc(SendTo.Everyone)]
    public void AttachBuildingToPlayerRpc(Vector3 baseOfOriginPosition, bool isNeutralBuilding)
    {
        this.baseOfOriginPosition = baseOfOriginPosition;
        this.isNeutralBuilding = isNeutralBuilding;
        occupationStatus = isNeutralBuilding ? Occupation.Empty : Occupation.Occupied;
        if (!isNeutralBuilding && IsOwner)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            var meshRendererMaterials = new Material[1];

            meshRendererMaterials[0] = playerBuildingMaterial;

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].materials = meshRendererMaterials;
            }
        }
    }

    public void PreSpawnUnits(Vector3 unitSpawnPoint)
    {
        claimedByPlayerWithClientId = 999;
        claimingPercentage = 0;
        prespawnedUnits = new List<Transform>();
        disabledMinersIndexes = new List<int>();
        this.unitSpawnPoint = unitSpawnPoint;
        for (int i = 0; i < amoutOfMinersMax; i++)
        {
            disabledMinersIndexes.Add(i);
            Transform unitTransform = Instantiate(unitPrefab, unitSpawnPoint, Quaternion.identity);
            unitTransform.gameObject.SetActive(false);
            prespawnedUnits.Add(unitTransform);
        }
    }

    public override void OnNetworkSpawn()
    {
        buildingXP = 0;
        buildingXPMax = 50;
        unitSpawnTimeMax = 12;
        unitSpawnTime = 0;
        buildingLevel = 0;
        amoutOfMinersSpawned = 0;
    }

    private void Update()
    {
        // Only player building can spawn units | If unit claims the buildign it becomes non-neutral
        if (!IsOwner || occupationStatus == Occupation.Empty || amoutOfMinersSpawned == amoutOfMinersMax) return;

        if (unitSpawnTime > 0) unitSpawnTime -= Time.deltaTime;
        else if (PlayerMouseInput.Instance.lastWorldPosition != Vector3.zero)
        {
            unitSpawnTime = unitSpawnTimeMax;
            SpawnMinerServerRpc(buildingLevel, baseOfOriginPosition, PlayerMouseInput.Instance.lastWorldPosition);
            amoutOfMinersSpawned++;
        }
        //if (Input.GetMouseButtonDown(0))
        //{
        //    SpawnMinerServerRpc(buildingLevel, baseOfOriginPosition, PlayerMouseInput.Instance.lastWorldPosition);
        //}
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnMinerServerRpc(int buildingLevel, Vector3 getBackPosition, Vector3 directionToMove, ServerRpcParams serverRpcParams = default)
    {
        // Get inactive miner index
        int minerIndex = disabledMinersIndexes[0];
        // Enable the disabled miner
        Transform unitTransform = prespawnedUnits[minerIndex];
        unitTransform.gameObject.SetActive(true);
        unitTransform.position = unitSpawnPoint;
        
        // Remove from inactive indexes
        disabledMinersIndexes.Remove(minerIndex);
        // Set-up unit
        Unit unit = unitTransform.GetComponent<Unit>();
        //Unit_Reworked unit = unitTransform.GetComponent<Unit_Reworked>();
        // Spawn with ownership of the building owner
        unit.NetworkObject.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
        unit.SetOwnerBuildingForServer(this);
        unit.SetHealthForUnit(buildingLevel);
        unit.InitializeOwnerRpc(buildingLevel, getBackPosition, directionToMove, minerIndex);
    }

    public void BuildingGainXP(float xp)
    {
        if (buildingLevel >= buildingLevelMax) return;
        buildingXP += xp + (buildingLevel * 0.5f);

        if (buildingXP >= buildingXPMax)
        {
            buildingLevel++;
            buildingXPMax = buildingXPMax * 2;
            unitSpawnTimeMax = unitSpawnTimeMax * 0.92f;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void InteractWithBuildingServerRpc(float claimPower, ServerRpcParams serverRpcParams = default)
    {
        if (claimingPercentage == 0)
        {
            // Interacting player is the first one to claim
            claimedByPlayerWithClientId = serverRpcParams.Receive.SenderClientId;
            claimingPercentage = claimPower;
        }

        if (claimedByPlayerWithClientId != serverRpcParams.Receive.SenderClientId)
        {
            // Somebody has already claimed this building
            if (claimingPercentage == 100)
            {
                // Player who claimed this building before has lost its' ownership
                UpdateOccupationStatusOfTheBuildingEveryoneRpc(Occupation.Empty);
                NotifyBuildingHasLostOwnerRpc();
            }

            if (claimingPercentage > claimPower)
            {
                claimingPercentage -= claimPower;
            }
            else
            {
                // Interacting player is about to claim this building ower previous owner
                claimingPercentage = claimPower - claimingPercentage;
                claimedByPlayerWithClientId = serverRpcParams.Receive.SenderClientId;
            }
        }
        else
        {
            // Interacting player has already claimed this building before
            claimingPercentage += claimPower;
        }

        if (claimingPercentage >= 100)
        {
            // Interacting player has claimed this building completely
            claimingPercentage = 100;
            if (OwnerClientId != serverRpcParams.Receive.SenderClientId)
            {
                NetworkObject.ChangeOwnership(serverRpcParams.Receive.SenderClientId);
            }
            if (occupationStatus != Occupation.Occupied)
            {
                UpdateOccupationStatusOfTheBuildingEveryoneRpc(Occupation.Occupied);
                NotifyBuildingNewOwnerRpc();
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void NotifyBuildingHasLostOwnerRpc()
    {
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = defaultBuildingMaterial;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }
    }

    [Rpc(SendTo.Owner)]
    private void NotifyBuildingNewOwnerRpc()
    {
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = playerBuildingMaterial;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateOccupationStatusOfTheBuildingEveryoneRpc(Occupation updatedOccupationStatus)
    {
        occupationStatus = updatedOccupationStatus;
    }

    public void ResetUnit(int unitIndex)
    {
        // Disable/Reset the enabled miner
        Transform unitTransform = prespawnedUnits[unitIndex];
        unitTransform.gameObject.SetActive(false);
        unitTransform.position = unitSpawnPoint;
        // Restore disabled miner indexes
        disabledMinersIndexes.Add(unitIndex);
        NotifyAboutDisabledUnitOwnerRpc();
    }

    [Rpc(SendTo.Owner)]
    private void NotifyAboutDisabledUnitOwnerRpc()
    {
        amoutOfMinersSpawned--;
    }
}
