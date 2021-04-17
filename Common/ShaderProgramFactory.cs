using OpenTK.Graphics.OpenGL4;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Common
{

    public static class ShaderProgramFactory
    {
        public static TriangleAndPointShader BuildTriangleAndPointShaderProgram(string vertexPath, string fragmentPath, int bufferCount = 1)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int shaderProgramHandle = BuildShaderProgram(vertexPath, fragmentPath, out Dictionary<string, int> uniformLocations);
                return new TriangleAndPointShader(shaderProgramHandle, uniformLocations, bufferCount);
            }
        }

        public static TextureShader BuildTextureShaderProgram(string vertexPath, string fragmentPath, int bufferCount = 1)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int shaderProgramHandle = BuildShaderProgram(vertexPath, fragmentPath, out Dictionary<string, int> uniformLocations);
                return new TextureShader(shaderProgramHandle, uniformLocations, bufferCount);
            }
        }

        private static int BuildShaderProgram(string vertexPath, string fragmentPath, out Dictionary<string, int> uniformLocations)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int vertexShaderHandle = BuildShader(vertexPath, ShaderType.VertexShader);
                int fragmentShaderHandle = BuildShader(fragmentPath, ShaderType.FragmentShader);

                int shaderProgramHandle = BuildProgram(vertexShaderHandle, fragmentShaderHandle);

                CleanUpShader(shaderProgramHandle, vertexShaderHandle);
                CleanUpShader(shaderProgramHandle, fragmentShaderHandle);

                uniformLocations = GetUniformLocations(shaderProgramHandle);

                return shaderProgramHandle;
            }
        }

        private static int BuildShader(string shaderPath, ShaderType shaderType)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                string shaderSource = File.ReadAllText(shaderPath);
                int shader = GL.CreateShader(shaderType);
                GL.ShaderSource(shader, shaderSource);
                CompileShader(shader);
                return shader;
            }
        }

        private static void CompileShader(int shader)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                GL.CompileShader(shader);
                GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);
                if (code != (int)All.True)
                {
                    string infoLog = GL.GetShaderInfoLog(shader);
                    throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
                }
            }
        }

        private static int BuildProgram(int vertexShaderHandle, int fragmentShaderHandle)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int shaderProgramHandle = GL.CreateProgram();
                GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
                GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);
                LinkProgram(shaderProgramHandle);
                return shaderProgramHandle;
            }
        }

        private static void LinkProgram(int shaderProgramHandle)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                GL.LinkProgram(shaderProgramHandle);

                GL.GetProgram(shaderProgramHandle, GetProgramParameterName.LinkStatus, out int code);
                if (code != (int)All.True)
                {
                    string infoLog = GL.GetProgramInfoLog(shaderProgramHandle);
                    throw new Exception($"Error occured whilst linking Shader Program({shaderProgramHandle}).\n\n{infoLog}");
                }
            }
        }

        private static void CleanUpShader(int shaderProgramHandle, int shaderHandle)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                GL.DetachShader(shaderProgramHandle, shaderHandle);
                GL.DeleteShader(shaderHandle);
            }
        }

        private static Dictionary<string, int> GetUniformLocations(int shaderProgramHandle)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
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
}
