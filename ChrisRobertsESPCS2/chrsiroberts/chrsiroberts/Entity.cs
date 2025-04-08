using System;
using System.Numerics;
using System.Drawing;
using System.Collections.Generic;

namespace chrsiroberts
{
    public class Entity
    {
        
        public IntPtr Address { get; set; }

        
        public Vector3 Position { get; set; }
        public Vector3 ViewOffset { get; set; }
        public Vector2 Position2D { get; set; }
        public Vector2 ViewPosition2D { get; set; }

        
        public Vector3 Head { get; set; }
        public Vector2 Head2D { get; set; }

        
        public float Distance { get; set; }
        public int Health { get; set; }
        public int Team { get; set; }

        private IntPtr clientDllAddress;
        private static readonly Dictionary<string, int> Bones = new()
        {
            { "Waist", 9 },
            { "UpperChest", 4 },
            { "LowerChest", 3 },
            { "Stomach", 2 },
            { "Pelvis", 0 },
            { "Neck", 5 },
            { "Head", 6 },
            { "ShoulderLeft", 8 },
            { "ForeLeft", 9 },
            { "LeftArm", 10 },
            { "HandLeft", 11 },
            { "ShoulderRight", 13 },
            { "ForeRight", 14 },
            { "RightArm", 15 },
            { "HandRight", 16 },
            { "KneeLeft", 23 },
            { "FeetLeft", 24 },
            { "KneeRight", 26 },
            { "FeetRight", 27 }
        };


        public bool Update(MemoryReader reader, IntPtr clientDll, IntPtr localPlayerController)
        {
            try
            {
                if (Address == IntPtr.Zero)
                    return false;

                    
                var gameSceneNode = reader.Read<IntPtr>(Address + Offsets.m_pGameSceneNode);
                if (gameSceneNode == IntPtr.Zero)
                    return false;

                
                Team = reader.Read<int>(Address + Offsets.m_iTeamNum);
                Health = reader.Read<int>(Address + Offsets.m_iHealth);

                
                Position = reader.Read<Vector3>(gameSceneNode + Offsets.m_vecAbsOrigin);

                   
                Head = new Vector3(Position.X, Position.Y, Position.Z + 72.0f);

                
                float[] viewMatrix = Calculate.GetViewMatrix(reader, clientDll);
                Position2D = Calculate.WorldToScreen(viewMatrix, Position, 1920, 1080);
                Head2D = Calculate.WorldToScreen(viewMatrix, Head, 1920, 1080);

                
                var localPlayerPawn = reader.Read<IntPtr>(clientDll + Offsets.dwLocalPlayerController);
                if (localPlayerPawn != IntPtr.Zero)
                {
                    var localGameSceneNode = reader.Read<IntPtr>(localPlayerPawn + Offsets.m_pGameSceneNode);
                    if (localGameSceneNode != IntPtr.Zero)
                    {
                        var localPos = reader.Read<Vector3>(localGameSceneNode + Offsets.m_vecAbsOrigin);
                        Distance = Calculate.CalculateDistance(localPos, Position);
                    }
                }

                
                if (Health <= 0 || Health > 100)
                    return false;

                if (Team != 2 && Team != 3)
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void DrawBones(MemoryReader memoryReader, Graphics g, IntPtr clientDll, Pen bonePen)
        {
            try
            {
                
                IntPtr sceneNode = memoryReader.Read<IntPtr>(Address + Offsets.m_pGameSceneNode);
                if (sceneNode == IntPtr.Zero)
                    return;

                
                IntPtr boneMatrix = memoryReader.Read<IntPtr>(sceneNode + Offsets.m_modelState + 0x80);
                if (boneMatrix == IntPtr.Zero)
                    return;

                
                float[] viewMatrix = Calculate.GetViewMatrix(memoryReader, clientDll);

                
                Vector2[] bones2d = new Vector2[13];
                for (int i = 0; i < 13; i++)
                {
                    Vector3 bonePos = memoryReader.Read<Vector3>(boneMatrix + GetBoneIndexForArrayPosition(i) * 0x20);
                    bones2d[i] = Calculate.WorldToScreen(viewMatrix, bonePos, 1920, 1080);
                }

               
                var connections = new (int, int)[]
                {
                    (1, 2),  // Neck to Head
                    (1, 3),  // Neck to ShoulderLeft
                    (1, 6),  // Neck to ShoulderRight
                    (3, 4),  // ShoulderLeft to ForeLeft
                    (6, 7),  // ShoulderRight to ForeRight
                    (4, 5),  // ForeLeft to HandLeft
                    (7, 8),  // ForeRight to HandRight
                    (1, 0),  // Neck to Waist
                    (0, 9),  // Waist to KneeLeft
                    (0, 11), // Waist to KneeRight
                    (9, 10), // KneeLeft to FeetLeft
                    (11, 12) // KneeRight to FeetRight
                };

                
                foreach (var (start, end) in connections)
                {
                    if (bones2d[start].X > 0 && bones2d[start].Y > 0 &&
                        bones2d[end].X > 0 && bones2d[end].Y > 0)
                    {
                        g.DrawLine(bonePen, bones2d[start].X, bones2d[start].Y,
                                 bones2d[end].X, bones2d[end].Y);
                    }
                }

                
                if (bones2d[2].X > 0 && bones2d[2].Y > 0)
                {
                    g.FillEllipse(Brushes.White, bones2d[2].X - 3, bones2d[2].Y - 3, 6, 6);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawBones error: {ex.Message}");
            }
        }

        private int GetBoneIndexForArrayPosition(int arrayPos)
        {
            return arrayPos switch
            {
                0 => 0,  // Waist
                1 => 5,  // Neck
                2 => 6,  // Head
                3 => 8,  // ShoulderLeft
                4 => 9,  // ForeLeft
                5 => 11, // HandLeft
                6 => 13, // ShoulderRight
                7 => 14, // ForeRight
                8 => 16, // HandRight
                9 => 23, // KneeLeft
                10 => 24, // FeetLeft
                11 => 26, // KneeRight
                12 => 27, // FeetRight
                _ => 0
            };
        }
    }
}