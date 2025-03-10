using UnityEngine;

namespace Assets.Scripts.Unit
{
    public class UnitSearch : MonoBehaviour
    {
        private Unit unit;

        private Collider[] unitColliders;
        private Collider[] buildingColliders;
        private Collider[] resourceColliders;

        private void Awake()
        {
            unit = GetComponent<Unit>();

            unit.DelegateManager.OnUnitSetUp += DelegateManager_OnUnitSetUp;
        }

        private void DelegateManager_OnUnitSetUp()
        {
            unitColliders = new Collider[unit.Config.MaxUnits];
            buildingColliders = new Collider[unit.Config.MaxBuildings];
            resourceColliders = new Collider[unit.Config.MaxResources];

            unit.DelegateManager.OnUnitSetUp -= DelegateManager_OnUnitSetUp;
        }

        public bool FindNearestTarget(out InteractionTarget interactionTarget)
        {
            if (!unit.CanInteract)
            {
                // Get back to base of origin and unload resources
                interactionTarget = new InteractionTarget
                {
                    Position = unit.Config.BaseOfOriginPosition,
                    Building = Building.Instance,
                    InteractionDistance = unit.Config.BuildingInteractionDistance,
                    Interaction = InteractionTarget.InteractionType.Unload
                };
            }
            else
            {
                interactionTarget = SearchForUnit() ??
                        SearchForBuilding() ??
                        SearchForResource();
            }

            return interactionTarget != null;
        }

        private InteractionTarget SearchForUnit()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, unit.GetCurrentSearchRadius, unitColliders, unit.Config.UnitLayerMask);

            InteractionTarget target = null;
            float nearestDistance = float.MaxValue;

            // theoretically can add here OrderBy(distance) for unitColliders and return as soon as find Enemy Unit

            for (int i = 0; i < count; i++)
            {
                Unit unit = unitColliders[i].GetComponentInParent<Unit>();
                if (unit != null && unit.OwnerClientId != this.unit.OwnerClientId)
                {
                    float distance = Vector3.Distance(transform.position, unit.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        target = new InteractionTarget
                        {
                            Unit = unit,
                            Position = unit.transform.position,
                            InteractionDistance = this.unit.Config.UnitInteractionDistance,
                            Interaction = InteractionTarget.InteractionType.Attack,
                        };
                        unit.DelegateManager.OnDespawn += this.unit.DelegateManager.OnInteractionTargetDespawn;
                    }
                }
            }

            return target;
        }

        private InteractionTarget SearchForBuilding()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, unit.GetCurrentSearchRadius, buildingColliders, unit.Config.BuildingLayerMask);

            InteractionTarget target = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Building building = buildingColliders[i].GetComponentInParent<Building>();

                if (building == null) continue;

                bool buildingBelongsToUnitOwner = building.OwnerClientId == unit.OwnerClientId;
                bool isBuildingOccupied = building.occupationStatus == Building.Occupation.Occupied;

                float distance = Vector3.Distance(transform.position, building.transform.position);

                if (distance < nearestDistance)
                {
                    target = new InteractionTarget();

                    if (!building.isNeutralBuilding && !buildingBelongsToUnitOwner)
                    {
                        // Steal Holy Resource from enemy
                        target.Interaction = InteractionTarget.InteractionType.Steal;
                    }
                    else if (!buildingBelongsToUnitOwner || !isBuildingOccupied)
                    {
                        // Capture NeutralBuilding
                        target.Interaction = InteractionTarget.InteractionType.Capture;
                    }
                    else
                    {
                        // Ignore Buildings that have similar Owner as Unit
                        target = null;
                        continue;
                    }

                    nearestDistance = distance;
                    target.Building = building;
                    target.Position = building.transform.position;
                    target.InteractionDistance = unit.Config.BuildingInteractionDistance;
                }
            }

            return target;
        }

        private InteractionTarget SearchForResource()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, unit.GetCurrentSearchRadius, resourceColliders, unit.Config.ResourceLayerMask);

            InteractionTarget target = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Resource resource = resourceColliders[i].GetComponentInParent<Resource>();

                if (resource != null)
                {
                    float distance = Vector3.Distance(transform.position, resource.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        target = new InteractionTarget
                        {
                            Resource = resource,
                            Position = resource.transform.position,
                            InteractionDistance = resource.interactionDistance,
                            Interaction = InteractionTarget.InteractionType.Gather
                        };
                        resource.OnDespawn += unit.DelegateManager.OnInteractionTargetDespawn;
                    }
                }
            }

            return target;
        }
    }
}
