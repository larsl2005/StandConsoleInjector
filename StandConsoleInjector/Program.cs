// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

public class Program
{
	static string Stand_DLL_Version = "(THIS SHOULD GET OVERWRITTEN LATER)";
	static string processPath = Process.GetCurrentProcess().MainModule.FileName;
	static object lockObj = new object();




	private static Random random = new Random();

	private const string launchpad_update_version = "1.8.3";

	private const string launchpad_display_version = "1.8.3";

	private const int width_simple = 248;

	private static readonly int width_advanced;

	private static string[] versions;

	private static string stand_dll;

	private static int gta_pid;

	private static bool game_was_open;

	private static bool can_auto_inject = true;

	private static bool any_successful_injection;

	private static int download_progress;

	private static IContainer components;

	private static Timer ProcessScanTimer;

	private static Timer AutoInjectTimer;

	private static Timer GameClosedTimer;

	private static Timer UpdateTimer;

	private static Timer ReInjectTimer;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern int CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType,
		uint flProtect);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size,
		int lpNumberOfBytesWritten);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize,
		IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

	static void Main(string[] args)
	{
		Console.ForegroundColor = ConsoleColor.Magenta;
		if (!args.Contains("-NuhUh"))
			CheckIfRunningInShittyHackMode(args);
		_Main();
	}

    static void CheckIfRunningInShittyHackMode(string[] args)
    {
        string LauncArgs = "";
        string eeee = "";

        bool IsEpicAndShouldRelaunch = false;
        bool IsRunningHackyMode = false;
        string ProcNameToStart = "N/A";

        foreach (string arg in args)
        {
            if (arg == "-StraightIntoFreemode")
                Console.WriteLine("\nBRO WHY THE HECK WOULD YOU DO THAT!!! XDDDDD");
            LauncArgs = LauncArgs + arg + " ";
            goto Skip; 
        Skip:
            eeee = eeee + arg.GetType().ToString() + " (" + arg + ") ";
        }

        if (processPath.ToLower().Contains("playgtav"))
            ProcNameToStart = "_PlayGTAV.exe";

        if (File.Exists("Redistributables\\Rockstar-Games-Epic.exe"))
            IsEpicAndShouldRelaunch = true;

        if (ProcNameToStart != "N/A")
            IsRunningHackyMode = true;

        if (IsRunningHackyMode)
        {
            if (File.Exists(ProcNameToStart))
                goto Whatever;

            Console.WriteLine("\nCould not find renamed " + ProcNameToStart.Substring(1) + ", enter the new name manually");
            ProcNameToStart = Console.ReadLine();
            if (!ProcNameToStart.Contains(".exe"))
                ProcNameToStart = ProcNameToStart + ".exe";

        Whatever:
            Process.Start(ProcNameToStart, LauncArgs);

            if (IsEpicAndShouldRelaunch)
                Process.Start(processPath, "-NuhUh " + LauncArgs);
        }
    }

	static void InstallLauncherForShittyHackMode()
	{
		if (!processPath.ToLower().Contains("playgtav"))
		{
			string ToPrint = "READ THIS ENTIRELY BEFORE INSTALLING:\nI have no idea if injecting this way is detected, I haven't tested it long enough to know (I don't THINK it is though)\n" +
				"USE AT YOUR OWN RISK!!!!\nThis will make the injector launch through your desired launcher, then the injector will launch the game, this makes it easier to run under linux\n"
				+ "THIS WILL MOST LIKELY ONLY WORK WITH THE DOTNET NATIVE VERSION OF THE INJECTOR, MAKE SURE YOU ARE USING THAT VERSION!\n\n\nPress any key to continue..";

			Console.WriteLine(ToPrint);
			Console.ReadKey();
			Console.Clear();
		Insert:
			Console.WriteLine("Insert your gta install directory");
			string dir = Console.ReadLine();
			if (File.Exists(dir + "\\PlayGTAV.exe") || File.Exists(dir + "/PlayGTAV.exe"))
			{
				if (File.Exists(dir + "\\_PlayGTAV.exe") || File.Exists(dir + "/_PlayGTAV.exe"))
				{
					Console.WriteLine("\nLauncher most likely already installed!");
					Console.WriteLine("\nDo you want to:\n[1] Update\n[2] Uninstall\n[3] Cancel\nIf your game just updated. and the launcher does NOT work, press update");
				UpdateKeyInput:
					ConsoleKey key = Console.ReadKey().Key;
					if (key == ConsoleKey.D1)
						goto Skip;
					else if (key == ConsoleKey.D2)
						Uninstall(dir);
					else if (key == ConsoleKey.D3)
					{
						Console.WriteLine("\n");
						_Main();
					}
					else
						goto UpdateKeyInput;
				}

				Console.WriteLine("\nFound gta launcher!");
				File.Copy(dir + "\\PlayGTAV.exe", dir + "\\_PlayGTAV.exe");

			Skip:
				File.Delete(dir + "\\PlayGTAV.exe");
				File.Copy(processPath, dir + "\\PlayGTAV.exe");
				Console.WriteLine("\nSuccess!\n");
				Thread.Sleep(2000);
				_Main();
			}
			else
			{
				Console.WriteLine("\nGta V Not found!");
				goto Insert;
			}
		}
		else
		{
			Console.WriteLine("\nCannot install launcher as launcher!");
		}
	}

	static void Uninstall(string dir)
	{
		if (processPath.Contains("PlayGTAV"))
		{
			Console.WriteLine("\nCannot uninstall launcher from launcher mode\nRun the injector outside the GTA folder AND with a different name that \"PlayGTAV.exe\"");
			Thread.Sleep(2000);
			_Main();
		}
		else
		{
			if (!IsAdministrator())
				Console.WriteLine("\n\nNot running as admin, uninstall/cleanup might fail!");
			File.Delete(dir + "\\PlayGTAV.exe");
			File.Copy(dir + "\\_PlayGTAV.exe", dir + "\\PlayGTAV.exe");
			try
			{
				File.Delete(dir + "\\_PlayGTAV.exe");
			}
			catch
			{
				Console.WriteLine("\nFailed to cleanup, but original launcher has been restored");
			}
			Console.WriteLine("\nUninstalled");
			Thread.Sleep(2000);
			_Main();
		}
	}

	static void _Main()
	{
		GetLatestStandVersion().Wait();


		Console.ForegroundColor = ConsoleColor.Magenta;
		Console.WriteLine("This is based on The Stand Launchpad originally made by Calamity Inc (https://calamity.gg).\nThis is very rough, and was never really meant to be public." +
			"\nif you run into issues create an issue on github: https://github.com/larsl2005/StandConsoleInjector\nI am NOT associated with Stand and/or Calamity Inc." +
			"\nPasted by 0harmony\n\nOPTIONS\n[1] Check for Stand updates\n[2] Inject\n[3] Install/Uninstall launcher\n[4] Exit");
		ConsoleKey key = Console.ReadKey().Key;
		Console.Clear();
		switch (key)
		{
			case ConsoleKey.D1:
				UpdateStart();
				return;
			case ConsoleKey.D2:
				inject();
				return;
			case ConsoleKey.D3:
				InstallLauncherForShittyHackMode();
				return;
			case ConsoleKey.D4:
				Environment.Exit(0);
				return;
			default:
				_Main();
				return;
		}
	}

	static void EnsureStandFolderExists()
	{
		if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand"))
			Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand");
		if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin"))
			Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin");
	}

	static void UpdateStart()
	{
		Console.Clear();
		EnsureStandFolderExists();
		Console.WriteLine("Checking latest stand version, please hold");
		Console.WriteLine("\nLatest version: " + Stand_DLL_Version);
		Thread.Sleep(2000);
		downloadStandDll();
		_Main();
	}

	private static async Task<string> GetLatestStandVersion()
	{
		using (HttpClient httpClient = new HttpClient())
		{
			HttpResponseMessage response = await httpClient.GetAsync("https://stand.gg/versions.txt");
			response.EnsureSuccessStatusCode();
			
			string text = await response.Content.ReadAsStringAsync();
			string[] versions = text.Split(":");
			
			Stand_DLL_Version = versions[1].Trim();
			return Stand_DLL_Version;
		}
	}


	private static string generateRandomString(int length)
	{
		return new string((from s in Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length)
						   select s[random.Next(s.Length)]).ToArray());
	}

	private static void onDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
	{
		lock (lockObj) 
		{
			if (e.ProgressPercentage > download_progress)
			{
				download_progress = e.ProgressPercentage;
				Console.WriteLine("Download Progress: " + e.ProgressPercentage);
			}
		}
	}
	private static void onDownloadComplete(object sender, AsyncCompletedEventArgs e)
	{
		lock (e.UserState)
		{
			Monitor.Pulse(e.UserState);
		}
	}

private static bool downloadStandDll()
{
    stand_dll = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin\\Stand " + Stand_DLL_Version + ".dll";
    if (File.Exists(stand_dll))
    {
        Console.WriteLine("\nStand DLL already updated, skipping download\n");
        Thread.Sleep(2000);
        return false;
    }

    Console.WriteLine("\nDownloading Stand");
    download_progress = 0;

    Task task = Task.Run(() =>
    {
        using (WebClient webClient = new WebClient())
        {
            webClient.DownloadProgressChanged += onDownloadProgress;
            webClient.DownloadFileCompleted += onDownloadComplete;

            object obj = new object();
            lock (obj)
            {
                webClient.DownloadFileAsync(new Uri("https://stand.gg/Stand%20" + Stand_DLL_Version + ".dll"), stand_dll + ".tmp", obj);
                Monitor.Wait(obj);
            }
        }
    });

    do
    {
        Thread.Sleep(20);
    } while (!task.Wait(20));

    if (File.Exists(stand_dll))
    {
        File.Delete(stand_dll);
    }

    File.Move(stand_dll + ".tmp", stand_dll);
    Console.WriteLine(stand_dll + "\n");

    if (new FileInfo(stand_dll).Length < 1024)
    {
        File.Delete(stand_dll);
        Console.WriteLine("\nIt looks like the DLL download has failed. Ensure you have no anti-virus program interfering.");
        return false;
    }

    Thread.Sleep(2000);
    return true;
}



	private static unsafe void inject()
	{

		updateGtaPid();
		bool flag = false;
		List<string> list = new List<string>();
		stand_dll = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin\\Stand " + Stand_DLL_Version + ".dll";
    	if (File.Exists(stand_dll))
    	{
            list.Add(stand_dll);
        }
		int num = 0;
		IntPtr intPtr = OpenProcess(1082u, 1, (uint)gta_pid);
		if (intPtr == IntPtr.Zero)
		{
			Console.WriteLine("\nFailed to get a hold of the game's process.");
		}
		else
		{
			IntPtr procAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
			if (procAddress == IntPtr.Zero)
				Console.WriteLine("\nFailed to find LoadLibraryW.");
			else
			{
				string text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin\\Temp";
				if (!Directory.Exists(text))
					Directory.CreateDirectory(text);
				try
				{
					foreach (string item in list)
					{
						if (!File.Exists(item))
						{
							Console.WriteLine("\nCouldn't inject " + item + " because the file doesn't exist.\n!!THIS MEANS YOU NEED TO CHECK FOR UPDATES!!");
							continue;
						}
						string text2 = text + "\\SL_" + generateRandomString(5) + ".dll";
						File.Copy(item, text2);
						byte[] bytes = Encoding.Unicode.GetBytes(text2);
						IntPtr intPtr2 = VirtualAllocEx(intPtr, (IntPtr)(void*)null, (IntPtr)bytes.Length, 12288u, 64u);
						if (intPtr2 == IntPtr.Zero)
							Console.WriteLine("\nCouldn't allocate the bytes to represent " + item);
						else if (WriteProcessMemory(intPtr, intPtr2, bytes, (uint)bytes.Length, 0) == 0)
							Console.WriteLine("\nCouldn't write " + text2 + " to allocated memory");
						else if (CreateRemoteThread(intPtr, (IntPtr)(void*)null, IntPtr.Zero, procAddress, intPtr2, 0u, (IntPtr)(void*)null) == IntPtr.Zero)
							Console.WriteLine("\nFailed to create remote thread for " + item);
						else
						{
							num++;
							Console.WriteLine(num);
						}
					}
				}
				catch (IOException)
				{
					Console.WriteLine("\nAntivirus error or something");
				}
			}
			CloseHandle(intPtr);
		}

		if (num == 0)
		{
			if (!any_successful_injection && list.Count != 0 && !flag)
				Console.WriteLine("\nFailed (Nothing was injected.)\n");
		}
		else
		{
			Console.WriteLine("\nSuccesfully (Injected)\n");
		}
		Thread.Sleep(2000);
		_Main();

	}

	private static void updateGtaPid()
	{
		Process[] processes = Process.GetProcesses();
		int i = 0;
		while (i < processes.Length)
		{
			Process process = processes[i];
			if (process.ProcessName == "GTA5")
			{
				if (gta_pid == process.Id)
					return;
				gta_pid = process.Id;
				game_was_open = true;
				Console.WriteLine("\nGta found: PID " + gta_pid);
				return;
			}
			else
				i++;
		}
		Console.WriteLine("\nGta not found");
		bool result = gta_pid != 0;
		gta_pid = 0;
	}


	private static bool IsAdministrator()
	{
		WindowsIdentity identity = WindowsIdentity.GetCurrent();
		WindowsPrincipal principal = new WindowsPrincipal(identity);
		return principal.IsInRole(WindowsBuiltInRole.Administrator);
	}

}