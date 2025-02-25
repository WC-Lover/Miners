using System;
using Mono.Cecil;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Unit : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float obstacleDetectionDistance = 0.5f;
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private LayerMask buildingLayerMask;
    [SerializeField] private LayerMask resourceLayerMask;
    [SerializeField] private Material playerUnitMaterial;
    [SerializeField] private Material enemyUnitMaterial;

    [Header("Debug")]
    [SerializeField] private UnitState currentState;
    [SerializeField] public float stamina;
    [SerializeField] private float carryingWeight;
    [SerializeField] private float holyResourceWeight;
    [SerializeField] private float carryingWeightReturned;

    public NetworkVariable<float> health = new NetworkVariable<float>();
    private UnitStats stats;
    private SearchSettings searchSettings;
    private MovementSettings movementSettings;
    [SerializeField] private InteractionTarget currentTarget;
    private Rigidbody rb;
    private Building serverOwnerBuilding;
    private int minerIndex;
    private float rangeOfInteraction;

    private Vector3 basePosition;
    public EventHandler OnUnitDespawn;
    public event EventHandler<OnStaminaChangedEventArgs> OnStaminaChanged;
    public class OnStaminaChangedEventArgs : EventArgs
    {
        public float stamina;
    }

    private enum UnitState
    {
        ApproachingSpawnPosition,
        SearchingForInteraction,
        ApproachingTarget,
        Interacting,
        ReturningToBase,
        Restoring
    }

    private struct UnitStats
    {
        public float Speed;
        public float GatherPower;
        public float AttackDamage;
        public float StaminaMax;
        public int CarryCapacity;
        public float InteractionCooldown;
    }

    private struct SearchSettings
    {
        public float DefaultRadius;
        public float CurrentRadius;
        public int MaxUnits;
        public int MaxBuildings;
        public int MaxResources;
    }

    private struct MovementSettings
    {
        public Vector3 SpawnDirection;
        public Vector3 BaseDirection;
        public float RotationSpeed;
    }

    private class InteractionTarget
    {
        public Vector3 Position;
        public Unit Unit;
        public Building Building;
        public Resource Resource;
        public float RangeOfInteraction;
    }

    public void SetOwnerBuildingForServer(Building ownerBuilding) => serverOwnerBuilding = ownerBuilding;
    public void SetHealthForUnit(int buildingLevel) => health.Value = buildingLevel + 3;
    public void SetUnitUIMaxStats()
    {
        var unitUI = GetComponentInChildren<UnitUI>();
        unitUI.unitMaxHealth = health.Value;
        unitUI.unitMaxStamina = stats.StaminaMax;
    }

    [Rpc(SendTo.Owner)]
    public void InitializeOwnerRpc(int buildingLevel, Vector3 basePosition, Vector3 spawnDirection, int minerIndex, BonusSelectUI.Bonus tempBonus, BonusSelectUI.Bonus permBonus)
    {
        if (!IsOwner) return;

        rb = GetComponent<Rigidbody>();

        InitializeStats(buildingLevel, basePosition, spawnDirection, tempBonus, permBonus);
        SetUnitUIMaxStats();
        TransitionState(UnitState.ApproachingSpawnPosition);
        currentTarget = new InteractionTarget { Position = spawnDirection, RangeOfInteraction = 0.14f }; // range is unit width / 2 + 0.015f

        this.minerIndex = minerIndex;
        this.basePosition = basePosition;
    }

    public override void OnNetworkSpawn()
    {
        InitializeMaterials();
    }

    private void InitializeStats(int buildingLevel, Vector3 basePosition, Vector3 spawnDirection, BonusSelectUI.Bonus tempBonus, BonusSelectUI.Bonus permBonus)
    {
        stats = new UnitStats
        {
            Speed = buildingLevel * 0.1f + 0.4f + (tempBonus == BonusSelectUI.Bonus.Speed ? 0.1f : 0) + (permBonus == BonusSelectUI.Bonus.Speed ? 0.1f : 0),
            GatherPower = buildingLevel * 0.5f + 1 + (tempBonus == BonusSelectUI.Bonus.Gather ? 0.25f : 0) + (permBonus == BonusSelectUI.Bonus.Gather ? 0.25f : 0),
            AttackDamage = buildingLevel * 0.2f + 1 + (tempBonus == BonusSelectUI.Bonus.Damage ? 0.25f : 0) + (permBonus == BonusSelectUI.Bonus.Damage ? 0.25f : 0),
            StaminaMax = (buildingLevel / 5) + 4 + (tempBonus == BonusSelectUI.Bonus.Damage ? 1f : 0) + (permBonus == BonusSelectUI.Bonus.Damage ? 1f : 0),
            CarryCapacity = (buildingLevel / 5) + 2,
            InteractionCooldown = buildingLevel > 0 ? buildingLevel * 0.95f * 2 : 2
        };

        searchSettings = new SearchSettings
        {
            DefaultRadius = 0.25f,
            CurrentRadius = 0.25f,
            MaxUnits = 100, // In late stage each of 4 players can spawn about 50 units
            MaxBuildings = 4,
            MaxResources = 55 // At max 10*10 (overall) - 3*3 (holy resource) - [4*10 - 4] (edges)
        };

        movementSettings = new MovementSettings
        {
            SpawnDirection = spawnDirection,
            BaseDirection = basePosition,
            RotationSpeed = 3f,
        };

        rangeOfInteraction = 0.25f; // Units' width. Only used in Unit-Unit interaction. 
        stamina = stats.StaminaMax;
    }

    private void InitializeMaterials()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        Material[] meshRendererMaterials = new Material[1];

        meshRendererMaterials[0] = IsOwner ? playerUnitMaterial : enemyUnitMaterial;

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        UpdateState();
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case UnitState.ApproachingSpawnPosition:
                UpdateApproachSpawnPosition();
                break;
            case UnitState.SearchingForInteraction:
                UpdateSearch();
                break;
            case UnitState.ApproachingTarget:
                UpdateApproach();
                break;
            case UnitState.Interacting:
                UpdateInteraction();
                break;
            case UnitState.ReturningToBase:
                UpdateReturnToBase();
                break;
            case UnitState.Restoring:
                UpdateRestore();
                break;
        }
    }

    #region State Methods
    private void UpdateApproachSpawnPosition()
    {
        if (MoveTowards(movementSettings.SpawnDirection))
        {
            TransitionState(UnitState.SearchingForInteraction);
        }
    }

    private void UpdateSearch()
    {
        currentTarget = FindNearestInteraction();

        if (currentTarget != null)
        {
            rb.rotation = Quaternion.LookRotation(currentTarget.Position - transform.position);
            TransitionState(UnitState.ApproachingTarget);
        }
        else
        {
            searchSettings.CurrentRadius += Time.fixedDeltaTime;
        }
    }

    private void UpdateApproach()
    {
        if (currentTarget == null || MoveTowards(currentTarget.Position))
        {
            TransitionState(UnitState.Interacting);
        }
    }

    private void UpdateInteraction()
    {
        if (currentTarget == null || !CanInteract())
        {
            TransitionState(UnitState.SearchingForInteraction);
            return;
        }

        PerformInteraction();

        if (ShouldReturnToBase())
        {
            OnInteractionTargetDespawn(null, EventArgs.Empty); // If object is no longer of interest, unsubscribe from delegate
            TransitionState(UnitState.ReturningToBase);
        }
    }

    private void UpdateReturnToBase()
    {
        if (currentTarget != null) MoveTowards(currentTarget.Position); // BasePosition
    }

    private void UpdateRestore()
    {
        if (currentTarget != null && currentTarget.Building != null && currentTarget.Building.IsOwner)
        {
            currentTarget.Building.BuildingGainXP(carryingWeight * Time.fixedDeltaTime, holyResourceWeight);
            if (holyResourceWeight > 0) holyResourceWeight = 0;
            carryingWeight = Mathf.Max(carryingWeight - Time.fixedDeltaTime, 0);
            stamina = Mathf.Min(stamina + Time.fixedDeltaTime, stats.StaminaMax);
            OnStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs { stamina = this.stamina });
        }
        if (stamina >= stats.StaminaMax && carryingWeight <= 0)
        {
            currentTarget = null;
            TransitionState(UnitState.SearchingForInteraction);
        }
    }
    #endregion

    #region Helper Methods

    private bool MoveTowards(Vector3 targetPosition)
    {
        if (currentTarget == null) return false;

        Vector3 desiredDirection = (targetPosition - transform.position).normalized;
        Vector3 actualDirection = desiredDirection;

        if (ObstacleInFront(out Vector3 avoidanceDir))
        {
            // Prioritize avoidance when close to obstacles
            float avoidanceStrength = Mathf.Clamp01(1 - Vector3.Dot(desiredDirection, avoidanceDir));
            actualDirection = Vector3.Lerp(desiredDirection, avoidanceDir, avoidanceStrength);
        }

        RotateTowards(actualDirection);
        rb.MovePosition(transform.position + stats.Speed * Time.fixedDeltaTime * actualDirection);
        if (currentTarget.RangeOfInteraction == 0) return false; // Only possible when state -> ReturnToBase
        return currentTarget.RangeOfInteraction >= Vector3.Distance(transform.position, targetPosition);
    }

    private void OnInteractionTargetDespawn(object sender, EventArgs e)
    {
        if (currentTarget == null) return;

        // Cleanup event subscriptions
        if (currentTarget.Unit != null)
        {
            currentTarget.Unit.OnUnitDespawn -= OnInteractionTargetDespawn;
            currentTarget.Unit = null;
        }

        if (currentTarget.Resource != null)
        {
            currentTarget.Resource.OnResourceDespawn -= OnInteractionTargetDespawn;
            currentTarget.Resource = null;
        }

        currentTarget = null;
    }

    private void RotateTowards(Vector3 direction)
    {
        float speedBoost = 1f;
        float angleDifference = Vector3.Angle(transform.forward, direction);

        // Dynamic rotation speed based on urgency
        if (angleDifference > 45f)
        {
            speedBoost = Mathf.Lerp(2f, 4f, (angleDifference - 45f) / 90f);
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(targetRotation);
        //rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation,
        //    movementSettings.RotationSpeed * speedBoost * Time.fixedDeltaTime);
    }

    private bool ObstacleInFront(out Vector3 avoidanceDirection)
    {
        avoidanceDirection = transform.forward;
        bool obstacleDetected = false;
        float leftWeight = 0f;
        float rightWeight = 0f;

        float[] rayAngles = { -30f, -15f, 0f, 15f, 30f }; // Wider detection
        float[] rayDistances = { 1f, 0.8f, 0.6f, 0.8f, 1f }; // Different distances per angle

        for (int i = 0; i < rayAngles.Length; i++)
        {
            float angle = rayAngles[i];
            float distance = rayDistances[i] * obstacleDetectionDistance;
            Vector3 rayDir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Vector3 rayStart = transform.position + transform.up * 0.1f;

            Debug.DrawRay(rayStart, rayDir, Color.red);

            if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit, distance))
            {
                if (IsCurrentTarget(hit.collider)) continue;

                obstacleDetected = true;
                float weight = 1 - (hit.distance / distance);

                // Determine steering direction based on ray angle
                if (angle < 0)
                {
                    // Left side obstacle - accumulate right steering
                    rightWeight += weight;
                }
                else if (angle > 0)
                {
                    // Right side obstacle - accumulate left steering
                    leftWeight += weight;
                }
                else
                {
                    // Center obstacle - check both sides
                    rightWeight += weight;
                    leftWeight += weight;
                }
            }
        }

        if (!obstacleDetected) return false;

        // Calculate final avoidance direction
        if (leftWeight > rightWeight)
        {
            // Steer left with intensity based on weight difference
            avoidanceDirection = Vector3.Lerp(transform.forward, -transform.right,
                Mathf.Clamp01(leftWeight - rightWeight)).normalized;
        }
        else if (rightWeight > leftWeight)
        {
            // Steer right with intensity based on weight difference
            avoidanceDirection = Vector3.Lerp(transform.forward, transform.right,
                Mathf.Clamp01(rightWeight - leftWeight)).normalized;
        }
        else
        {
            // Equal weights - move backward
            avoidanceDirection = -transform.forward;
        }

        Debug.DrawRay(transform.position, avoidanceDirection * 2f, Color.magenta);
        return true;
    }

    private bool IsCurrentTarget(Collider detectedCollider)
    {
        if (currentTarget == null) return false;
        // ReturningToBase specific case, as it doesn't have currentTarget.Building to check
        if (currentTarget.RangeOfInteraction == 0f
            && currentTarget.Position + (Vector3.up * 0.25f) == detectedCollider.transform.position) return true;
        // Check if detected collider belongs to current target
        return (currentTarget.Unit != null && detectedCollider.transform.IsChildOf(currentTarget.Unit.transform)) ||
               (currentTarget.Building != null && detectedCollider.transform.IsChildOf(currentTarget.Building.transform)) ||
               (currentTarget.Resource != null && detectedCollider.transform.IsChildOf(currentTarget.Resource.transform));
    }

    private InteractionTarget FindNearestInteraction()
    {
        InteractionTarget nearest = new InteractionTarget();
        float nearestDistance = float.MaxValue;

        // Priority order: Units -> Buildings -> Resources
        CheckForEnemyUnits(ref nearest, ref nearestDistance);
        if (nearest.Unit != null) return nearest;

        CheckForBuildings(ref nearest, ref nearestDistance);
        if (nearest.Building != null) return nearest;

        CheckForResources(ref nearest, ref nearestDistance);
        if (nearest.Resource != null) return nearest;

        return null;
    }

    private void CheckForEnemyUnits(ref InteractionTarget target, ref float nearestDistance)
    {
        if (stamina <= 0) return;

        Collider[] results = new Collider[searchSettings.MaxUnits];
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchSettings.CurrentRadius, results, unitLayerMask);
        
        for (int i = 0; i < count; i++)
        {
            Unit unit = results[i].GetComponentInParent<Unit>();
            if (unit != null && unit.OwnerClientId != OwnerClientId)
            {
                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    target.Unit = unit;
                    target.Position = unit.transform.position;
                    target.RangeOfInteraction = rangeOfInteraction;
                    unit.OnUnitDespawn += OnInteractionTargetDespawn;
                }
            }
        }
    }

    private void CheckForBuildings(ref InteractionTarget target, ref float nearestDistance)
    {
        Collider[] results = new Collider[searchSettings.MaxBuildings];
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchSettings.CurrentRadius, results, buildingLayerMask);

        for (int i = 0; i < count; i++)
        {
            Building building = results[i].GetComponentInParent<Building>();

            if (!building.isNeutralBuilding) continue;

            bool buildingIsOccupied = building.occupationStatus == Building.Occupation.Occupied ? true : false;
            bool buildingBelongsToUnitOwner = building.OwnerClientId == OwnerClientId;

            if (building != null && (!buildingBelongsToUnitOwner || !buildingIsOccupied))
            {
                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    target.Building = building;
                    target.Position = building.transform.position;
                    target.RangeOfInteraction = 0.5f + 0.125f + 0.125f;
                }
            }
        }
    }

    private void CheckForResources(ref InteractionTarget target, ref float nearestDistance)
    {
        if (stamina <= 0) return;

        Collider[] results = new Collider[searchSettings.MaxResources];
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchSettings.CurrentRadius, results, resourceLayerMask);

        for (int i = 0; i < count; i++)
        {
            Resource resource = results[i].GetComponentInParent<Resource>();

            if (resource != null)
            {
                float distance = Vector3.Distance(transform.position, resource.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    target.Resource = resource;
                    target.Position = resource.transform.position;
                    target.RangeOfInteraction = resource.rangeOfInteraction;
                    resource.OnResourceDespawn += OnInteractionTargetDespawn;
                }
            }
        }
    }

    private bool CanInteract()
    {
        bool canInteract = stamina > 0 && carryingWeight < stats.CarryCapacity;
        if (currentTarget.Unit != null) canInteract &= currentTarget.Unit.health.Value > 0;
        if (currentTarget.Building != null) canInteract &= currentTarget.Building.isNeutralBuilding 
                && (currentTarget.Building.occupationStatus == Building.Occupation.Empty || !currentTarget.Building.IsOwner);
        if (currentTarget.Resource != null) canInteract &= currentTarget.Resource.weight.Value > 0;
        //try
        //{
        //    canInteract &= Vector3.Distance(transform.position, currentTarget.Position) <= currentTarget.RangeOfInteraction;
        //}
        //catch (Exception ex)
        //{
        //    Debug.Log("target doesn't have range of interaction or possition");
        //}
        return canInteract;
    }

    private bool ShouldReturnToBase()
    {
        return stamina <= 0 || carryingWeight >= stats.CarryCapacity;
    }

    private void PerformInteraction()
    {
        // Implement specific interaction logic here
        stamina -= Time.fixedDeltaTime;
        if (currentTarget.Unit != null)
        {
            currentTarget.Unit.InteractWithOtherUnitServerRpc(stats.AttackDamage * Time.fixedDeltaTime);
        }
        else if (currentTarget.Building != null)
        {
            currentTarget.Building.InteractWithBuildingServerRpc(stats.GatherPower * Time.fixedDeltaTime);
        }
        else if (currentTarget.Resource != null)
        {
            float gatheredAtOnce = stats.GatherPower * Time.fixedDeltaTime;
            carryingWeight += gatheredAtOnce;
            if (currentTarget.Resource.IsHolyResource()) holyResourceWeight += gatheredAtOnce;
            currentTarget.Resource.InteractWithResourceServerRpc(gatheredAtOnce);
        }
        OnStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs { stamina = this.stamina });
    }

    private void TransitionState(UnitState newState)
    {
        currentState = newState;
        searchSettings.CurrentRadius = searchSettings.DefaultRadius;

        switch (newState)
        {
            case UnitState.ReturningToBase:
                currentTarget = new InteractionTarget { Position = basePosition, RangeOfInteraction = 0f };
                break;
            case UnitState.SearchingForInteraction:
                currentTarget = null; // Clear any previous target
                break;
            case UnitState.ApproachingTarget:
                // Ensure we have a valid target
                if (currentTarget == null)
                {
                    TransitionState(UnitState.SearchingForInteraction);
                }
                break;
        }
    }
    #endregion

    [Rpc(SendTo.Server)]
    private void InteractWithOtherUnitServerRpc(float damage)
    {
        if (health.Value > 0) health.Value -= damage;
        if (health.Value <= 0) DespawnUnit();
    }

    private void DespawnUnit()
    {
        NotifyClientsUnitDespawnEverybodyRpc();
        serverOwnerBuilding.ResetUnit(minerIndex);
        NetworkObject.Despawn(false);
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsUnitDespawnEverybodyRpc()
    {
        // Unsubscribe to avoid Memory Leaks and Ghost Callbacks
        OnUnitDespawn?.Invoke(this, EventArgs.Empty);
        OnUnitDespawn = null; // just to be sure
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == UnitState.ReturningToBase && collision.collider != null)
        {
            Building building = collision.collider.GetComponentInParent<Building>();
            if (building != null && building.IsOwner && building.occupationStatus == Building.Occupation.Occupied)
            {
                currentTarget = new InteractionTarget { Building = building };
                TransitionState(UnitState.Restoring);
            }
        }

        if (currentState == UnitState.ApproachingTarget && collision.collider != null)
        { 
            Resource holyResource = collision.collider.GetComponentInParent<Resource>();
            if (holyResource != null && holyResource.IsHolyResource() && holyResource.weight.Value > 0)
            {
                currentTarget = new InteractionTarget { Position = holyResource.transform.position, Resource = holyResource, RangeOfInteraction = holyResource.rangeOfInteraction };
                TransitionState(UnitState.Interacting);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.Position);
        }

        // Draw search radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, searchSettings.CurrentRadius);
    }
}