using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ResourceSpawner : MonoBehaviour
{
    public static ResourceSpawner Instance;

    [SerializeField] private Transform commonResourcePrefab;
    [SerializeField] private Transform rareResourcePrefab;
    [SerializeField] private Transform holyResourcePrefab;

    [SerializeField] private float resourceSpawnTime;
    [SerializeField] private float resourceSpawnTimeMax;
    private bool allOccupied = false;

    // COMMON/RARE RESOURCE ZONE BETWEEN PLAYERS LISTS
    private List<Transform> disabledCommonResourceTransformListZoneBetween;
    private List<Transform> disabledRareResourceTransformListZoneBetween;
    // PLAYERS' COMMON RESOURCE ZONE 1-4 LISTS
    private List<Transform> disabledCommonResourceTransformListZoneOne;
    private List<Transform> disabledCommonResourceTransformListZoneTwo;
    private List<Transform> disabledCommonResourceTransformListZoneThree;
    private List<Transform> disabledCommonResourceTransformListZoneFour;
    // PLAYERS' RARE RESOURCE ZONE 1-4 LISTS
    private List<Transform> disabledRareResourceTransformListZoneOne;
    private List<Transform> disabledRareResourceTransformListZoneTwo;
    private List<Transform> disabledRareResourceTransformListZoneThree;
    private List<Transform> disabledRareResourceTransformListZoneFour;
    // AVAILABLE ZONES INDEXES
    private List<int> availableDisabledResourcesIndexesZoneOne;
    private List<int> availableDisabledResourcesIndexesZoneTwo;
    private List<int> availableDisabledResourcesIndexesZoneThree;
    private List<int> availableDisabledResourcesIndexesZoneFour;
    private List<int> availableDisabledResourcesIndexesZoneBetween;
    // DICTIONARY KEY: INDEX | VALUE: LIST<TRANSFORM>
    private Dictionary<int, List<Transform>> indexToZoneListDictionary;
    private Dictionary<int, List<int>> indexToAvailableZoneListDictionary;

    private void Awake()
    {
        Instance = this;

        resourceSpawnTimeMax = 10;
        //resourceSpawnTimeMax = 0;
        resourceSpawnTime = 0;

        // DICTIONARY KEY: INDEX | VALUE: LIST<TRANSFORM>
        indexToZoneListDictionary = new Dictionary<int, List<Transform>>();
        indexToAvailableZoneListDictionary = new Dictionary<int, List<int>>();

        // COMMON/RARE RESOURCE ZONE BETWEEN PLAYERS LISTS
        disabledCommonResourceTransformListZoneBetween = new List<Transform>();
        disabledRareResourceTransformListZoneBetween = new List<Transform>();

        // PLAYERS' COMMON RESOURCE ZONE 1-4 LISTS
        disabledCommonResourceTransformListZoneOne = new List<Transform>();
        disabledCommonResourceTransformListZoneTwo = new List<Transform>();
        disabledCommonResourceTransformListZoneThree = new List<Transform>();
        disabledCommonResourceTransformListZoneFour = new List<Transform>();

        // PLAYERS' RARE RESOURCE ZONE 1-4 LISTS
        disabledRareResourceTransformListZoneOne = new List<Transform>();
        disabledRareResourceTransformListZoneTwo = new List<Transform>();
        disabledRareResourceTransformListZoneThree = new List<Transform>();
        disabledRareResourceTransformListZoneFour = new List<Transform>();

        // AVAILABLE ZONES INDEXES
        availableDisabledResourcesIndexesZoneOne = new List<int>();
        availableDisabledResourcesIndexesZoneTwo = new List<int>();
        availableDisabledResourcesIndexesZoneThree = new List<int>();
        availableDisabledResourcesIndexesZoneFour = new List<int>();
        availableDisabledResourcesIndexesZoneBetween = new List<int>();

        for (int i = -4; i < 5; i++)
        {
            for (int j = -4; j < 5; j++)
            {
                // UNAVAILABLE FOR SPAWN ZONES (buildings, holyResource)
                if ((i == -4 || i == 4) && (j == -4 || j == 4)) continue;
                if (Math.Abs(i) <= 1 && Math.Abs(j) <= 1) continue;
                if ((i == -4 || i == 4 || j == -4 || j == 4) && (i == 0 || j == 0)) continue;

                
                Vector3 resourcePosition = new Vector3(i, 0, j);

                Transform commonResourceTransorm = Instantiate(commonResourcePrefab, resourcePosition, Quaternion.identity);
                commonResourceTransorm.gameObject.SetActive(false);
                Transform rareResourceTransorm = Instantiate(rareResourcePrefab, resourcePosition, Quaternion.identity);
                rareResourceTransorm.gameObject.SetActive(false);

                if (i == 0 || j == 0)
                {
                    // ZONE IN BETWEEN PLAYERS
                    disabledCommonResourceTransformListZoneBetween.Add(commonResourceTransorm);
                    disabledRareResourceTransformListZoneBetween.Add(rareResourceTransorm);

                    availableDisabledResourcesIndexesZoneBetween.Add(availableDisabledResourcesIndexesZoneBetween.Count);
                }
                else if ((i >= -4 && i <= 0) && (j >= -4 && j <= 0))
                {
                    // ZONE 1
                    disabledCommonResourceTransformListZoneOne.Add(commonResourceTransorm);
                    disabledRareResourceTransformListZoneOne.Add(rareResourceTransorm);

                    availableDisabledResourcesIndexesZoneOne.Add(availableDisabledResourcesIndexesZoneOne.Count);
                }
                else if ((i <= 4 && i >= 0) && (j >= -4 && j <= 0))
                {
                    // ZONE 2
                    disabledCommonResourceTransformListZoneTwo.Add(commonResourceTransorm);
                    disabledRareResourceTransformListZoneTwo.Add(rareResourceTransorm);

                    availableDisabledResourcesIndexesZoneTwo.Add(availableDisabledResourcesIndexesZoneTwo.Count);
                }
                else if ((i <= 4 && i >= 0) && (j <= 4 && j >= 0))
                {
                    // ZONE 3
                    disabledCommonResourceTransformListZoneThree.Add(commonResourceTransorm);
                    disabledRareResourceTransformListZoneThree.Add(rareResourceTransorm);

                    availableDisabledResourcesIndexesZoneThree.Add(availableDisabledResourcesIndexesZoneThree.Count);
                }
                else if ((i >= -4 && i <= 0) && (j <= 4 && j >= 0))
                {
                    // ZONE 4
                    disabledCommonResourceTransformListZoneFour.Add(commonResourceTransorm);
                    disabledRareResourceTransformListZoneFour.Add(rareResourceTransorm);

                    availableDisabledResourcesIndexesZoneFour.Add(availableDisabledResourcesIndexesZoneFour.Count);
                }
            }
        }

        // Store all the above info in dictionaries, to avoid complex resource spawn process
        indexToZoneListDictionary[0] = disabledCommonResourceTransformListZoneOne;
        indexToZoneListDictionary[1] = disabledRareResourceTransformListZoneOne;
        indexToZoneListDictionary[2] = disabledCommonResourceTransformListZoneTwo;
        indexToZoneListDictionary[3] = disabledRareResourceTransformListZoneTwo;
        indexToZoneListDictionary[4] = disabledCommonResourceTransformListZoneThree;
        indexToZoneListDictionary[5] = disabledRareResourceTransformListZoneThree;
        indexToZoneListDictionary[6] = disabledCommonResourceTransformListZoneFour;
        indexToZoneListDictionary[7] = disabledRareResourceTransformListZoneFour;
        indexToZoneListDictionary[8] = disabledRareResourceTransformListZoneBetween;
        indexToZoneListDictionary[9] = disabledRareResourceTransformListZoneBetween;

        indexToAvailableZoneListDictionary[0] = availableDisabledResourcesIndexesZoneOne;
        indexToAvailableZoneListDictionary[2] = availableDisabledResourcesIndexesZoneTwo;
        indexToAvailableZoneListDictionary[4] = availableDisabledResourcesIndexesZoneThree;
        indexToAvailableZoneListDictionary[6] = availableDisabledResourcesIndexesZoneFour;
        indexToAvailableZoneListDictionary[8] = availableDisabledResourcesIndexesZoneBetween;

        SpawnHolyResource();
    }

    private void Update()
    {
        if (allOccupied) return;

        if (resourceSpawnTime > 0)
        {
            resourceSpawnTime -= Time.deltaTime;
            return;
        }

        int resourceType = Random.Range(0, 11); // 0-10, 0-7(70%) common, 8-10(30%) rare
        SpawnResource(resourceType <= 7 ? Resource.ResourceType.Common : Resource.ResourceType.Rare);
    }

    private void SpawnResource(Resource.ResourceType resourceType)
    {
        resourceSpawnTime = resourceSpawnTimeMax;

        bool resourceTypeCommon = resourceType == Resource.ResourceType.Common;

        // Check if there is place to spawn left
        bool allOccupiedTemp = true;

        // Get through each zone and spawn one resource on it
        for (int i = 0; i < indexToZoneListDictionary.Keys.Count; i += 2)
        {
            // Get random Index from Current Zone that is available to spawn Transform
            if (indexToAvailableZoneListDictionary[i].Count == 0) continue;
            allOccupiedTemp = false;

            int randomIndex = indexToAvailableZoneListDictionary[i][Random.Range(0, indexToAvailableZoneListDictionary[i].Count)];
            Transform resourceTransformToSpawn = resourceTypeCommon
                ? indexToZoneListDictionary[i][randomIndex] // Common resource
                : indexToZoneListDictionary[i + 1][randomIndex]; // Rare resource
            // Remove random Index not to spawn on the same spot, it will be restored when resource has been gathered -> ClearOccupiedZone
            indexToAvailableZoneListDictionary[i].Remove(randomIndex);

            // Enable resource
            resourceTransformToSpawn.gameObject.SetActive(true);
            // Spawn resource for other players
            resourceTransformToSpawn.GetComponent<NetworkObject>().Spawn();
            resourceTransformToSpawn.GetComponent<Resource>().SetResourceWeight();
            // Save occupied zone index and index of resource on this zone
            resourceTransformToSpawn.GetComponent<Resource>().SetOccupiedZone(randomIndex, i);
        }

        allOccupied = allOccupiedTemp;
    }

    private void SpawnHolyResource()
    {
        GameObject holyResource = Instantiate(holyResourcePrefab, Vector3.zero, Quaternion.identity).gameObject;
        holyResource.GetComponent<NetworkObject>().Spawn();
        holyResource.SetActive(true);
    }

    public void ClearOccupiedZone(int occupiedIndex, int occupiedZoneIndex)
    {
        // Restore occupied zone
        indexToAvailableZoneListDictionary[occupiedZoneIndex].Add(occupiedIndex);

        allOccupied = false;
    }
}
