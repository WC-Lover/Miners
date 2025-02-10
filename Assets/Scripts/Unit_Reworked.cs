using System;
using Unity.Netcode;
using UnityEngine;

public class Unit_Reworked : NetworkBehaviour
{

    // MOVE
    private Vector3 moveDirection;
    private Vector3 baseOfOriginDirection;
    private float moveSpeed;
    private Rigidbody rb;

    private float health;
    private float healthMax;

    // INTERACT
    private float interactionCooldown;
    private float interactionCooldownMax;
    private int interactionAmount;
    private int interactionAmountMax;
    private float interactionPower;
    private float carryingWeight;
    private float carryingWeightMax;
    private float gatherMultiplier;
    private float claimMultiplier;
    private float attackMultiplier;

    // SEARCH
    [SerializeField] private float defaultSearchRadius;
    [SerializeField] private float searchRadius;
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private LayerMask buildingLayerMask;
    [SerializeField] private LayerMask resourceLayerMask;
    private Collider[] unitsColliders;
    private Collider[] buildingsColliders;
    private Collider[] resourcesColliders;

    // MATERIALS
    [SerializeField] private Material unitPlayerMaterial;
    [SerializeField] private Material defaultUnitMaterial;

    private Building serverOwnerBuilding;
    private int unitIndex;

    private UnitActionStatus unitActionStatus;

    private enum UnitActionStatus
    {
        Search,
        Approach,
        Interact
    }

    public void SetOwnerBuildingForServer(Building ownerBuilding)
    {
        serverOwnerBuilding = ownerBuilding;
    }

    [Rpc(SendTo.Owner)]
    public void InitializeOwnerRpc(int buildingLevel, Vector3 getBackPosition, Vector3 directionToMove, int unitIndex)
    {
        unitActionStatus = UnitActionStatus.Approach;
        this.unitIndex = unitIndex;
        // SEARCH & COLLISION
        rb = GetComponent<Rigidbody>();
        unitsColliders = new Collider[200]; // in late 4 players each spawn about 50
        buildingsColliders = new Collider[8];
        resourcesColliders = new Collider[100]; // at max 10*10 (overall), - 3*3 (middle), - (4 * 10 - 3) (edges) 
        searchRadius = defaultSearchRadius;
        // ATTRIBUTES
        healthMax = buildingLevel + 3;
        moveSpeed = buildingLevel * 0.1f + 1f;
        gatherMultiplier = buildingLevel * 0.5f + 1;
        claimMultiplier = buildingLevel * 0.5f + 1;
        attackMultiplier = buildingLevel * 0.2f + 1;
        carryingWeightMax = (buildingLevel / 5) + 2;
        baseOfOriginDirection = getBackPosition;
        moveDirection = directionToMove;
        interactionPower = (buildingLevel * 0.2f) + 1;
        interactionAmountMax = (buildingLevel / 5) + 3;

        interactionCooldownMax = 1.2f;
        carryingWeight = 0;
        health = healthMax;
        interactionAmount = interactionAmountMax;
        // MATERIALS
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = unitPlayerMaterial;
        meshRenderer.materials = meshRendererMaterials;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsOwner && collision.collider == null && interactionCooldown > 0) return;

        unitActionStatus = UnitActionStatus.Interact;

        bool interacted = false;

        Unit_Reworked unit = collision.collider.GetComponentInParent<Unit_Reworked>();

        if (unit != null)
        {
            if (interactionAmount > 0 && !unit.IsOwner)
            {
                // ATTACK OTHER UNIT
                unit.InteractWithOtherUnitOwnerRpc(interactionPower * attackMultiplier);
                interacted = true;
                interactionAmount--;
                Debug.Log("Interact Unit");
            }
        }
        else 
        {
            Building building = collision.collider.GetComponentInParent<Building>();

            if (building != null)
            {
                // GET BACK TO/CLAIM NEUTRAL BUILDING
                if (building.isNeutralBuilding
                    && (!building.IsOwner || building.occupationStatus == Building.Occupation.Empty)
                    && interactionAmount > 0)
                {
                    // CLAIM NEUTRAL BUILDING
                    building.InteractWithBuildingServerRpc(interactionPower * claimMultiplier);
                    interacted = true;
                    interactionAmount--;
                    Debug.Log("Interact Neutral Building");
                }
                else if (building.IsOwner && building.occupationStatus == Building.Occupation.Occupied)
                {
                    // RESTORE INTERACTIONS AMOUNT
                    building.BuildingGainXP(carryingWeight);
                    interacted = true;
                    interactionAmount = interactionAmountMax;
                    carryingWeight = 0;
                    unitActionStatus = UnitActionStatus.Search;
                    Debug.Log("Interact Owner Building");
                }
            }
            else
            {
                Resource resource = collision.collider.GetComponentInParent<Resource>();

                if (resource != null && interactionAmount > 0)
                {
                    // GATHER RESOURCE
                    resource.InteractWithResourceServerRpc(interactionPower * gatherMultiplier);
                    carryingWeight += interactionPower * gatherMultiplier;
                    if (carryingWeight >= carryingWeightMax) carryingWeight = carryingWeightMax;
                    interacted = true;
                    interactionAmount--;
                    Debug.Log("Interact Resource");
                }
            }

        }

        if (interacted) interactionCooldown = interactionCooldownMax; 

        if (interactionAmount <= 0 || carryingWeight == carryingWeightMax)
        {
            moveDirection = baseOfOriginDirection;
            unitActionStatus = UnitActionStatus.Approach;
            Debug.Log("Unit needs to restore interactions amount");
        }
    }

    [Rpc(SendTo.Owner)]
    private void InteractWithOtherUnitOwnerRpc(float damage)
    {
        health -= damage;
        if (health <= 0) UnitHasDiedServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void UnitHasDiedServerRpc()
    {
        NetworkObject.Despawn(false);
        serverOwnerBuilding.ResetUnit(unitIndex);
        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (interactionCooldown > 0) interactionCooldown -= Time.fixedDeltaTime;

        switch (unitActionStatus)
        {
            case UnitActionStatus.Search:
                Search();
                break;
            case UnitActionStatus.Approach:
                Approach();
                break;
        }
    }

    private void Search()
    {
        // FIND ANY ENEMY UNITS
        int unitsAround = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, unitsColliders, unitLayerMask);
        if (unitsAround > 0)
        {
            for (int i = 0; i < unitsAround; i++)
            {
                Unit_Reworked unit = unitsColliders[i].GetComponentInParent<Unit_Reworked>();

                if (unit.IsOwner) continue;

                // Approach and attack closest enemy
                float distanceToOtherUnit = Vector3.Distance(transform.position, unit.transform.position);
                float distanceToMoveDirection = moveDirection != Vector3.up ? Vector3.Distance(transform.position, moveDirection) : -1;
                if (distanceToMoveDirection == -1 || distanceToOtherUnit < distanceToMoveDirection)
                {
                    unitActionStatus = UnitActionStatus.Approach;
                    moveDirection = unit.transform.position;
                }
            }

            if (moveDirection != Vector3.up) return;
        }

        // FIND BUILDING TO CLAIM/GET BACK TO
        int buildingsAround = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, buildingsColliders, buildingLayerMask);
        if (buildingsAround > 0)
        {
            for (int i = 0; i < buildingsAround; i++)
            {
                Building building = buildingsColliders[i].GetComponentInParent<Building>();

                float distanceToBuilding = Vector3.Distance(transform.position, building.transform.position);
                float distanceToMoveDirection = moveDirection != Vector3.up ? Vector3.Distance(transform.position, moveDirection) : -1;

                if (building.IsOwner && building.occupationStatus == Building.Occupation.Occupied)
                {
                    // (OwnerBuilding) Get back and restore interactions amount
                    if (interactionAmount <= 0 || carryingWeight >= carryingWeightMax)
                    {
                        unitActionStatus = UnitActionStatus.Approach;
                        moveDirection = building.transform.position;
                        continue;
                    }
                }


                if (building.isNeutralBuilding && distanceToMoveDirection == -1 || distanceToBuilding < distanceToMoveDirection)
                {
                    // (NeutralBuilding) Approach and claim closest building
                    unitActionStatus = UnitActionStatus.Approach;
                    moveDirection = building.transform.position;
                }
            }

            if (moveDirection != Vector3.up) return;
        }

        // FIND RESOURCE TO GATHER
        int resourcesAround = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, resourcesColliders, resourceLayerMask);
        if (resourcesAround > 0)
        {
            for (int i = 0; i < resourcesAround; i++)
            {
                Resource resource = resourcesColliders[i].GetComponentInParent<Resource>();

                float distanceToResource = Vector3.Distance(transform.position, resource.transform.position);
                float distanceToMoveDirection = moveDirection != Vector3.up ? Vector3.Distance(transform.position, moveDirection) : -1;

                if (distanceToMoveDirection == -1 || distanceToResource < distanceToMoveDirection)
                {
                    unitActionStatus = UnitActionStatus.Approach;
                    moveDirection = resource.transform.position;
                }
            }

            if (moveDirection != Vector3.up) return;
        }

        searchRadius += Time.fixedDeltaTime;
    }

    private void Approach()
    {
        if (moveDirection == Vector3.up) return;
        if (Vector3.Distance(transform.position, moveDirection) >= 0.05f)
        {
            Vector3 directionNormalized = (moveDirection - transform.position).normalized;
            //rb.AddForce(moveSpeed * directionNormalized, ForceMode.VelocityChange);
            rb.linearVelocity = directionNormalized * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}
