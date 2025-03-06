using UnityEngine;

namespace Assets.Scripts.Unit
{
    public struct UnitConfig
    {
        [Header("Health")]
        public float HealthMax;
        [Space]

        [Header("Movement")]
        public float Speed;
        public float ObstacleDetectionDistance;
        [Space]

        [Header("Interaction")]
        public float InteractionCooldown;
        public float InteractionCooldownMax;
        public InteractionTarget InteractionTarget;
        // Maybe add GatherStaminaDrain
        // AttackStaminaDrain etc.
        public float StaminaMax;
        [Space]

        [Header("Interaction Distance")]
        public float UnitInteractionDistance;
        public float BuildingInteractionDistance;
        public float ResourceInteractionDistance;
        public float HolyResourceInteractionDistance;
        [Space]

        [Header("Unit Interaction")]
        public float AttackDamage;
        [Space]

        [Header("Resource Interaction")]
        public float GatherWeight;
        public float ResourceWeight;
        public float HolyResourceWeight;
        public float ResourceWeightMax;
        [Space]

        [Header("Building Interaction")]
        public float CaptureStrength;
        [Space]

        [Header("Search")]
        public float DefaultSearchRadius;
        public float CurrentSearchRadius;
        public int MaxUnits;
        public int MaxBuildings;
        public int MaxResources;
        public LayerMask UnitLayerMask;
        public LayerMask BuildingLayerMask;
        public LayerMask ResourceLayerMask;
        [Space]

        [Header("After Spawn")]
        public Vector3 FirstDestinationPosition;
        public Vector3 BaseOfOriginPosition;
    }

    public class InteractionTarget
    {
        public Vector3 Position;
        public Unit Unit;
        public Building Building;
        public Resource Resource;
        public float InteractionDistance;
        public InteractionType Interaction = InteractionType.None;

        public enum InteractionType{
            Attack,
            Gather,
            Unload,
            Capture,
            Steal,
            None
        }
    }
}