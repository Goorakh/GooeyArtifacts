using HG;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace GooeyArtifacts.Utils.Extensions
{
    public static class NetworkExtensions
    {
        public static void WriteItemPickupTimestamps(this NetworkWriter writer, Run.FixedTimeStamp[] itemPickupTimeStamps)
        {
            List<uint> indicesWithValue = new List<uint>(itemPickupTimeStamps.Length);
            for (uint i = 0; i < itemPickupTimeStamps.Length; i++)
            {
                if (!itemPickupTimeStamps[i].isInfinity)
                {
                    indicesWithValue.Add(i);
                }
            }

            writer.WritePackedUInt32((uint)indicesWithValue.Count);
            foreach (uint i in indicesWithValue)
            {
                writer.WritePackedUInt32(i);
            }

            foreach (uint i in indicesWithValue)
            {
                writer.Write(itemPickupTimeStamps[i]);
            }
        }

        public static void ReadItemPickupTimestamps(this NetworkReader reader, Run.FixedTimeStamp[] destItemPickupTimeStamps)
        {
            uint indicesCount = reader.ReadPackedUInt32();
            uint[] indices = new uint[indicesCount];
            for (int i = 0; i < indicesCount; i++)
            {
                indices[i] = reader.ReadPackedUInt32();
            }

            ArrayUtils.SetAll(destItemPickupTimeStamps, Run.FixedTimeStamp.positiveInfinity);
            foreach (uint i in indices)
            {
                destItemPickupTimeStamps[i] = reader.ReadFixedTimeStamp();
            }
        }
    }
}
