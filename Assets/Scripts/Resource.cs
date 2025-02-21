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
    public float rangeOfInteraction;
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

        if (resourceType == ResourceType.Common)
        {
            rangeOfInteraction = 0.27f; // 0.125(UnitWidth / 2) + 0.1(CommonResourceWidth / 2) + 0.035
            if (IsServer) weight.Value = 3;
        }
        else if (resourceType == ResourceType.Rare)
        {
            rangeOfInteraction = 0.31f; // 0.125(UnitWidth / 2) + 0.15(RareResourceWidth / 2) + 0.035
            if (IsServer) weight.Value = 6;
        }
        else if (resourceType == ResourceType.Holy)
        {
            rangeOfInteraction = 0.66f; // 0.125(UnitWidth / 2) + 0.5(HolyResourceWidth / 2) + 0.035
            if (IsServer) weight.Value = 200;
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
        NotifyClientsResourceDespawnEverybodyRpc();

        if (resourceType == ResourceType.Rare)
        {
            // Apply buff to unit who gathered the resource?
        }
        if (resourceType == ResourceType.Holy)
        {
            Debug.Log("Winner");
        }
        ResourceSpawner.Instance.ClearOccupiedZone(occupiedIndex, occupiedZoneIndex);

        // If resource is Interaction target for any Unit, notify about its despawn, so they would change target before they completely approached it.
        
        NetworkObject.Despawn(false);
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyClientsResourceDespawnEverybodyRpc()
    {
        OnResourceDespawn?.Invoke(this, EventArgs.Empty);
    }

    public void SetOccupiedZone(int occupiedIndex, int occupiedZoneIndex)
    {
        this.occupiedZoneIndex = occupiedZoneIndex;
        this.occupiedIndex = occupiedIndex;
    }
}
