using System.IO;
using DPFP;
using DPUruNet;

namespace PantallaChecador.Utils
{
    public static class FingerprintHelper
    {
        // --- DPFP: Serialización ---
        public static byte[] SerializeDPFP(Template template)
        {
            using (var stream = new MemoryStream())
            {
                template.Serialize(stream);
                return stream.ToArray();
            }
        }

        // --- DPFP: Deserialización ---
        public static Template DeserializeDPFP(byte[] data)
        {
            return new Template(new MemoryStream(data));
        }

        // --- DPUruNet: Serialización ---
        public static byte[] SerializeFMD(Fmd fmd)
        {
            return Fmd.SerializeFmd(fmd);
        }

        // --- DPUruNet: Deserialización ---
        public static Fmd DeserializeFMD(byte[] data)
        {
            return Fmd.DeserializeFmd(data);
        }
    }
}
