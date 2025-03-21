using System;
using System.Collections.Generic;
using Assets.Scripts.Unit;
using Unity.Netcode;
using UnityEngine;

public class Building : NetworkBehaviour
{
    public static Building Instance;

    // PLAYER STATS
    [SerializeField] private float unitSpawnTime;
    [SerializeField] private float unitSpawnTimeMax;
    private float resourceWeight;
    private float buildingMaxResourceWeight;
    private int buildingLevel;
    private const int buildingLevelMax = 15;
    private Vector3 baseOfOriginPosition;
    private const int amoutOfMinersMax = 50;
    private int amoutOfMinersSpawned;

    private BonusSelectUI.Bonus tempBonus;
    private BonusSelectUI.Bonus permBonus;

    private float holyResourceWeight = 0;

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
    // Maybe network variable?
    private float claimingPercentage = 0;
    // UI / MATERIALS
    [SerializeField] private Material playerBuildingMaterial;
    [SerializeField] private Material enemyPlayerBuildingMaterial;
    [SerializeField] private Material neutralBuildingMaterial;
    [SerializeField] private Transform bonusSelectionUI;
    [SerializeField] private Transform playerBuildingUI;
    private MeshRenderer[] meshRenderers;

    // Material
    [SerializeField] private BuildingMaterial buildingMaterial;

    // Can be changed to actions
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

    public bool IsOccupied => occupationStatus == Occupation.Occupied;

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        resourceWeight = 0;
        buildingMaxResourceWeight = 50;
        unitSpawnTimeMax = 2;
        unitSpawnTime = 0;
        buildingLevel = 0;
        amoutOfMinersSpawned = 0;
        GameManager.Instance.OnGameFinished += GameManager_OnGameFinished;
    }

    public override void OnNetworkSpawn()
    {
        if (isNeutralBuilding)
        {
            buildingMaterial.SetNeutralBuildingMaterial();
        }
        else if (IsOwner)
        {
            buildingMaterial.SetBuildingMaterial();
            Instance = this;
            GameManager.Instance.OnGameReady += GameManager_OnGameReady;
        }
    }

    private void GameManager_OnGameReady()
    {
        if (!isNeutralBuilding)
        {
            bonusSelectionUI.gameObject.SetActive(true);
            playerBuildingUI.gameObject.SetActive(true);
        }
        GameManager.Instance.OnGameReady -= GameManager_OnGameReady;
    }

    private void GameManager_OnGameFinished(object sender, GameManager.OnGameFinishedEventArgs e)
    {
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Owner)]
    public void SetBuildingOwnerRpc(Vector3 baseOfOriginPosition)
    {
        this.baseOfOriginPosition = baseOfOriginPosition;
    }

    public void PreCreateUnits(Vector3 unitSpawnPoint)
    {
        unitPool.PredefineUnitPoolByHost(unitPrefab, unitSpawnPoint);
    }



    private void Update()
    {
        // Only player building can spawn units | If unit claims the buildign it becomes non-neutral
        if (!IsOwner || occupationStatus == Occupation.Empty || amoutOfMinersSpawned == amoutOfMinersMax || !unitsAllowedToSpawn) return;

        if (unitSpawnTime > 0)
        {
            unitSpawnTime -= Time.deltaTime;
            OnUnitSpawnTimeChanged?.Invoke(this, new OnUnitSpawnTimeChangedEventArgs
            {
                unitSpawnTimer = this.unitSpawnTime,
                unitSpawnTimerMax = this.unitSpawnTimeMax
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
        //Add possibility to press right mouse button to spawn with faster interval or all at once?
        //if (Input.GetMouseButtonDown(1))
        //{
        //    SpawnMinerServerRpc(buildingLevel, baseOfOriginPosition, PlayerMouseInput.Instance.lastWorldPosition, tempBonus, permBonus);
        //}
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnMinerServerRpc(int buildingLevel, Vector3 baseOfOriginPosition, Vector3 firstDestinationPosition, BonusSelectUI.Bonus tempBonus, BonusSelectUI.Bonus permBonus, ServerRpcParams serverRpcParams = default)
    {
        Unit unit = unitPool.GetUnit();

        if (unit == null) return;

        unit.NetworkObject.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
        unit.SetOwnerBuildingForServer(this);
        unit.Network.InitializeUnit(new UnitSpawnData 
        { 
            buildingLevel = buildingLevel,
            baseOfOriginPosition = baseOfOriginPosition,
            firstDestinationPosition = firstDestinationPosition,
            tempBonus = tempBonus,
            permBonus = permBonus 
        });
    }

    public void BuildingGainXP(float resourceWeight, float holyResourceWeight)
    {
        if (holyResourceWeight > 0)
        {
            this.holyResourceWeight += holyResourceWeight;
        }
        if (buildingLevel >= buildingLevelMax) return;
        this.resourceWeight += resourceWeight + (buildingLevel * 0.01f);
        OnBuildingXPChanged?.Invoke(this, new OnBuildingXPChangedEventArgs
        {
            buildingXP = this.resourceWeight,
            buildingXPMax = this.buildingMaxResourceWeight,
        });

        if (this.resourceWeight >= buildingMaxResourceWeight)
        {
            buildingLevel++;
            buildingMaxResourceWeight = buildingMaxResourceWeight * 1.5f;
            this.resourceWeight = 0;
            unitSpawnTimeMax = unitSpawnTimeMax * 0.92f;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void InteractWithNeutralBuildingServerRpc(float claimPower, ServerRpcParams serverRpcParams = default)
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
