#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Numerics;
using CS2ExternalESP;

namespace chrsiroberts
{
    public class Program : Form
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        // Fields
        private static MemoryReader memoryReader;
        private static List<Entity> entities = new List<Entity>();
        private static readonly object entityLock = new object();
        private static IntPtr clientDllAddress;
        private static IntPtr localPlayerController;
        private static bool isRunning = true;
        private static readonly Pen redPen = new(Color.Red, 2);
        private static readonly Pen bluePen = new(Color.Blue, 2);
        private static readonly Font font = new("Arial", 10);
        private static readonly Brush healthBrushRed = new SolidBrush(Color.Red);
        private static readonly Brush healthBrushGreen = new SolidBrush(Color.Green);
        private static readonly Brush textBrush = new SolidBrush(Color.White);
        private Thread renderThread;

        public Program()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.Size = new Size(1920, 1080);
            this.Location = new Point(0, 0);
            this.ShowInTaskbar = false;

            // Add double buffering
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();

            renderThread = new Thread(RenderLoop);
            renderThread.Start();
        }

        private void RenderLoop()
        {
            while (isRunning)
            {
                try
                {
                    
                    Renderer.Update();
                    
                    this.Invoke((MethodInvoker)delegate
                    {
                        this.Invalidate(false);
                    });
                    Thread.Sleep(1); 
                }
                catch
                {
                    
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.Black);

            
            Renderer.DrawMenu(g);

            
            Point screenBottom = new Point(this.Width / 2, this.Height);

            
            using (var enemyBoxPen = new Pen(Renderer.EnemyColor, 2))
            using (var enemyLinePen = new Pen(Color.White, 2)) 
            using (var friendlyBoxPen = new Pen(Renderer.FriendlyColor, 2))
            using (var friendlyLinePen = new Pen(Renderer.FriendlyLineColor, 2))
            {
                lock (entityLock)
                {
                    foreach (var entity in entities)
                    {
                        if (entity != null && entity.Health > 0)
                        {
                            if (entity.Position2D.X > 0 && entity.Position2D.X < this.Width &&
                                entity.Position2D.Y > 0 && entity.Position2D.Y < this.Height)
                            {
                                var boxPen = entity.Team == 2 ? enemyBoxPen : friendlyBoxPen;
                                var linePen = entity.Team == 2 ? enemyLinePen : friendlyLinePen;

                                
                                if (Renderer.ShowESPBoxes)
                                {
                                    float height = Math.Abs(entity.Head2D.Y - entity.Position2D.Y);
                                    float width = height / 2;

                                    
                                    g.DrawLine(enemyLinePen,
                                        screenBottom.X,
                                        screenBottom.Y,
                                        entity.Position2D.X,
                                        entity.Position2D.Y);

                                    // Box ESP
                                    g.DrawRectangle(boxPen,
                                        entity.Position2D.X - width / 2,
                                        entity.Head2D.Y,
                                        width,
                                        height);

                                    // Health Bar
                                    if (Renderer.ShowHealthBars)
                                    {
                                        float healthBarHeight = height;
                                        float healthBarWidth = 5;
                                        float healthPercentage = entity.Health / 100f;

                                        g.FillRectangle(healthBrushRed,
                                            entity.Position2D.X - width / 2 - healthBarWidth - 2,
                                            entity.Head2D.Y,
                                            healthBarWidth,
                                            healthBarHeight);

                                        g.FillRectangle(healthBrushGreen,
                                            entity.Position2D.X - width / 2 - healthBarWidth - 2,
                                            entity.Head2D.Y,
                                            healthBarWidth,
                                            healthBarHeight * healthPercentage);
                                    }

                                    // Distance
                                    string distanceText = $"{entity.Distance:F0}m";
                                    g.DrawString(distanceText, font, textBrush,
                                        entity.Position2D.X - width / 2,
                                        entity.Position2D.Y + 2);
                                }

                                // Bone ESP
                                if (Renderer.ShowBoneESP)
                                {
                                    using (var bonePen = new Pen(Color.White, Renderer.BoneThickness))
                                    {
                                        entity.DrawBones(memoryReader, g, clientDllAddress, bonePen);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static void Main()
        {
            AllocConsole();
            Console.WriteLine("Starting ESP...");

            try
            {
                Process[] processes = Process.GetProcessesByName("cs2");
                if (processes.Length == 0)
                {
                    Console.WriteLine("CS2 not found! Start the game first.");
                    Console.ReadLine();
                    return;
                }

                Console.WriteLine("Found CS2 process.");
                memoryReader = new MemoryReader(processes[0]);
                Console.WriteLine("Memory reader initialized.");

                clientDllAddress = memoryReader.GetModuleAddress("client.dll");
                Console.WriteLine($"Found client.dll at: 0x{clientDllAddress.ToString("X")}");

                localPlayerController = memoryReader.Read<IntPtr>(clientDllAddress + Offsets.dwLocalPlayerController);
                Console.WriteLine($"Local player controller: 0x{localPlayerController.ToString("X")}");

                if (localPlayerController == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to read local player controller. Check offsets.");
                    Console.ReadLine();
                    return;
                }

                Thread entityThread = new Thread(EntityLoop);
                entityThread.Start();

                Application.Run(new Program());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ReadLine();
            }
        }

        private static void EntityLoop()
        {
            while (isRunning)
            {
                try
                {
                    var entityList = memoryReader.Read<IntPtr>(clientDllAddress + Offsets.dwEntityList);
                    if (entityList == IntPtr.Zero)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    lock (entityLock)
                    {
                        entities.Clear();
                    }

                    for (int i = 0; i < 64; i++)
                    {
                        try
                        {
                            long listEntryAddress = entityList.ToInt64() + (0x8 * ((i & 0x7FFF) >> 9) + 0x10);
                            var listEntry = memoryReader.Read<IntPtr>(new IntPtr(listEntryAddress));
                            if (listEntry == IntPtr.Zero) continue;

                            long controllerAddress = listEntry.ToInt64() + (0x78 * (i & 0x1FF));
                            var controller = memoryReader.Read<IntPtr>(new IntPtr(controllerAddress));
                            if (controller == IntPtr.Zero) continue;

                            var pawnHandle = memoryReader.Read<uint>(controller + Offsets.m_hPlayerPawn);
                            long pawnListEntryAddress = entityList.ToInt64() + (0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
                            var pawnListEntry = memoryReader.Read<IntPtr>(new IntPtr(pawnListEntryAddress));
                            if (pawnListEntry == IntPtr.Zero) continue;

                            long pawnAddress = pawnListEntry.ToInt64() + (0x78 * (pawnHandle & 0x1FF));
                            var pawn = memoryReader.Read<IntPtr>(new IntPtr(pawnAddress));
                            if (pawn == IntPtr.Zero) continue;

                            var entity = new Entity { Address = pawn };
                            if (entity.Update(memoryReader, clientDllAddress, localPlayerController))
                            {
                                lock (entityLock)
                                {
                                    entities.Add(entity);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Entity processing error: {ex.Message}");
                            continue;
                        }
                    }

                    Thread.Sleep(16);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"EntityLoop error: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isRunning = false;
            if (renderThread != null && renderThread.IsAlive)
            {
                renderThread.Join();
            }
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x20;  // WS_EX_LAYERED | WS_EX_TRANSPARENT
                return cp;
            }
        }
    }
}