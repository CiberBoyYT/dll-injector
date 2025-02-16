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

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    const uint MEM_COMMIT = 0x00001000;
    const uint MEM_RESERVE = 0x00002000;
    const uint PAGE_READWRITE = 0x04;

    public static void InjectDll(int processId, string dllPath)
    {
        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("problem when opening process");
            return;
        }
        IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE); //get memory allocated

        if (allocMem == IntPtr.Zero)
        {
            Console.WriteLine("problem allocate memory");
            return;
        }
        byte[] buffer = Encoding.Default.GetBytes(dllPath); //dll to byte
        uint bytesWritten;
        if (!WriteProcessMemory(hProcess, allocMem, buffer, (uint)buffer.Length, out bytesWritten)) //write process to memory
        {
            Console.WriteLine("error write memory");
            return;
        }
        IntPtr loadLibraryAddr = GetProcAddress(LoadLibrary("kernel32.dll"), "LoadLibraryA");
        if (loadLibraryAddr == IntPtr.Zero)
        {
            Console.WriteLine("error loadlibraryA");
            return;
        }
        IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMem, 0, IntPtr.Zero);

        if (hThread == IntPtr.Zero)
        {
            Console.WriteLine("error create remote thred");
            return;
        }

        Console.WriteLine("dll injected correctly :)");
        CloseHandle(hProcess); //close process handle
        Console.WriteLine("process handle closed, we dont need it now");
    }
    static void Main()
    {
        Console.WriteLine("Welcome to dllinjector, in what process you want to inject DLL??");
        string processP = Console.ReadLine();
        if (processP == "")
        {
            Console.WriteLine("you havent entered anything, exiting!!!!");
            Process.GetCurrentProcess().Kill();
        }
        Console.WriteLine("okay, now insert the dll you wanna inject");
        string dllp = Console.ReadLine();
        if (dllp == "")
        {
            Console.WriteLine("you didn't enter a correct dllpath, exiting");
            Process.GetCurrentProcess().Kill();
        }
        var process = Process.GetProcessesByName(processP); //find process
        if (process.Length == 0)
        {
            Console.WriteLine("the process you put is not running, aborting.");
            return;
        }
        InjectDll(process[0].Id, dllp);
        Console.WriteLine("dll inject start!!!");
    }
}
