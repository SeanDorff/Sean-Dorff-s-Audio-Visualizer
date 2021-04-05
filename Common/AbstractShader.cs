namespace Common
{
    public abstract class AbstractShader
    {
        private int elementBufferHandle;
        private int vertexBufferHandle;
        private int vertexArrayHandle;
        private Shader shader;

        public int ElementBufferHandle { get => elementBufferHandle; set => elementBufferHandle = value; }
        public int VertexBufferHandle { get => vertexBufferHandle; set => vertexBufferHandle = value; }
        public int VertexArrayHandle { get => vertexArrayHandle; set => vertexArrayHandle = value; }
        public Shader Shader { get => shader; set => shader = value; }
    }
}
