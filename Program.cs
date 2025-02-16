//code written by ciberboy, credit if using
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

class INJECT_DLL
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

    const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    const uint MEM_COMMIT = 0x00001000;
    const uint MEM_RESERVE = 0x00002000;
    const uint PAGE_READWRITE = 0x04;

    static void PauseOnError(string message)
    {
        Console.WriteLine($"{message} (error code: {Marshal.GetLastWin32Error()})");
        Console.WriteLine("press ENTER key to exit...");
        Console.ReadLine();
    }

    public static void InjectDll(int processId, string dllPath)
    {
        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            PauseOnError("ERR1: Could not open process.");
            return;
        }
        IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (allocMem == IntPtr.Zero)
        {
            PauseOnError("ERR2: Could not allocate memory.");
            CloseHandle(hProcess);
            return;
        }
        byte[] buffer = Encoding.ASCII.GetBytes(dllPath + "\0"); 
        uint bytesWritten;
        if (!WriteProcessMemory(hProcess, allocMem, buffer, (uint)buffer.Length, out bytesWritten))
        {
            PauseOnError("ERR3: can't write memory");
            CloseHandle(hProcess);
            return;
        }
        IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
        if (kernel32Handle == IntPtr.Zero)
        {
            PauseOnError("ERR4: Could not get handle for kernel32");
            CloseHandle(hProcess);
            return;
        }
        IntPtr loadLibraryAddr = GetProcAddress(kernel32Handle, "LoadLibraryA");
        if (loadLibraryAddr == IntPtr.Zero)
        {
            PauseOnError("ERR5: Problem loading LoadLibraryA");
            CloseHandle(hProcess);
            return;
        }
        IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMem, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            PauseOnError("ERR6: Couldn't make remote thread!");
            CloseHandle(hProcess);
            return;
        }
        Console.WriteLine("dll has been injected!!!");
        CloseHandle(hThread);  
        CloseHandle(hProcess); 
    }

    static void Main()
    {
        Console.WriteLine("Welcome to DLL Injector by CiberBoy! You can inject any DLL to a running process...");
        Console.BackgroundColor = ConsoleColor.Green;
        Console.WriteLine("WARNING: PLEASE DO NOT INJECT DLLS TO CRITICAL PROCESS OR YOU MAY CAUSE DATA LOSS AND CRASHES!!");
        Console.WriteLine("Enter the process to do the dll injection:");
        string processP = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(processP))
        {
            PauseOnError("process name is not correct.");
            return;
        }
        Console.WriteLine("now, introduce the path to the DLL: ");
        string dllp = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(dllp))
        {
            PauseOnError("dll path is incorrect.");
            return;
        }
        var processes = Process.GetProcessesByName(processP);
        if (processes.Length == 0)
        {
            PauseOnError("process doesn't exist");
            return;
        }
        InjectDll(processes[0].Id, dllp);
    }
}
