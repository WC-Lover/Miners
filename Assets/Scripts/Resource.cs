using System;
using Unity.Netcode;
using UnityEngine;

public class Resource : NetworkBehaviour
{

    public NetworkVariable<float> weight = new NetworkVariable<float>();
    [SerializeField] private ResourceSO resourceData;
    private int occupiedIndex;
    public float interactionDistance;

    public Action OnResourceDespawn;
    public Action OnResourceInteraction;

    public void SetResourceWeight() => weight.Value = resourceData.baseValue;
    public bool IsHolyResource => resourceData.type == ResourceType.Holy;
    public bool IsCommonResource => resourceData.type == ResourceType.Common;

    public override void OnNetworkSpawn()
    {
        GetComponentInChildren<ResourceUI>().resourceMaxWeight = resourceData.baseValue;

        // Change material for the Resource
        //var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        //var meshRendererMaterials = new Material[1];
        //meshRendererMaterials[0] = resourceData.material;
        //for (int i = 0; i < meshRenderers.Length; i++)
        //{
        //    meshRenderers[i].materials = meshRendererMaterials;
        //}

        //rangeOfInteraction = resourceData.interactionRange;
    }

    public enum ResourceType
    {
        Common,
        Rare,
        Holy
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractWithResourceServerRpc(float gatherWeight, ServerRpcParams serverRpcParams = default)
    {        
        if (weight.Value > 0)
        {
            OnResourceInteraction?.Invoke();
            float resourceWeightGathered = weight.Value > gatherWeight ? gatherWeight : weight.Value;
            weight.Value -= resourceWeightGathered;
            if (resourceData.type == ResourceType.Holy) GameManager.Instance.UpdatePlayerHolyResourceData(resourceWeightGathered, serverRpcParams.Receive.SenderClientId);
            if (weight.Value == 0) ResourceHasBeenGathered();
        }
    }

    private void ResourceHasBeenGathered()
    {
        // If resource is Interaction target for any Unit, notify about its despawn, so they would change target before they completely approached it.
        NotifyClientsResourceDespawnEverybodyRpc();

        if (resourceData.type == ResourceType.Holy)
        {
            GameManager.Instance.GameHasFinished();
        }
        else
        {
            ResourceSpawner.Instance.ClearOccupiedZone(this, occupiedIndex);
        }
        
        NetworkObject.Despawn(false);
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsResourceDespawnEverybodyRpc()
    {
        // Unsubscribe to avoid Memory Leaks and Ghost Callbacks
        OnResourceDespawn?.Invoke();
        OnResourceDespawn = null;
    }

    public void SetOccupiedZone(int occupiedIndex)
    {
        this.occupiedIndex = occupiedIndex;
    }
}
