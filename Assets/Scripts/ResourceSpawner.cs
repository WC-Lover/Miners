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
    private List<List<Vector3>> listOfResourcePositionLists;

    private void Awake()
    {
        Instance = this;

        //resourceSpawnTimeMax = 10;
        ////resourceSpawnTimeMax = 0;
        //resourceSpawnTime = 0;


        // RESOURCE ZONE BETWEEN PLAYERS
        resourcePositionListZoneBetween = new List<Vector3>();

        // PLAYERS' RESOURCE ZONE 1-4
        resourcePositionListZoneOne = new List<Vector3>();
        resourcePositionListZoneTwo = new List<Vector3>();
        resourcePositionListZoneThree = new List<Vector3>();
        resourcePositionListZoneFour = new List<Vector3>();

        listOfResourcePositionLists = new List<List<Vector3>>(5) {
            resourcePositionListZoneBetween,
            resourcePositionListZoneOne,
            resourcePositionListZoneTwo,
            resourcePositionListZoneThree,
            resourcePositionListZoneFour
        };

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
                    listOfResourcePositionLists[0].Add(resourcePosition);
                }
                else if ((i >= -4 && i <= 0) && (j >= -4 && j <= 0))
                {
                    // ZONE 1
                    listOfResourcePositionLists[1].Add(resourcePosition);
                }
                else if ((i <= 4 && i >= 0) && (j >= -4 && j <= 0))
                {
                    // ZONE 2
                    listOfResourcePositionLists[2].Add(resourcePosition);
                }
                else if ((i <= 4 && i >= 0) && (j <= 4 && j >= 0))
                {
                    // ZONE 3
                    listOfResourcePositionLists[3].Add(resourcePosition);
                }
                else if ((i >= -4 && i <= 0) && (j <= 4 && j >= 0))
                {
                    // ZONE 4
                    listOfResourcePositionLists[4].Add(resourcePosition);
                }
            }
        }


        // 5 zones - 1-4 zone and between
        for (int i = 0; i < 5; i++)
        {
            resourcePool.PredefineUnitPoolByHost(true, ref commonResourcePrefab, i, listOfResourcePositionLists[i].Count);
            resourcePool.PredefineUnitPoolByHost(false, ref rareResourcePrefab, i, listOfResourcePositionLists[i].Count);
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

        allOccupied = true;

        for (int zoneIndex = 0; zoneIndex < 5; zoneIndex++)
        {
            if (listOfResourcePositionLists[zoneIndex].Count == 0) continue;
            if (listOfResourcePositionLists[zoneIndex].Count > 1) allOccupied = false;

            Debug.Log(isCommonResource);

            Vector3 position = listOfResourcePositionLists[zoneIndex][Random.Range(0, listOfResourcePositionLists[zoneIndex].Count)];
            Resource resource = resourcePool.GetResource(isCommonResource, zoneIndex);

            resource.transform.position = position;
            // Remove from list to prevent from spawning 2 resources on same spot
            listOfResourcePositionLists[zoneIndex].Remove(position);

            // Enable resource
            resource.gameObject.SetActive(true);
            // Spawn resource for other players
            resource.GetComponent<NetworkObject>().Spawn();
            // Save occupied zone index and index of resource on this zone
            resource.GetComponent<Resource>().SetOccupiedZone(zoneIndex, position);
        }
    }

    private void SpawnHolyResource()
    {
        GameObject holyResource = Instantiate(holyResourcePrefab, Vector3.zero, Quaternion.identity).gameObject;
        holyResource.GetComponent<NetworkObject>().Spawn();
        holyResource.SetActive(true);
    }

    public void ClearOccupiedZone(bool isCommonResource, int zoneIndex, Resource resource)
    {
        // Refill pool
        resourcePool.ReturnResource(isCommonResource, zoneIndex, resource);
        // Restore position
        listOfResourcePositionLists[zoneIndex].Add(resource.spawnPosition);

        allOccupied = false;
    }

    public void GameHasStarted()
    {
        gameHasStarted = true;
    }
}
