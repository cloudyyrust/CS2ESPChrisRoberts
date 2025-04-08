using System;
using System.Numerics;

namespace chrsiroberts
{
    public static class Calculate
    {
        public static Vector2 WorldToScreen(float[] viewMatrix, Vector3 pos, int screenWidth, int screenHeight)
        {
            float w = viewMatrix[12] * pos.X + viewMatrix[13] * pos.Y + viewMatrix[14] * pos.Z + viewMatrix[15];

            if (w < 0.01f)
                return new Vector2(-1, -1);

            float x = viewMatrix[0] * pos.X + viewMatrix[1] * pos.Y + viewMatrix[2] * pos.Z + viewMatrix[3];
            float y = viewMatrix[4] * pos.X + viewMatrix[5] * pos.Y + viewMatrix[6] * pos.Z + viewMatrix[7];

            float screenX = (screenWidth / 2) * (1 + (x / w));
            float screenY = (screenHeight / 2) * (1 - (y / w));

            return new Vector2(screenX, screenY);
        }

        public static float[] GetViewMatrix(MemoryReader reader, IntPtr clientDll)
        {
            try
            {
                float[] matrix = new float[16];
                IntPtr matrixAddress = clientDll + Offsets.dwViewMatrix;

                for (int i = 0; i < 16; i++)
                {
                    matrix[i] = reader.Read<float>(matrixAddress + (i * sizeof(float)));
                }

                return matrix;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"View matrix error: {ex.Message}");
                return new float[16] {
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                };
            }
        }

        public static float CalculateDistance(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            return (float)Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z) * 0.0254f;
        }
    }
}