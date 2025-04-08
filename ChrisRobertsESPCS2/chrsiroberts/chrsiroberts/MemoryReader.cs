using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace chrsiroberts
{
    public class MemoryReader
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("ntdll.dll")]
        private static extern int NtDuplicateObject(
            IntPtr SourceProcessHandle,
            IntPtr SourceHandle,
            IntPtr TargetProcessHandle,
            out IntPtr TargetHandle,
            uint DesiredAccess,
            uint HandleAttributes,
            uint Options
        );

        private delegate int NtReadVirtualMemoryDelegate(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            [Out] byte[] Buffer,
            int BufferSize,
            out int NumberOfBytesRead
        );

        private readonly NtReadVirtualMemoryDelegate _ntReadVirtualMemory;
        private readonly IntPtr processHandle;
        private readonly Process process;
        private IntPtr _clientDllBaseAddress = IntPtr.Zero;
        private IntPtr _engineDllBaseAddress = IntPtr.Zero;

        // Unique identifier for your cheat
        private const string CheatIdentifier = "ChrisRoberts_v0.1";

        public MemoryReader(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            this.process = process;

            // Get handle from a legitimate process
            IntPtr sourceHandle = GetProcessHandle("explorer.exe");
            NtDuplicateObject(sourceHandle, IntPtr.Zero, IntPtr.Zero, out processHandle, 0x0010, 0, 0x2);

            if (processHandle == IntPtr.Zero)
            {
                
                processHandle = OpenProcess(0x0010, false, process.Id);
            }

            if (processHandle == IntPtr.Zero)
                throw new Exception("Failed to open process. Ensure the game is running and the process ID is correct.");

            //great anti cheat vac
            IntPtr ntdllHandle = GetModuleHandle("ntdll.dll");
            IntPtr ntReadVirtualMemoryPtr = GetProcAddress(ntdllHandle, "NtReadVirtualMemory");
            _ntReadVirtualMemory = Marshal.GetDelegateForFunctionPointer<NtReadVirtualMemoryDelegate>(ntReadVirtualMemoryPtr);
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            try
            {
                int size = Marshal.SizeOf<T>();
                byte[] buffer = new byte[size];
                int bytesRead;

                int status = _ntReadVirtualMemory(processHandle, address, buffer, size, out bytesRead);
                if (status != 0) // 0 means success
                {
                    throw new Exception($"Failed to read memory at 0x{address.ToString("X")}");
                }

                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Memory read error at 0x{address.ToString("X")}: {ex.Message}");
                return default;
            }
        }

        public IntPtr GetModuleAddress(string moduleName)
        {
            if (process?.Modules == null) return IntPtr.Zero;

            foreach (ProcessModule module in process.Modules)
            {
                if (module?.ModuleName != null &&
                    module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }

            throw new Exception($"Module {moduleName} not found");
        }

        public bool ReadBytes(IntPtr address, byte[] buffer, int size)
        {
            try
            {
                int bytesRead;
                int status = _ntReadVirtualMemory(processHandle, address, buffer, size, out bytesRead);
                return status == 0; // 0 means success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read bytes from memory: {ex.Message}");
                return false;
            }
        }

        public IntPtr GetClientDllBaseAddress()
        {
            if (_clientDllBaseAddress == IntPtr.Zero)
            {
                _clientDllBaseAddress = FindModuleBaseAddress("client.dll");
            }
            return _clientDllBaseAddress;
        }

        public IntPtr GetEngineDllBaseAddress()
        {
            if (_engineDllBaseAddress == IntPtr.Zero)
            {
                _engineDllBaseAddress = FindModuleBaseAddress("engine.dll");
            }
            return _engineDllBaseAddress;
        }

        private IntPtr FindModuleBaseAddress(string moduleName)
        {
            IntPtr[] moduleHandles = new IntPtr[1024];
            uint cbNeeded;
            if (EnumProcessModules(processHandle, moduleHandles, (uint)(moduleHandles.Length * IntPtr.Size), out cbNeeded))
            {
                for (int i = 0; i < cbNeeded / IntPtr.Size; i++)
                {
                    StringBuilder moduleNameBuilder = new StringBuilder(260);
                    if (GetModuleFileNameEx(processHandle, moduleHandles[i], moduleNameBuilder, moduleNameBuilder.Capacity) > 0)
                    {
                        if (moduleNameBuilder.ToString().EndsWith(moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            return moduleHandles[i];
                        }
                    }
                }
            }
            return IntPtr.Zero;
        }

        ~MemoryReader()
        {
            if (processHandle != IntPtr.Zero)
                CloseHandle(processHandle);
        }

        
        private static IntPtr GetProcessHandle(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                return processes[0].Handle;
            }
            return IntPtr.Zero; 
        }
    }
}