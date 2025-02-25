using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerHolyResourceData : IEquatable<PlayerHolyResourceData>, INetworkSerializable
{

    public ulong clientId;
    public FixedString64Bytes playerName;
    public float holyResourceGathered;
    public bool Equals(PlayerHolyResourceData other)
    {
        return clientId == other.clientId &&
            playerName == other.playerName &&
            holyResourceGathered == other.holyResourceGathered;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref holyResourceGathered);
    }
}
