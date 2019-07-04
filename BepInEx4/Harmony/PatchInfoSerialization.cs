using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using HarmonyLib;

namespace Harmony
{
    public static class PatchInfoSerialization
    {
        public static byte[] Serialize(this PatchInfo patchInfo)
        {
            byte[] buffer;
            using (var memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, patchInfo);
                buffer = memoryStream.GetBuffer();
            }

            return buffer;
        }

        public static PatchInfo Deserialize(byte[] bytes)
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Binder = new Binder();
            var serializationStream = new MemoryStream(bytes);
            return (PatchInfo) binaryFormatter.Deserialize(serializationStream);
        }

        public static int PriorityComparer(object obj, int index, int priority, string[] before, string[] after)
        {
            var traverse = Traverse.Create(obj);
            var value = traverse.Field("owner").GetValue<string>();
            var value2 = traverse.Field("priority").GetValue<int>();
            var value3 = traverse.Field("index").GetValue<int>();
            if (before != null && Array.IndexOf(before, value) > -1) return -1;
            if (after != null && Array.IndexOf(after, value) > -1) return 1;
            if (priority != value2) return -priority.CompareTo(value2);
            return index.CompareTo(value3);
        }

        private class Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                foreach (var type in new[]
                {
                    typeof(PatchInfo),
                    typeof(Patch[]),
                    typeof(Patch)
                })
                    if (typeName == type.FullName)
                        return type;
                return Type.GetType($"{typeName}, {assemblyName}");
            }
        }
    }
}