using Unity.Netcode;

namespace Assets.Scripts.Unit
{
    public class UnitNetwork : NetworkBehaviour
    {
        private Unit unit;

        private NetworkVariable<bool> IsAlive = new NetworkVariable<bool>();
        public void SetAlive(bool alive) => IsAlive.Value = alive;
        public bool Alive => IsAlive.Value;

        private void Awake()
        {
            unit = GetComponent<Unit>();
        }

        #region Accesible from outside RPCs

        public void InitializeUnit(UnitSpawnData unitSpawnData)
        {
            InitializeUnitOwnerRpc(unitSpawnData);
        }
        public void InteractWithUnit(float damage, ulong killerClientId)
        {
            InteractWithUnitOwnerRpc(damage, killerClientId);
        }
        public void GiveResourceToKiller(float holyResourceWeight, float resourceWeight, ulong killerClientId, ulong victimClientId)
        {
            GiveResourceToKillerEveryoneRpc(holyResourceWeight, resourceWeight, killerClientId, victimClientId);
        }
        public void DespawnUnit(float holyResourceWeight, float resourceWeight, ulong killerClientId, ulong victimClientId)
        {
            DespawnUnitServerRpc(holyResourceWeight, resourceWeight, killerClientId, victimClientId);
        }
        public void AnyResourceGathered()
        {
            AnyResourceGatheredNotOwnerRpc();
        }
        public void HolyResourceGathered()
        {
            HolyResourceGatheredNotOwnerRpc();
        }
        public void UnloadResource()
        {
            UnloadResourceNotOwnerRpc();
        }

        #endregion

        #region RPCs

        /// <summary>
        /// <para>Initializes the unit on the owner's client.</para>
        /// <para>This RPC is sent only to the owner of the unit to ensure proper initialization and synchronization of unit properties.</para>
        /// </summary>
        /// <param name="buildingLevel">The level of the building that spawned the unit.</param>
        /// <param name="baseOfOriginPosition">The position of the base where the unit originated.</param>
        /// <param name="firstDestinationPosition">The first destination the unit will move to after spawning.</param>
        [Rpc(SendTo.Owner)]
        private void InitializeUnitOwnerRpc(UnitSpawnData unitSpawnData)
        {
            unit.SetUnit(unitSpawnData);
        }

        /// <summary>
        /// <para>Handles interactions with the unit on the owner's client.</para>
        /// <para>This RPC is sent only to the owner to process damage and handle units' death.</para>
        /// </summary>
        /// <param name="damage">The amount of damage dealt to the unit.</param>
        /// <param name="killerClientId">The client ID of the player who dealt the killing blow.</param>
        [Rpc(SendTo.Owner)]
        private void InteractWithUnitOwnerRpc(float damage, ulong killerClientId)
        {
            if (unit.IsAlive)
            {
                if (unit.ModifyHealth(-damage) == 0)
                {
                    if (unit.GetHolyResourceWeight > 0 || unit.GetResourceWeight > 0)
                    {
                        GiveResourceToKiller(
                            unit.GetHolyResourceWeight,
                            unit.GetResourceWeight,
                            killerClientId,
                            NetworkManager.Singleton.LocalClientId
                            );
                    }
                    else
                    {
                        DespawnUnit(unit.GetHolyResourceWeight, unit.GetResourceWeight, killerClientId, NetworkManager.Singleton.LocalClientId);
                    }
                }
            }
        }

        /// <summary>
        /// <para>Distributes resources to the killer of the unit via everyone RPC.</para>
        /// <para>This RPC ensures that the killer receives resources for eliminating the unit.</para>
        /// </summary>
        /// <param name="holyResourceWeight">The amount of holy resource to be awarded to the killer.</param>
        /// <param name="resourceWeight">The amount of standard resource to be awarded to the killer.</param>
        /// <param name="killerClientId">The client ID of the player who killed the unit.</param>
        [Rpc(SendTo.Everyone)]
        private void GiveResourceToKillerEveryoneRpc(float holyResourceWeight, float resourceWeight, ulong killerClientId, ulong victimClientId)
        {
            if (NetworkManager.Singleton.LocalClientId == killerClientId)
            {
                if (unit.GetHolyResourceWeight == 0 && holyResourceWeight > 0)
                {
                    // Invoke Action to show obtained holy resource
                    HolyResourceGathered();
                }
                else if (unit.GetResourceWeight == 0 && resourceWeight > 0)
                {
                    // Invoke Action to show obtained resource
                    AnyResourceGathered();
                }
                unit.AddResource(resourceWeight - holyResourceWeight);
                unit.AddHolyResource(holyResourceWeight);

                DespawnUnit(holyResourceWeight, resourceWeight, killerClientId, victimClientId);
            }
        }

        /// <summary>
        /// <para>Despawns the unit on all clients, ensuring synchronization across the network.</para>
        /// <para>This RPC is sent to all clients to remove the unit from the game world.</para>
        /// </summary>
        [Rpc(SendTo.Server)]
        private void DespawnUnitServerRpc(float holyResourceWeight, float resourceWeight, ulong killerClientId, ulong victimClientId)
        {
            if (holyResourceWeight > 0)
            {
                GameManager.Instance.UpdatePlayerHolyResourceData(-holyResourceWeight, victimClientId);
                GameManager.Instance.UpdatePlayerHolyResourceData(holyResourceWeight, killerClientId);
            }

            NetworkObject.Despawn(false);
        }

        /// <summary>
        /// <para>Notifies non-owner clients that any resource has been obtained by the Unit.</para>
        /// <para>This RPC is used to update non-owner clients about resource changes without affecting the owner.</para>
        /// </summary>
        [Rpc(SendTo.NotOwner)]
        private void AnyResourceGatheredNotOwnerRpc()
        {
            // Invoke Action to enable AnyResourceIndicator UI
            unit.DelegateManager.OnResourceGathered?.Invoke(false);
        }

        /// <summary>
        /// <para>Notifies non-owner clients that holy resource has been obtained by the Unit.</para>
        /// <para>This RPC is used to update non-owner clients about resource changes without affecting the owner.</para>
        /// </summary>
        [Rpc(SendTo.NotOwner)]
        private void HolyResourceGatheredNotOwnerRpc()
        {
            // Invoke Action to enable HolyResourceIndicator UI
            unit.DelegateManager.OnResourceGathered?.Invoke(true);
        }

        /// <summary>
        /// <para>Notifies all clients(except Owner) that resources have been unloaded.</para>
        /// <para>This RPC ensures that all clients are synchronized regarding resource changes.</para>
        /// </summary>
        [Rpc(SendTo.NotOwner)]
        private void UnloadResourceNotOwnerRpc()
        {
            // Invoke Action to disable Holy/Any ResourceIndicator UI
            unit.DelegateManager.OnResourceUnload?.Invoke();
        }

        #endregion
    }
}