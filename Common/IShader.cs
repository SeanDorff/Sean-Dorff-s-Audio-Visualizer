using OpenTK.Mathematics;

namespace Common
{
    public interface IShader
    {
        public void DrawElements();
        public void DrawArrays(int length);
        public void SetFloat(string name, float data);
        public void SetFloatArray(string name, float[] data);
        public void SetInt(string name, int data);
        public void SetMatrix4(string name, Matrix4 data);
        public void SetVector3(string name, Vector3 data);
        public void Use();
        public void Unload();
    }
}
