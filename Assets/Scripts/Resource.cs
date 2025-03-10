using System;
using Unity.Netcode;
using UnityEngine;

public class Resource : NetworkBehaviour
{

    [SerializeField] private ResourceSO resourceData;
    public NetworkVariable<float> weight = new NetworkVariable<float>();
    private int occupiedIndex;
    public float interactionDistance;

    private bool isInteractable;

    public Action OnDespawn;
    public Action OnResourceInteraction;

    public bool IsHolyResource => resourceData.type == ResourceType.Holy;
    public bool IsCommonResource => resourceData.type == ResourceType.Common;

    public Vector3 spawnPosition { get; private set; }

    [SerializeField] MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        interactionDistance = resourceData.interactionDistance;
        GetComponentInChildren<ResourceUI>().resourceMaxWeight = resourceData.resourceWeight;
        propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];

            renderer.sharedMaterial = resourceData.material;

            renderer.GetPropertyBlock(propertyBlock);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        weight.Value = resourceData.resourceWeight;
        isInteractable = true;
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
        if (isInteractable && weight.Value > 0)
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
        isInteractable = false;

        if (resourceData.type == ResourceType.Holy)
        {
            GameManager.Instance.GameHasFinished();
        }
        
        NetworkObject.Despawn(false);
        gameObject.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) ResourceSpawner.Instance.ClearOccupiedZone(IsCommonResource, occupiedIndex, this);

        // If resource is Interaction target for any Unit, notify about its despawn, so they would change target before they completely approached it.
        OnDespawn?.Invoke();
        // Unsubscribe to avoid Memory Leaks and Ghost Callbacks
        OnDespawn = null;
    }

    public void SetOccupiedZone(int occupiedIndex, Vector3 spawnPosition)
    {
        this.occupiedIndex = occupiedIndex;
        this.spawnPosition = spawnPosition;
    }
}
