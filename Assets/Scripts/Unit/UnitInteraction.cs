using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Unit
{
    public class UnitInteraction : MonoBehaviour
    {
        private Unit unit;

        private void Awake()
        {
            unit = GetComponent<Unit>();
        }

        public InteractionTarget PerformInteraction(InteractionTarget target)
        {
            if (target.Interaction == InteractionTarget.InteractionType.Unload)
            {
                Unload(target.Building);
                return null;
            }

            if (!CanInteract(target)) return null;

            switch (target.Interaction)
            {
                case (InteractionTarget.InteractionType.Attack):
                    Attack(target.Unit);
                    break;
                case (InteractionTarget.InteractionType.Capture):
                    Capture(target.Building);
                    break;
                case (InteractionTarget.InteractionType.Steal):
                    Steal(target.Building);
                    break;
                case (InteractionTarget.InteractionType.Gather):
                    Gather(target.Resource);
                    break;
            }

            unit.ModifyStamina(-1);

            return target;
        }

        private bool CanInteract(InteractionTarget target)
        {
            bool canInteract = unit.CanInteract && target != null;

            if (target.Unit != null) canInteract &= target.Unit.Network.Alive;
            if (target.Resource != null) canInteract &= target.Resource.weight.Value > 0;

            if (canInteract) canInteract &= Vector3.Distance(transform.position, target.Position) <= target.InteractionDistance;
            
            return canInteract;
        }
        private void Attack(Unit unit)
        {
            unit.Network.InteractWithUnit(this.unit.Config.AttackDamage, NetworkManager.Singleton.LocalClientId);
        }
        private void Capture(Building building)
        {
            building.InteractWithNeutralBuildingServerRpc(unit.Config.CaptureStrength);
        }
        private void Steal(Building building)
        {
            // Create Server Rpc in Building(Non-Neutral) for stealing. 
        }
        private void Gather(Resource resource)
        {
            resource.InteractWithResourceServerRpc(unit.Config.GatherWeight);
            
            if (resource.IsHolyResource) unit.AddHolyResource(unit.Config.GatherWeight);
            else unit.AddResource(unit.Config.GatherWeight);
        }
        private void Unload(Building building)
        {
            // When unload trigger action to start coroutine that refills stamina
            building.BuildingGainXP(unit.GetResourceWeight, unit.GetHolyResourceWeight);
            unit.UnloadResources();
        }
    }
}
