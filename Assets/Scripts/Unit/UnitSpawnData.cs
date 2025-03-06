using System;
using Unity.Netcode;
using UnityEngine;


namespace Assets.Scripts.Unit
{
    public struct UnitSpawnData : IEquatable<UnitSpawnData>, INetworkSerializable
    {

        public int buildingLevel;
        public Vector3 baseOfOriginPosition;
        public Vector3 firstDestinationPosition;
        public BonusSelectUI.Bonus tempBonus;
        public BonusSelectUI.Bonus permBonus;
        public bool Equals(UnitSpawnData other)
        {
            return buildingLevel == other.buildingLevel &&
                baseOfOriginPosition == other.baseOfOriginPosition &&
                firstDestinationPosition == other.firstDestinationPosition &&
                tempBonus == other.tempBonus &&
            permBonus == other.permBonus;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref buildingLevel);
            serializer.SerializeValue(ref baseOfOriginPosition);
            serializer.SerializeValue(ref firstDestinationPosition);
            serializer.SerializeValue(ref tempBonus);
            serializer.SerializeValue(ref permBonus);
        }
    }
}