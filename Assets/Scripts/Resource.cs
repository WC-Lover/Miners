using System;
using Unity.Netcode;
using UnityEngine;

public class Resource : NetworkBehaviour
{

    public NetworkVariable<float> weight = new NetworkVariable<float>();
    [SerializeField] private ResourceSO resourceData;
    private int occupiedZoneIndex;
    private int occupiedIndex;
    public float rangeOfInteraction;
    public EventHandler OnResourceDespawn;
    public EventHandler OnResourceInteraction;

    public void SetResourceWeight() => weight.Value = resourceData.baseValue;

    public bool IsHolyResource() => resourceData.type == ResourceType.Holy;

    public override void OnNetworkSpawn()
    {
        GetComponentInChildren<ResourceUI>().resourceMaxWeight = resourceData.baseValue;

        // Change material for the Resource
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = resourceData.material;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }

        rangeOfInteraction = resourceData.interactionRange;
    }

    public enum ResourceType
    {
        Common,
        Rare,
        Holy
    }

    [Rpc(SendTo.Server)]
    public void InteractWithResourceServerRpc(float gatherWeight)
    {
        if (weight.Value - gatherWeight > 0)
        {
            OnResourceInteraction?.Invoke(this, EventArgs.Empty);
            weight.Value -= gatherWeight;
        }
        else ResourceHasBeenGathered();
    }

    private void ResourceHasBeenGathered()
    {
        // If resource is Interaction target for any Unit, notify about its despawn, so they would change target before they completely approached it.
        NotifyClientsResourceDespawnEverybodyRpc();

        if (resourceData.type == ResourceType.Holy)
        {
            //GameManager.Instance.GameHasFinished();
        }
        else
        {
            ResourceSpawner.Instance.ClearOccupiedZone(occupiedIndex, occupiedZoneIndex);
        }
        
        NetworkObject.Despawn(false);
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsResourceDespawnEverybodyRpc()
    {
        // Unsubscribe to avoid Memory Leaks and Ghost Callbacks
        OnResourceDespawn?.Invoke(this, EventArgs.Empty);
        OnResourceDespawn = null;
    }

    public void SetOccupiedZone(int occupiedIndex, int occupiedZoneIndex)
    {
        this.occupiedZoneIndex = occupiedZoneIndex;
        this.occupiedIndex = occupiedIndex;
    }
}
