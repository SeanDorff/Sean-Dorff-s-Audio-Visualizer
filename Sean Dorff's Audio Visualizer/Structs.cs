using OpenTK.Mathematics;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal struct SSpectrumBar
    {
        public Vector3 LowerLeft;
        public Vector3 LowerRight;
        public Vector3 UpperLeft;
        public Vector3 UpperRight;
        public Vector4 Color;
    }

    internal struct SIndexDistance
    {
        public uint Index;
        public int IntegerDistance;
    }

    internal struct SStartParameter
    {
        public int ShaderNo;
        public int Generation;
    }

    internal struct SStar
    {
        public Vector3 Position;
        public float Generation;
        public Vector4 Color;
    }
}
