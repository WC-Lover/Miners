using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ResourceSpawner : MonoBehaviour
{
    public static ResourceSpawner Instance;

    [SerializeField] private Resource commonResourcePrefab;
    [SerializeField] private Resource rareResourcePrefab;
    [SerializeField] private Transform holyResourcePrefab;

    [SerializeField] private ResourcePool resourcePool;

    [SerializeField] private float resourceSpawnTime;
    [SerializeField] private float resourceSpawnTimeMax;
    private bool allOccupied = false;
    private bool gameHasStarted = false;

    // COMMON/RARE RESOURCE ZONE BETWEEN PLAYERS LISTS
    private List<Vector3> resourcePositionListZoneBetween;
    // PLAYERS' COMMON RESOURCE ZONE 1-4 LISTS
    private List<Vector3> resourcePositionListZoneOne;
    private List<Vector3> resourcePositionListZoneTwo;
    private List<Vector3> resourcePositionListZoneThree;
    private List<Vector3> resourcePositionListZoneFour;


    private void Awake()
    {
        Instance = this;

        resourceSpawnTimeMax = 10;
        //resourceSpawnTimeMax = 0;
        resourceSpawnTime = 0;


        // COMMON/RARE RESOURCE ZONE BETWEEN PLAYERS LISTS
        resourcePositionListZoneBetween = new List<Vector3>();

        // PLAYERS' COMMON RESOURCE ZONE 1-4 LISTS
        resourcePositionListZoneOne = new List<Vector3>();
        resourcePositionListZoneTwo = new List<Vector3>();
        resourcePositionListZoneThree = new List<Vector3>();
        resourcePositionListZoneFour = new List<Vector3>();

        for (int i = -4; i < 5; i++)
        {
            for (int j = -4; j < 5; j++)
            {
                // UNAVAILABLE FOR SPAWN ZONES (buildings, holyResource)
                if ((i == -4 || i == 4) && (j == -4 || j == 4)) continue;
                if (Math.Abs(i) <= 1 && Math.Abs(j) <= 1) continue;
                if ((i == -4 || i == 4 || j == -4 || j == 4) && (i == 0 || j == 0)) continue;

                
                Vector3 resourcePosition = new Vector3(i, 0, j);

                if (i == 0 || j == 0)
                {
                    // ZONE IN BETWEEN PLAYERS
                    resourcePositionListZoneBetween.Add(resourcePosition);
                }
                else if ((i >= -4 && i <= 0) && (j >= -4 && j <= 0))
                {
                    // ZONE 1
                    resourcePositionListZoneOne.Add(resourcePosition);
                }
                else if ((i <= 4 && i >= 0) && (j >= -4 && j <= 0))
                {
                    // ZONE 2
                    resourcePositionListZoneTwo.Add(resourcePosition);
                }
                else if ((i <= 4 && i >= 0) && (j <= 4 && j >= 0))
                {
                    // ZONE 3
                    resourcePositionListZoneThree.Add(resourcePosition);
                }
                else if ((i >= -4 && i <= 0) && (j <= 4 && j >= 0))
                {
                    // ZONE 4
                    resourcePositionListZoneFour.Add(resourcePosition);
                }
            }
        }

        // 5 zones - 1-4 zone and between
        for (int k = 0; k < 5; k++)
        {
            if (k == 0)
            {
                resourcePool.PredefineUnitPoolByHost(commonResourcePrefab, k, resourcePositionListZoneBetween.Count);
                resourcePool.PredefineUnitPoolByHost(rareResourcePrefab, k, resourcePositionListZoneBetween.Count);
            }
            if (k == 1)
            {
                resourcePool.PredefineUnitPoolByHost(commonResourcePrefab, k, resourcePositionListZoneOne.Count);
                resourcePool.PredefineUnitPoolByHost(rareResourcePrefab, k, resourcePositionListZoneOne.Count);
            }
            if (k == 2)
            {
                resourcePool.PredefineUnitPoolByHost(commonResourcePrefab, k, resourcePositionListZoneTwo.Count);
                resourcePool.PredefineUnitPoolByHost(rareResourcePrefab, k, resourcePositionListZoneTwo.Count);
            }
            if (k == 3)
            {
                resourcePool.PredefineUnitPoolByHost(commonResourcePrefab, k, resourcePositionListZoneThree.Count);
                resourcePool.PredefineUnitPoolByHost(rareResourcePrefab, k, resourcePositionListZoneThree.Count);
            }
            if (k == 4)
            {
                resourcePool.PredefineUnitPoolByHost(commonResourcePrefab, k, resourcePositionListZoneFour.Count);
                resourcePool.PredefineUnitPoolByHost(rareResourcePrefab, k, resourcePositionListZoneFour.Count);
            }
        }
        SpawnHolyResource();
    }

    private void Update()
    {
        if (allOccupied || !gameHasStarted) return;

        if (resourceSpawnTime > 0)
        {
            resourceSpawnTime -= Time.deltaTime;
            return;
        }

        int resourceType = Random.Range(0, 10); // 0-9, 0-6(70%) common, 7-9(30%) rare
        SpawnResource(resourceType <= 6 ? true : false);
    }

    private void SpawnResource(bool isCommonResource)
    {
        resourceSpawnTime = resourceSpawnTimeMax;

        for (int i = 0; i < 5; i++)
        {
            Resource resource = resourcePool.GetResource(isCommonResource ? commonResourcePrefab : rareResourcePrefab, i);
            if (resource != null)
            {
                if (i == 0) resource.transform.position = resourcePositionListZoneBetween[Random.Range(0, resourcePositionListZoneBetween.Count)];
                else if (i == 1) resource.transform.position = resourcePositionListZoneOne[Random.Range(0, resourcePositionListZoneOne.Count)];
                else if (i == 2) resource.transform.position = resourcePositionListZoneTwo[Random.Range(0, resourcePositionListZoneTwo.Count)];
                else if (i == 3) resource.transform.position = resourcePositionListZoneThree[Random.Range(0, resourcePositionListZoneThree.Count)];
                else if (i == 4) resource.transform.position = resourcePositionListZoneFour[Random.Range(0, resourcePositionListZoneFour.Count)];

                // Enable resource
                resource.gameObject.SetActive(true);
                // Spawn resource for other players
                resource.GetComponent<NetworkObject>().Spawn();
                resource.GetComponent<Resource>().SetOccupiedZone(i);
                // Save occupied zone index and index of resource on this zone
                resource.GetComponent<Resource>().SetResourceWeight();
            }
        }

        allOccupied = (resourcePositionListZoneBetween.Count == 0 &&
            resourcePositionListZoneOne.Count == 0 &&
            resourcePositionListZoneTwo.Count == 0 &&
            resourcePositionListZoneThree.Count == 0 &&
            resourcePositionListZoneFour.Count == 0);
    }

    private void SpawnHolyResource()
    {
        GameObject holyResource = Instantiate(holyResourcePrefab, Vector3.zero, Quaternion.identity).gameObject;
        holyResource.GetComponent<NetworkObject>().Spawn();
        holyResource.GetComponent<Resource>().SetResourceWeight();
        holyResource.SetActive(true);
    }

    public void ClearOccupiedZone(Resource resource, int index)
    {
        if (resource.IsCommonResource) resourcePool.ReturnResource(commonResourcePrefab, resource, index);
        else resourcePool.ReturnResource(rareResourcePrefab, resource, index);
        allOccupied = false;
    }

    public void GameHasStarted()
    {
        gameHasStarted = true;
    }
}
