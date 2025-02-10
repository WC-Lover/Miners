using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct BuildingData : IEquatable<BuildingData>, INetworkSerializable
{

    public float buildingHPMax;
    public float buildingXPMax;
    public float buildingLevel;
    public float amountOfMinersPerSpawn;
    public float minersSpawnTimeMax;
    public Vector3 unitSpawnPoint;
    public Vector3 spawnBasePoint;

    public bool Equals(BuildingData other)
    {
        return buildingHPMax == other.buildingHPMax &&
            buildingXPMax == other.buildingXPMax &&
            buildingLevel == other.buildingLevel &&
            amountOfMinersPerSpawn == other.amountOfMinersPerSpawn &&
            unitSpawnPoint == other.unitSpawnPoint &&
            spawnBasePoint == other.spawnBasePoint;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref buildingHPMax);
        serializer.SerializeValue(ref buildingXPMax);
        serializer.SerializeValue(ref buildingLevel);
        serializer.SerializeValue(ref amountOfMinersPerSpawn);
        serializer.SerializeValue(ref minersSpawnTimeMax);
        serializer.SerializeValue(ref unitSpawnPoint);
        serializer.SerializeValue(ref spawnBasePoint);
    }
}
