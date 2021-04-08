using OpenTK.Mathematics;

namespace Sean_Dorff_s_Audio_Visualizer
{
    internal struct SSpectrumBar
    {
        public Vector4 LowerLeft;
        public Vector4 LowerRight;
        public Vector4 UpperLeft;
        public Vector4 UpperRight;
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
