using System;
using Unity.Netcode;
using UnityEngine;

public class Resource : NetworkBehaviour
{

    public NetworkVariable<float> weight = new NetworkVariable<float>();
    [SerializeField] private ResourceType resourceType;
    private int occupiedZoneIndex;
    private int occupiedIndex;
    [SerializeField] private Material resourceMaterial;
    public EventHandler OnResourceDespawn;

    public override void OnNetworkSpawn()
    {
        // Change material for the Resource
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        var meshRendererMaterials = new Material[1];
        meshRendererMaterials[0] = resourceMaterial;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].materials = meshRendererMaterials;
        }

        if (!IsServer) return;

        if (resourceType == ResourceType.Common)
        {
            weight.Value = 3;
        }
        else if (resourceType == ResourceType.Rare)
        {
            weight.Value = 6;
        }
        else if (resourceType == ResourceType.Holy)
        {
            weight.Value = 200;
        }
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
        if (weight.Value - gatherWeight > 0) weight.Value -= gatherWeight;
        else ResourceHasBeenGathered();
    }

    private void ResourceHasBeenGathered()
    {
        if (resourceType == ResourceType.Rare)
        {
            // Apply buff to unit who gathered the resource?
        }
        if (resourceType == ResourceType.Holy)
        {
            Debug.Log("Winner");
        }
        Debug.Log("Despawning");
        ResourceSpawner.Instance.ClearOccupiedZone(occupiedIndex, occupiedZoneIndex);

        // If resource is Interaction target for any Unit, notify about its despawn, so they would change target before they completely approached it.
        OnResourceDespawn?.Invoke(this, EventArgs.Empty);
        
        NetworkObject.Despawn(false);
        gameObject.SetActive(false);
    }

    public void SetOccupiedZone(int occupiedIndex, int occupiedZoneIndex)
    {
        this.occupiedZoneIndex = occupiedZoneIndex;
        this.occupiedIndex = occupiedIndex;
    }
}
