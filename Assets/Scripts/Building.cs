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

    private BonusSelectUI.Bonus tempBonus;
    private BonusSelectUI.Bonus permBonus;

    private float holyResourceGathered = 0;

    private bool unitsAllowedToSpawn = false;
    // SERVER STATS
    [SerializeField] private Unit unitPrefab;
    private Vector3 unitSpawnPoint;
    private List<int> disabledMinersIndexes;
    private List<Transform> prespawnedUnits;
    [SerializeField] private UnitPool unitPool;
    // NEUTRAL BUILDING
    public Occupation occupationStatus;
    public bool isNeutralBuilding;
    private ulong claimedByPlayerWithClientId = 999;
    private float claimingPercentage = 0;
    // UI / MATERIALS
    [SerializeField] private Material playerBuildingMaterial;
    [SerializeField] private Material enemyPlayerBuildingMaterial;
    [SerializeField] private Material neutralBuildingMaterial;
    [SerializeField] private Transform bonusSelectionUI;
    [SerializeField] private Transform playerBuildingUI;

    public EventHandler<OnClaimingPercentageChangedEventArgs> OnClaimingPercentageChanged;
    public class OnClaimingPercentageChangedEventArgs: EventArgs
    {
        public float claimingPercentage;
    }
    public EventHandler<OnUnitSpawnTimeChangedEventArgs> OnUnitSpawnTimeChanged;
    public class OnUnitSpawnTimeChangedEventArgs : EventArgs
    {
        public float unitSpawnTimer;
        public float unitSpawnTimerMax;
    }
    public EventHandler<OnBuildingXPChangedEventArgs> OnBuildingXPChanged;
    public class OnBuildingXPChangedEventArgs : EventArgs
    {
        public float buildingXP;
        public float buildingXPMax;
    }

    public enum Occupation
    {
        Occupied,
        Empty
    }

    private void Awake()
    {
        GameManager.Instance.OnGameFinished += GameManager_OnGameFinished;
    }

    private void GameManager_OnGameFinished(object sender, GameManager.OnGameFinishedEventArgs e)
    {
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Owner)]
    public void SetBasePositionOwnerRpc(Vector3 baseOfOriginPosition, bool isNeutralBuilding)
    {
        this.baseOfOriginPosition = baseOfOriginPosition;
        SetBuildingEveryoneRpc(isNeutralBuilding);
    }

    [Rpc(SendTo.Everyone)]
    private void SetBuildingEveryoneRpc(bool isNeutralBuilding)
    {
        this.isNeutralBuilding = isNeutralBuilding;

        if (isNeutralBuilding) GameManager.Instance.OnGameReady -= GameManager_OnGameReady;

        occupationStatus = isNeutralBuilding ? Occupation.Empty : Occupation.Occupied;

        if (isNeutralBuilding)
        {
            permBonus = BonusSelectUI.Bonus.None;
            tempBonus = BonusSelectUI.Bonus.None;
        }

        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];

        meshRendererMaterials[0] = isNeutralBuilding ? neutralBuildingMaterial : (IsOwner ? playerBuildingMaterial : enemyPlayerBuildingMaterial);

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }
    }

    public void PreCreateUnits(Vector3 unitSpawnPoint)
    {
        unitPool.PredefineUnitPoolByHost(unitPrefab, unitSpawnPoint);
    }

    private void GameManager_OnGameReady(object sender, EventArgs e)
    {
        if (!isNeutralBuilding)
        {
            bonusSelectionUI.gameObject.SetActive(true);
            playerBuildingUI.gameObject.SetActive(true);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) GameManager.Instance.OnGameReady += GameManager_OnGameReady;
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
        if (!IsOwner || occupationStatus == Occupation.Empty || amoutOfMinersSpawned == amoutOfMinersMax || !unitsAllowedToSpawn) return;

        if (unitSpawnTime > 0)
        {
            unitSpawnTime -= Time.deltaTime;
            OnUnitSpawnTimeChanged?.Invoke(this, new OnUnitSpawnTimeChangedEventArgs {
                unitSpawnTimer = this.unitSpawnTime, unitSpawnTimerMax = this.unitSpawnTimeMax
            });
        }
        else if (PlayerMouseInput.Instance.lastWorldPosition != Vector3.zero)
        {
            unitSpawnTime = unitSpawnTimeMax;
            OnUnitSpawnTimeChanged?.Invoke(this, new OnUnitSpawnTimeChangedEventArgs
            {
                unitSpawnTimer = this.unitSpawnTime,
                unitSpawnTimerMax = this.unitSpawnTimeMax
            });
            SpawnMinerServerRpc(buildingLevel, baseOfOriginPosition, PlayerMouseInput.Instance.lastWorldPosition, tempBonus, permBonus);
            amoutOfMinersSpawned++;
        }
        // Add possibility to press right mouse button to spawn with faster interval or all at once?
        //if (Input.GetMouseButtonDown(1))
        //{
        //    SpawnMinerServerRpc(buildingLevel, baseOfOriginPosition, PlayerMouseInput.Instance.lastWorldPosition);
        //}
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnMinerServerRpc(int buildingLevel, Vector3 getBackPosition, Vector3 directionToMove, BonusSelectUI.Bonus tempBonus, BonusSelectUI.Bonus permBonus, ServerRpcParams serverRpcParams = default)
    {
        Unit unit = unitPool.GetUnit();
        unit.NetworkObject.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
        unit.SetOwnerBuildingForServer(this);
        unit.InitializeOwnerRpc(buildingLevel, getBackPosition, directionToMove, tempBonus, permBonus);
    }

    public void BuildingGainXP(float xp, float holyResource)
    {
        if (holyResource > 0)
        {
            holyResourceGathered += holyResource;
        }
        if (buildingLevel >= buildingLevelMax) return;
        buildingXP += xp + (buildingLevel * 0.5f);
        OnBuildingXPChanged?.Invoke(this, new OnBuildingXPChangedEventArgs
        {
            buildingXP = this.buildingXP,
            buildingXPMax = this.buildingXPMax,
        });

        if (buildingXP >= buildingXPMax)
        {
            buildingLevel++;
            buildingXPMax = buildingXPMax * 1.5f;
            buildingXP = 0;
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
            }
        }

        UpdateClaimPercentageEveryoneRpc(claimingPercentage);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateClaimPercentageEveryoneRpc(float claimingPercentage)
    {
        OnClaimingPercentageChanged?.Invoke(this, new OnClaimingPercentageChangedEventArgs { claimingPercentage = claimingPercentage});
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateOccupationStatusOfTheBuildingEveryoneRpc(Occupation updatedOccupationStatus)
    {
        occupationStatus = updatedOccupationStatus;

        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = updatedOccupationStatus == Occupation.Empty ? neutralBuildingMaterial :
            (IsOwner ? playerBuildingMaterial : enemyPlayerBuildingMaterial);

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }
    }

    public void ResetUnit(Unit unit)
    {
        unitPool.ReturnUnit(unit);
        NotifyAboutDisabledUnitOwnerRpc();
    }

    [Rpc(SendTo.Owner)]
    private void NotifyAboutDisabledUnitOwnerRpc()
    {
        amoutOfMinersSpawned--;
    }

    public void SetBonusAttributes(BonusSelectUI.Bonus tempBonus, BonusSelectUI.Bonus permBonus)
    {
        this.tempBonus = tempBonus;
        this.permBonus = permBonus;
    }

    public void AllowUnitSpawn()
    {
        unitsAllowedToSpawn = true;
    }
}
