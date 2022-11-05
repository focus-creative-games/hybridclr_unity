using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HybridCLR.Runtime
{
    public class DifferentialHybridAssemblyOptions
    {
        public const uint Signature = 0xABCDABCD;

        public List<uint> notChangeMethodTokens;


        public byte[] Marshal()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(Signature);
            writer.Write((uint)notChangeMethodTokens.Count);
            foreach (uint token in notChangeMethodTokens)
            {
                writer.Write(token);
            }
            writer.Flush();
            stream.Flush();
            byte[] result = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(result, 0, result.Length);
            Debug.Log($"HotPatchAssemblyConfig. options bytes:{result.Length}");
            return result;
        }
    }
}
