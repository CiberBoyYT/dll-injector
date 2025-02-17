/*
Code Written by CiberBoy and protected with GPL 3 license.
To use this code, you must credit CiberBoy and make your program open source or at least the part of the code where my code appears.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

class DllInjector
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibraryA(string lpLibFileName);

    const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    const uint MEM_COMMIT = 0x00001000;
    const uint MEM_RESERVE = 0x00002000;
    const uint PAGE_READWRITE = 0x04;

    static void PauseOnError(string message)
    {
        Console.WriteLine($"{message} (Error Code: {Marshal.GetLastWin32Error()})");
        Console.WriteLine("Press ENTER to exit..");
        Console.ReadLine();
    }

    static bool IsManagedDll(string dllPath)
    {
        try
        {
            AssemblyName.GetAssemblyName(dllPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static void nativeDll(int processId, string dllPath)
    {
        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            PauseOnError("ERR1A: Could not open process.");
            return;
        }
        IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (allocMem == IntPtr.Zero)
        {
            PauseOnError("ERR2A: Could not allocate memory.");
            CloseHandle(hProcess);
            return;
        }
        byte[] buffer = Encoding.ASCII.GetBytes(dllPath + "\0");
        uint bytesWritten;
        if (!WriteProcessMemory(hProcess, allocMem, buffer, (uint)buffer.Length, out bytesWritten))
        {
            PauseOnError("ERR3A: Could not write memory.");
            CloseHandle(hProcess);
            return;
        }
        IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
        IntPtr loadLibraryAddr = GetProcAddress(kernel32Handle, "LoadLibraryA");
        if (loadLibraryAddr == IntPtr.Zero)
        {
            PauseOnError("ERR4A: Could not get address of LoadLibraryA.");
            CloseHandle(hProcess);
            return;
        }
        IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMem, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            PauseOnError("ERR5A: Could not create remote thread.");
            CloseHandle(hProcess);
            return;
        }

        Console.WriteLine("DLL injected correctly!! Closing handle...");
        CloseHandle(hThread);
        CloseHandle(hProcess);
        Console.WriteLine("handle closed successfully! press ENTER to exit");
    }
    public static void managedDll(int processId, string dllPath, string methodName)
    {
        try
        {
            Assembly assembly = Assembly.LoadFile(dllPath);
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    Console.WriteLine($"Executing {methodName} from {type.FullName}");
                    method.Invoke(null, null);
                    Console.WriteLine("managed dll worked successfully!!");
                    return;
                }
            }
            PauseOnError("ERR1B: error getting method");
        }
        catch (Exception ex)
        {
            PauseOnError($"an expection occurred: {ex.Message}");
        }
    }

    static void Main()
    {
        if (MessageBox.Show("The program you're attempting to run is a DLL injector that can modify processes in memory.\nThis software itself isn't dangerous, but using it the wrong way, for example, injecting DLLs to system processes,\ncan cause crashes, data loss and additional damages to your system.\nUse this program with caution and only if you know what you're doing.\nContinue?", "DLL Injector by CiberBoy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            Console.WriteLine("Welcome to DLL Injector by CiberBoy! You can inject any DLL to a running process...");
            Console.WriteLine("WARNING: PLEASE DO NOT INJECT DLLS TO CRITICAL PROCESS OR YOU MAY CAUSE DATA LOSS AND CRASHES!!");
            Console.WriteLine("enter process name to inject:");
            string processP = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(processP))
            {
                PauseOnError("wrong process name.");
                return;
            }
            Console.WriteLine("Enter full DLL path:");
            string dllp = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(dllp) || !File.Exists(dllp))
            {
                PauseOnError("DLL path is not valid");
                return;
            }
            var processes = Process.GetProcessesByName(processP);
            if (processes.Length == 0)
            {
                PauseOnError("ERR1C: process doesn't exist!");
                return;
            }
            int processId = processes[0].Id;
            Console.WriteLine($"detect process ID: {processId}");
            if (IsManagedDll(dllp))
            {
                Console.WriteLine("managed DLL detected");
                Console.Write("enter the method name to call (default is 'Run'): ");
                string methodName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(methodName))
                    methodName = "Run";
                managedDll(processId, dllp, methodName);
            }
            else
            {
                Console.WriteLine("native dll detected, injecting just began...");
                nativeDll(processId, dllp);
            }
        }
    else
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
