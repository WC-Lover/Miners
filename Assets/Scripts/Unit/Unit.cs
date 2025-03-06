using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Unit
{
    [RequireComponent(typeof(UnitInteraction))]
    [RequireComponent(typeof(UnitMovement))]
    [RequireComponent(typeof(UnitNetwork))]
    [RequireComponent(typeof(UnitSearch))]
    [RequireComponent(typeof(UnitState))]
    public class Unit : NetworkBehaviour
    {
        // References
        public UnitNetwork Network { get; private set; }
        public UnitMovement Movement { get; private set; }
        public UnitInteraction Interaction { get; private set; }
        public UnitSearch Search { get; private set; }
        public UnitState State { get; private set; }
        public UnitDelegateManager DelegateManager { get; private set; }

        // Configuration
        public UnitConfig Config { get; private set; }
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask buildingLayerMask;
        [SerializeField] private LayerMask resourceLayerMask;

        // Stats
        private float health;
        private float stamina;
        private float resourceWeight;
        private float holyResourceWeight;
        private float currentSearchRadius;
        private bool arrivedAtFirstDestination;

        // Server
        private Building serverOwnerBuilding;
        public void SetOwnerBuildingForServer(Building ownerBuilding) => serverOwnerBuilding = ownerBuilding;

        // UI
        [SerializeField] private UnitUI unitUI;
        [SerializeField] private Material playerMaterial;

        private void Awake()
        {
            Network = GetComponent<UnitNetwork>();
            Movement = GetComponent<UnitMovement>();
            Interaction = GetComponent<UnitInteraction>();
            Search = GetComponent<UnitSearch>();
            State = GetComponent<UnitState>();

            DelegateManager = new UnitDelegateManager();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                InitializeMaterials();
                unitUI.SetUp();
            }
            else
            {
                Movement.enabled = false;
                Interaction.enabled = false;
                Search.enabled = false;
                State.enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            DelegateManager.DisableAllDelegates();

            if (IsServer)
            {
                serverOwnerBuilding.ResetUnit(this);
            }
        }

        public void SetUnit(UnitSpawnData unitSpawnData)
        {
            InitializeUnitConfig(unitSpawnData);
                
            Movement = GetComponent<UnitMovement>();
            Interaction = GetComponent<UnitInteraction>();
            Search = GetComponent<UnitSearch>();
            State = GetComponent<UnitState>();
        }

        private void InitializeUnitConfig(UnitSpawnData unitSpawnData)
        {
            Config = new UnitConfig
            {
                // Health
                HealthMax = unitSpawnData.buildingLevel + 3,
                // Movement
                Speed = unitSpawnData.buildingLevel * 0.1f + 0.4f + (unitSpawnData.tempBonus == BonusSelectUI.Bonus.Speed ? 0.1f : 0) + (unitSpawnData.permBonus == BonusSelectUI.Bonus.Speed ? 0.1f : 0),
                ObstacleDetectionDistance = 1,
                // Interaction
                InteractionCooldownMax = 2 - unitSpawnData.buildingLevel * 0.05f,
                InteractionTarget = null,
                StaminaMax = (unitSpawnData.buildingLevel / 5) + 4 + (unitSpawnData.tempBonus == BonusSelectUI.Bonus.Damage ? 1f : 0) + (unitSpawnData.permBonus == BonusSelectUI.Bonus.Damage ? 1f : 0),
                // Interaction Distance
                UnitInteractionDistance = 0.25f + 0.125f, // 0.25 width of a Unit
                BuildingInteractionDistance = 0.71f + 0.125f, // sqrt(0.5^2 + 0.5^2) ~ 0.71(width) + 0.125(unit width/2)
                // Attack
                AttackDamage = unitSpawnData.buildingLevel * 0.2f + 1 + (unitSpawnData.tempBonus == BonusSelectUI.Bonus.Damage ? 0.25f : 0) + (unitSpawnData.permBonus == BonusSelectUI.Bonus.Damage ? 0.25f : 0),
                // Gather
                GatherWeight = unitSpawnData.buildingLevel * 0.5f + 1 + (unitSpawnData.tempBonus == BonusSelectUI.Bonus.Gather ? 0.25f : 0) + (unitSpawnData.permBonus == BonusSelectUI.Bonus.Gather ? 0.25f : 0),
                ResourceWeightMax = (unitSpawnData.buildingLevel / 5) + 2,
                // Capture
                CaptureStrength = unitSpawnData.buildingLevel * 0.5f + 1,
                // Search
                DefaultSearchRadius = 0.75f,
                MaxUnits = 100, // In late stage each of 4 players can spawn about 50 units
                MaxBuildings = 4,
                MaxResources = 55, // At max 10*10 (overall) - 3*3 (holy resource) - [4*10 - 4] (edges)
                UnitLayerMask = unitLayerMask,
                BuildingLayerMask = buildingLayerMask,
                ResourceLayerMask = resourceLayerMask,
                // First Destination / Get Back
                FirstDestinationPosition = unitSpawnData.firstDestinationPosition,
                BaseOfOriginPosition = unitSpawnData.baseOfOriginPosition,
            };

            stamina = Config.StaminaMax;
            health = Config.HealthMax;
            resourceWeight = 0;
            holyResourceWeight = 0;
            currentSearchRadius = Config.DefaultSearchRadius;
            arrivedAtFirstDestination = false;

            DelegateManager.OnUnitSetUp?.Invoke();
        }

        private void InitializeMaterials()
        {
            // Rework single material block
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            Material[] meshRendererMaterials = new Material[1];

            meshRendererMaterials[0] = playerMaterial;

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].materials = meshRendererMaterials;
            }
        }

        // Public API for other components
        public float ModifyStamina(float amount)
        {
            stamina = Mathf.Clamp(stamina + amount, 0, Config.StaminaMax);
            
            DelegateManager.OnStaminaChanged?.Invoke(stamina);

            return stamina;
        }
        public float ModifyHealth(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, Config.HealthMax);
        
            DelegateManager.OnHealthChanged?.Invoke(health);

            return health;
        }
        public float AddResource(float amount)
        {
            if (resourceWeight == 0) DelegateManager.OnResourceGathered?.Invoke(false);

            resourceWeight = Mathf.Clamp(resourceWeight + amount, 0, Config.ResourceWeightMax);
        
            return resourceWeight;
        }
        public float AddHolyResource(float amount)
        {
            if (holyResourceWeight == 0) DelegateManager.OnResourceGathered?.Invoke(true);

            resourceWeight = Mathf.Clamp(resourceWeight + amount, 0, Config.ResourceWeightMax);
            holyResourceWeight += amount;

            return holyResourceWeight;
        }
        public void ModifyCurrentSearchRadius(float amount) => currentSearchRadius += amount;
        public void ResetCurrentSearchRadius() => currentSearchRadius = Config.DefaultSearchRadius;
        public void ArrivedAtFirstDestination() => arrivedAtFirstDestination = true;
        //public void ResetUnitInOwnerBuilding() => serverOwnerBuilding.ResetUnit(this);

        // Getters
        public bool CanGather => resourceWeight < Config.ResourceWeightMax;
        public bool CanInteract => stamina > 0;
        public bool IsAlive => health > 0;
        public float GetHolyResourceWeight => holyResourceWeight;
        public float GetResourceWeight => resourceWeight;
        public float GetCurrentSearchRadius => currentSearchRadius;
        public bool HasArrivedAtFirstDestination => arrivedAtFirstDestination;
    }
}