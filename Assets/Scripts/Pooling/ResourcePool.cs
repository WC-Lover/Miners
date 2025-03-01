using System.Collections.Generic;
using UnityEngine;

public class ResourcePool : MonoBehaviour
{
    private Dictionary<Resource, List<Queue<Resource>>> poolListDictionary = new Dictionary<Resource, List<Queue<Resource>>>();

    public void PredefineUnitPoolByHost(Resource resourcePrefab, int index, int resourcePoolSize)
    {
        if (poolListDictionary.TryGetValue(resourcePrefab, out var poolList))
        {
            poolList.Add(new Queue<Resource>(resourcePoolSize));
        }
        else
        {
            poolListDictionary[resourcePrefab] = new List<Queue<Resource>>();
            poolListDictionary[resourcePrefab].Add(new Queue<Resource>(resourcePoolSize));
        }
        
        Queue<Resource> resourceQ =  poolListDictionary[resourcePrefab][index];
        for (int i = 0; i < resourcePoolSize; i++)
        {
            Resource resource = Instantiate(resourcePrefab);
            resource.gameObject.SetActive(false);
            resourceQ.Enqueue(resource);
        }
    }

    public Resource GetResource(Resource resourcePrefab, int index)
    {
        Resource retResource = null;

        if (poolListDictionary.TryGetValue(resourcePrefab, out var resources))
        {
            Queue<Resource> resourceQ = resources[index];
            if (resourceQ.Count > 0) retResource = resourceQ.Dequeue();
        }

        return retResource;
    }

    public void ReturnResource(Resource resourcePrefab, Resource resource, int index)
    {
        resource.gameObject.SetActive(false);
        
        poolListDictionary[resourcePrefab][index].Enqueue(resource);
    }
}
