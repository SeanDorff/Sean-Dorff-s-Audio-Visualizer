using OpenTK.Graphics.OpenGL4;

using System;
using System.Collections.Generic;
using System.IO;

namespace Common
{

    public static class ShaderProgramFactory
    {
        public static TriangleAndPointShader BuildTriangleAndPointShaderProgram(string vertexPath, string fragmentPath, int bufferCount)
        {
            int vertexShaderHandle = BuildShader(vertexPath, ShaderType.VertexShader);
            int fragmentShaderHandle = BuildShader(fragmentPath, ShaderType.FragmentShader);

            int shaderProgramHandle = BuildProgram(vertexShaderHandle, fragmentShaderHandle);

            CleanUpShader(shaderProgramHandle, vertexShaderHandle);
            CleanUpShader(shaderProgramHandle, fragmentShaderHandle);

            Dictionary<string, int> uniformLocations = GetUniformLocations(shaderProgramHandle);

            return new TriangleAndPointShader(shaderProgramHandle, uniformLocations, bufferCount);
        }

        private static int BuildShader(string shaderPath, ShaderType shaderType)
        {
            string shaderSource = File.ReadAllText(shaderPath);
            int shader = GL.CreateShader(shaderType);
            GL.ShaderSource(shader, shaderSource);
            CompileShader(shader);
            return shader;
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);
            if (code != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        private static int BuildProgram(int vertexShaderHandle, int fragmentShaderHandle)
        {
            int shaderProgramHandle = GL.CreateProgram();
            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);
            LinkProgram(shaderProgramHandle);
            return shaderProgramHandle;
        }

        private static void LinkProgram(int shaderProgramHandle)
        {
            GL.LinkProgram(shaderProgramHandle);

            GL.GetProgram(shaderProgramHandle, GetProgramParameterName.LinkStatus, out int code);
            if (code != (int)All.True)
            {
                string infoLog = GL.GetProgramInfoLog(shaderProgramHandle);
                throw new Exception($"Error occured whilst linking Shader Program({shaderProgramHandle}).\n\n{infoLog}");
            }
        }

        private static void CleanUpShader(int shaderProgramHandle, int shaderHandle)
        {
            GL.DetachShader(shaderProgramHandle, shaderHandle);
            GL.DeleteShader(shaderHandle);
        }

        private static Dictionary<string, int> GetUniformLocations(int shaderProgramHandle)
        {
            Dictionary<string, int> uniformLocations = new();

            GL.GetProgram(shaderProgramHandle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);

            for (int i = 0; i < numberOfUniforms; i++)
            {
                string key = GL.GetActiveUniform(shaderProgramHandle, i, out _, out _);
                int location = GL.GetUniformLocation(shaderProgramHandle, key);
                uniformLocations.Add(key, location);
            }

            return uniformLocations;
        }
    }
}
