using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct ClaimedPlayerData : IEquatable<ClaimedPlayerData>, INetworkSerializable
{

    public ulong clientId;
    public float claimPercentage;
    public bool Equals(ClaimedPlayerData other)
    {
        return clientId == other.clientId &&
            claimPercentage == other.claimPercentage;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref claimPercentage);
    }
}
