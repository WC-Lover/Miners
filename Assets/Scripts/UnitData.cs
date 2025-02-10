using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct UnitData : IEquatable<UnitData>, INetworkSerializable
{

    public float speed;
    public float gatherPower;
    public float gatherCooldown;
    public float attackDamage;
    public float attackCooldown;
    public int carryCapacity;
    public Vector3 directionToMoveBackToBase;
    public Vector3 directionToMoveAfterSpawn;
    public float interactionsAmountMax;

    public bool Equals(UnitData other)
    {
        return speed == other.speed &&
            gatherPower == other.gatherPower &&
            gatherCooldown == other.gatherCooldown &&
            attackDamage == other.attackDamage &&
            carryCapacity == other.carryCapacity &&
            directionToMoveBackToBase == other.directionToMoveBackToBase &&
            directionToMoveAfterSpawn == other.directionToMoveAfterSpawn &&
            interactionsAmountMax == other.interactionsAmountMax;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref speed);
        serializer.SerializeValue(ref gatherPower);
        serializer.SerializeValue(ref gatherCooldown);
        serializer.SerializeValue(ref attackDamage);
        serializer.SerializeValue(ref attackCooldown);
        serializer.SerializeValue(ref carryCapacity);
        serializer.SerializeValue(ref directionToMoveBackToBase);
        serializer.SerializeValue(ref directionToMoveAfterSpawn);
        serializer.SerializeValue(ref interactionsAmountMax);
    }
}
