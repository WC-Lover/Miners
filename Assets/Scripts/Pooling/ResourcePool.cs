using System.Collections.Generic;
using UnityEngine;

public class ResourcePool : MonoBehaviour
{
    private Dictionary<bool, List<Queue<Resource>>> poolListDictionary = new Dictionary<bool, List<Queue<Resource>>>();

    public void PredefineUnitPoolByHost(bool isCommonResource, ref Resource resourcePrefab, int zoneIndex, int resourcePoolSize)
    {
        // Add Queue to poolList
        if (poolListDictionary.TryGetValue(isCommonResource, out var poolList))
        {
            poolList.Add(new Queue<Resource>(resourcePoolSize));
        }
        else
        {
            var resourceQueueList = new List<Queue<Resource>>();
            resourceQueueList.Add(new Queue<Resource>(resourcePoolSize));
            poolListDictionary[isCommonResource] = resourceQueueList;
        }
        
        // Fill Queue with resource
        Queue<Resource> resourceQ =  poolListDictionary[isCommonResource][zoneIndex];
        for (int i = 0; i < resourcePoolSize; i++)
        {
            Resource resource = Instantiate(resourcePrefab);
            resource.gameObject.SetActive(false);
            resourceQ.Enqueue(resource);
        }
    }

    public Resource GetResource(bool isCommonResource, int zoneIndex)
    {
        if (poolListDictionary.TryGetValue(isCommonResource, out var resources))
        {
            Queue<Resource> resourceQ = resources[zoneIndex];
            if (resourceQ.Count > 0) return resourceQ.Dequeue();
        }

        return null;
    }

    public void ReturnResource(bool isCommonResource, int zoneIndex, Resource resource)
    {
        resource.gameObject.SetActive(false);
        
        poolListDictionary[isCommonResource][zoneIndex].Enqueue(resource);
    }
}
