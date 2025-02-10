using System;
using Unity.Netcode;
using UnityEngine;

public class Unit : NetworkBehaviour
{
    // STATS
    const float interactionDistance = 1f;
    public NetworkVariable<float> health = new NetworkVariable<float>();
    private float speed;
    private float gatherPower;
    private float gatherCooldown;
    private float attackDamage; 
    private float attackCooldown;
    private float staminaMax;
    [SerializeField] private float stamina;
    private int carryCapacity;
    [SerializeField] private float carryingWeight;
    [SerializeField] private float carryingWeightReturned;
    // SEARCH
    [SerializeField] private float defaultSearchRadius = 0.25f;
    [SerializeField] private float searchRadius;
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private LayerMask buildingLayerMask;
    [SerializeField] private LayerMask resourceLayerMask;
    private Collider[] unitsColliders;
    private Collider[] buildingsColliders;
    private Collider[] resourcesColliders;
    // APPROACH/INTERACT
    private Vector3 interactionDirection;
    private float interactionDirectionUpdateTimer = 0;
    private float interactionDirectionUpdateTimerMax = 0.2f;
    private Resource closestInteractionResource;
    private Building closestInteractionBuilding;
    private Unit interactionUnit;
    private Vector3 directionToMoveBackToBase;
    private Vector3 directionToMoveAfterSpawn;
    // AVOID COLLISION
    private Vector3 currentDirection;
    private Vector3[] directionsArray = new Vector3[3];
    public float obstacleDetectionRange = 0.5f;
    // MATERIALS
    [SerializeField] private Material unitPlayerMaterial;
    [SerializeField] private Material defaultUnitMaterial;
    private UnitState unitState;
    private bool hasAlreadyBeenCreated = false;

    // TEMP
    private Rigidbody rb;

    private Building serverOwnerBuilding;

    private int minerIndex;
    private float deltaTime;

    public enum UnitState
    {
        Searching,
        Approach,
        Interact
    }

    public void SetOwnerBuildingForServer(Building ownerBuilding)
    {
        serverOwnerBuilding = ownerBuilding;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (carryingWeight >= carryCapacity || stamina <= 0)
        {

            Building building = collision.collider.GetComponentInParent<Building>();
            if (building != null && building.IsOwner
                && building.occupationStatus == Building.Occupation.Occupied)
            {
                // if returned from claiming other building, gain less XP but not 0!!!
                building.BuildingGainXP(carryingWeight * deltaTime);
                carryingWeightReturned += carryingWeight * deltaTime;
                if (carryingWeightReturned >= carryingWeight)
                {
                    stamina = staminaMax;
                    carryingWeight = 0;
                    carryingWeightReturned = 0;
                    // when unit is refilled, trigger search for next interaction
                    interactionDirection = Vector3.up;
                    currentDirection = Vector3.up;
                    closestInteractionBuilding = null;
                    directionToMoveAfterSpawn = Vector3.zero;
                }
            }

        }
        if (stamina > 0)
        {
            Unit unit = collision.collider.GetComponentInParent<Unit>();
            if (unit != null && !unit.IsOwner) unitState = UnitState.Searching;
        }
    }

    [Rpc(SendTo.Owner)]
    public void InitializeOwnerRpc(int BuildingLevel, Vector3 getBackPosition, Vector3 directionToMove, int minerIndex)
    {
        // This one is being sent to all Units of this owner???
        if (hasAlreadyBeenCreated) return;
        rb = GetComponent<Rigidbody>();
        hasAlreadyBeenCreated = true;
        this.minerIndex = minerIndex;
        unitState = UnitState.Approach;
        carryingWeight = 0;
        carryingWeightReturned = 0;
        unitsColliders = new Collider[100]; // in late 4 players each spawn about 50
        buildingsColliders = new Collider[4];
        resourcesColliders = new Collider[50]; // at max 10*10 (overall), - 3*3 (middle), - (4 * 10 - 3) (edges) 
        searchRadius = defaultSearchRadius;

        SetPlayerHealthServerRpc(BuildingLevel + 3);

        speed = BuildingLevel * 0.1f + 1.2f;
        // gatherPower(for resources) = claimPower(for buildings)
        gatherPower = BuildingLevel * 0.5f + 1;
        gatherCooldown = BuildingLevel > 0 ? BuildingLevel * 0.95f * 2 : 2;
        attackDamage = BuildingLevel * 0.2f + 1;
        attackCooldown = BuildingLevel > 0 ? BuildingLevel * 0.95f * 2 : 2;
        carryCapacity = (BuildingLevel / 5) + 2;
        directionToMoveBackToBase = getBackPosition;
        directionToMoveAfterSpawn = directionToMove;
        interactionDirection = directionToMove;
        staminaMax = (BuildingLevel / 5) + 3;

        stamina = staminaMax;

        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = unitPlayerMaterial;
        meshRenderer.materials = meshRendererMaterials;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (interactionDirection != Vector3.up)
        {
            ReCalculatePath();

            if (currentDirection != interactionDirection)
            {
                HandleObstacleAvoidance();
            }
            else
            {
                Vector3 direction = (currentDirection - transform.position).normalized;
                rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);
            }
            //if (interactionDirectionUpdateTimer <= 0)
            //{
            //    interactionDirectionUpdateTimer = interactionDirectionUpdateTimerMax;
            //}
            //else
            //{
            //    interactionDirectionUpdateTimer -= Time.fixedDeltaTime;
            //}
            //if (Vector3.Distance(transform.position, interactionDirection) > interactionDistance)
            //{
            //    ApproachInteraction();
            //}
            //else
            //{
            //    Interact();
            //}
        }
        else
        {
            //SearchForInteraction();
        }

        ////Debug.Log(unitState);
        //switch (unitState)
        //{
        //    case UnitState.Searching:
        //        SearchForClosestInteraction();
        //        break;
        //    case UnitState.Approach:
        //        ApproachClosestInteraction();
        //        break;
        //    case UnitState.Interact:
        //        Interact();
        //        break;
        //}
        //// Test if is not crucial for performance
        //UpdateUnitState();
    }

    private void ReCalculatePath()
    {
        //var unitsAround = stamina > 0 ? Physics.OverlapSphereNonAlloc(transform.position, searchRadius, unitsColliders, unitLayerMask) : 0;
        //if (unitsAround > 0) CheckNearbyUnitsForEnemies(unitsAround);

        CheckForObstacles();
    }

    private void CheckForObstacles()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, obstacleDetectionRange))
        {
            currentDirection = CalculateAvoidanceDirection(hit.normal);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!IsServer) return;

        for (int i = 0; i < directionsArray.Length; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, directionsArray[i]);
        }
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, obstacleDetectionRange);
    }

    private Vector3 CalculateAvoidanceDirection(Vector3 hitNormal)
    {
        Vector3[] testDirections = {
            Vector3.Cross(hitNormal, Vector3.up).normalized,
            -Vector3.Cross(hitNormal, Vector3.up).normalized,
            (hitNormal + Vector3.up).normalized
        };

        directionsArray = testDirections;

        foreach (Vector3 dir in testDirections)
        {
            Debug.Log($"dir -> {dir}");
            if (!Physics.Raycast(transform.position, dir, obstacleDetectionRange))
            {
                return transform.position + dir * obstacleDetectionRange * 2f;
            }
        }

        // Fallback: Move directly away from obstacle
        return transform.position - transform.forward * obstacleDetectionRange * 2f;
    }

    private void HandleObstacleAvoidance()
    {
        Vector3 direction = (currentDirection - transform.position).normalized;
        rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);

        // Return to main path when clear
        if (Vector3.Distance(transform.position, currentDirection) < 0.5f)
        {
            currentDirection = interactionDirection;
        }
    }

    private void CheckNearbyUnitsForEnemies(int unitsAround)
    {
        for (int i = 0; i < unitsAround; i++)
        {
            Unit unit = unitsColliders[i].GetComponentInParent<Unit>();
            Transform unitParentTransform = unit.GetComponent<Transform>();

            if (unit.OwnerClientId != OwnerClientId)
            {
                // Unit is considered as enemy if doesn't belong to same owner
                var distanceToEnemyUnit = Vector3.Distance(transform.position, unitParentTransform.position);
                if (interactionDirection == Vector3.up || distanceToEnemyUnit < Vector3.Distance(transform.position, interactionDirection))
                {
                    interactionDirection = unitParentTransform.position;
                    interactionUnit = unit;
                }
            }
        }
    }

    //private void UpdateUnitState()
    //{
    //    unitState = UnitState.Searching;

    //    if (interactionDirection == null && directionToMoveAfterSpawn == Vector3.zero) return;

    //    searchRadius = defaultSearchRadius;

    //    unitState = UnitState.Approach;

    //    var distance = directionToMoveAfterSpawn != Vector3.zero
    //        ? Vector3.Distance(transform.position, directionToMoveAfterSpawn)
    //        : Vector3.Distance(transform.position, interactionDirection.position);

    //    if (directionToMoveAfterSpawn != Vector3.zero && distance <= interactionDistance)
    //    {
    //        directionToMoveAfterSpawn = Vector3.zero;
    //        unitState = UnitState.Searching;
    //    }

    //    if (interactionDirection != null && distance <= interactionDistance) unitState = UnitState.Interact;
    //}

    //private void ApproachInteraction()
    //{
    //    if (directionToMoveAfterSpawn == Vector3.zero && interactionDirection == null) return;

    //    Vector3 finalDestination = directionToMoveAfterSpawn != Vector3.zero ? directionToMoveAfterSpawn : interactionDirection.position;
    //    Vector3 direction = (finalDestination - transform.position).normalized;
    //    rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * direction);

    //    //transform.position = Vector3.MoveTowards
    //    //(
    //    //    transform.position,
    //    //    directionToMoveAfterSpawn != Vector3.zero ? directionToMoveAfterSpawn : closestInteraction.position,
    //    //    speed * Time.deltaTime
    //    //);
    //}

    //private void SearchForInteraction()
    //{
    //    // FIRST SEARCH FOR OTHER UNITS, IF THERE ARE ANY ENEMIES AROUND, ATTACK
    //    // This is the only case where need to check stamina, player can be close to base and at the same time there may be an enemy Unit who followed fellow unit to Base!
    //    var unitsAround = stamina > 0 ? Physics.OverlapSphereNonAlloc(transform.position, searchRadius, unitsColliders, unitLayerMask) : 0;

    //    if (unitsAround > 0)
    //    {
    //        for (int i = 0; i < unitsAround; i++)
    //        {
    //            Collider unitCollider = unitsColliders[i];
    //            Unit unit = unitCollider.GetComponentInParent<Unit>();
    //            Transform unitParentTransform = unit.GetComponent<Transform>();

    //            if (unit.OwnerClientId != OwnerClientId)
    //            {
    //                var distanceToEnemyUnit = Vector3.Distance(transform.position, unitParentTransform.position);
    //                if (interactionDirection == null || distanceToEnemyUnit < Vector3.Distance(transform.position, interactionDirection.position))
    //                {
    //                    interactionDirection = unitParentTransform;
    //                    interactionUnit = unit;
    //                }
    //            }
    //        }
    //        // If any enemy unit is found focus it!
    //        if (interactionDirection != null) return;
    //    }

    //    // SECOND SEARCH FOR BUILDINGS, IF THERE IS UNCLAIMED BUILDING - CLAIM IT. IF BASE - UPDATE IT/RESTORE STAMINA
    //    var buildingAround = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, buildingsColliders, buildingLayerMask);

    //    if (buildingAround > 0)
    //    {
    //        for (int i = 0; i < buildingAround; i++)
    //        {
    //            Collider buildingCollider = buildingsColliders[i];
    //            Building building = buildingCollider.GetComponentInParent<Building>();
    //            Transform buildingParentTransform = building.GetComponent<Transform>();

    //            bool buildingIsNeutral = building.isNeutralBuilding;
    //            bool buildingIsOccupied = building.occupationStatus == Building.Occupation.Occupied ? true : false;
    //            bool buildingBelongsToUnitOwner = building.IsOwner;

    //            if (buildingIsNeutral && (!buildingBelongsToUnitOwner || !buildingIsOccupied) && stamina > 0)
    //            {    
    //                // Found neutral building to claim
    //                var distanceToBuilding = Vector3.Distance(transform.position, buildingParentTransform.position);
    //                if (interactionDirection == null || distanceToBuilding < Vector3.Distance(transform.position, interactionDirection.position))
    //                {
    //                    interactionDirection = buildingParentTransform;
    //                    closestInteractionBuilding = building;
    //                }
    //            }
    //            else if (buildingBelongsToUnitOwner && buildingIsOccupied && (stamina < staminaMax || carryingWeight > 0))
    //            {
    //                // Got back to base and ready to update building and restore stamina 
    //                interactionDirection = buildingParentTransform;
    //                closestInteractionBuilding = building;
    //            }
    //        }
    //        // If any building is found focus it!
    //        if (interactionDirection != null) return;
    //    }

    //    // LASTLY TRY TO FIND ANY RESOURCES NEARBY
    //    var resourcesAround = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, resourcesColliders, resourceLayerMask);

    //    if (resourcesAround > 0)
    //    {
    //        for (int i = 0; i < resourcesAround; i++)
    //        {
    //            Collider resourceCollider = resourcesColliders[i];
    //            Resource resource = resourceCollider.GetComponentInParent<Resource>();
    //            if (resource.weight.Value <= 0) continue;
    //            Transform resourceParentTransform = resource.GetComponent<Transform>();

    //            var distanceToResource = Vector3.Distance(transform.position, resourceParentTransform.position);
    //            if (interactionDirection == null || distanceToResource < Vector3.Distance(transform.position, interactionDirection.position))
    //            {
    //                interactionDirection = resourceParentTransform;
    //                closestInteractionResource = resource;
    //            }
    //        }
    //        // If any resource is found focus it!
    //        if (interactionDirection != null) return;
    //    }

    //    searchRadius += Time.deltaTime;
    //}

    //private void Interact()
    //{
    //    if (interactionDirection == null) return;

    //    if (interactionUnit != null && interactionUnit.health.Value <= 0)
    //    {
    //        interactionDirection = null;
    //        interactionUnit = null;
    //        return;
    //    }
    //    if (closestInteractionBuilding != null && !closestInteractionBuilding.IsOwner && (stamina <= 0|| carryingWeight >= carryCapacity))
    //    {
    //        interactionDirection = null;
    //        closestInteractionBuilding = null;
    //        return;
    //    }
    //    if (closestInteractionResource != null && closestInteractionResource.weight.Value <= 0)
    //    {
    //        interactionDirection = null;
    //        closestInteractionResource = null;
    //        return;
    //    }

    //    deltaTime = Time.deltaTime;
    //    stamina -= deltaTime;

    //    if (interactionUnit != null)
    //    {
    //        interactionUnit.InteractWithOtherUnitServerRpc(attackDamage * deltaTime); // other unit takes damage from this unit
    //    }
    //    else if (closestInteractionBuilding != null)
    //    {
    //        bool closestInteractionBuildingIsOccupied = closestInteractionBuilding.occupationStatus == Building.Occupation.Occupied ? true : false;
    //        if (closestInteractionBuilding.isNeutralBuilding && (!closestInteractionBuilding.IsOwner || !closestInteractionBuildingIsOccupied))
    //        {
    //            closestInteractionBuilding.InteractWithBuildingServerRpc(gatherPower * deltaTime);
    //        }
    //    }
    //    else if (closestInteractionResource != null)
    //    {
    //        closestInteractionResource.InteractWithResourceServerRpc(gatherPower * deltaTime);
    //        carryingWeight += gatherPower * deltaTime;
    //    }

    //    // If Unit is full, this will trigger Search which will lead to going back to base
    //    if (stamina <= 0 || carryingWeight >= carryCapacity)
    //    {
    //        interactionDirection = null;
    //        interactionUnit = null;
    //        closestInteractionBuilding = null;
    //        closestInteractionResource = null;
    //        directionToMoveAfterSpawn = directionToMoveBackToBase;
    //    }
    //}

    [Rpc(SendTo.Server)]
    private void InteractWithOtherUnitServerRpc(float damage)
    {
        if (health.Value > 0) health.Value -= damage;
        if (health.Value <= 0) DespawnUnit();
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerHealthServerRpc(float newHealth)
    {
        health.Value = newHealth;
    }

    private void DespawnUnit()
    {
        NetworkObject.Despawn(false);
        serverOwnerBuilding.ResetUnit(minerIndex);
        gameObject.SetActive(false);
    }
}
