// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

public class Program
{
	static string Stand_DLL_Version = "(THIS SHOULD GET OVERWRITTEN LATER)";

	private static Random random = new Random();

	private const string launchpad_update_version = "1.8.3";

	private const string launchpad_display_version = "1.8.3";

	private  const int width_simple = 248;

	private static readonly int width_advanced;

	private static string[] versions;

	private static string stand_dll;

	private static int gta_pid;

	private static  bool game_was_open;

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
    
    static void Main ()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console. WriteLine("This is based on The Stand Launchpad originally made by Calamity Inc (https://calamity.gg).\nThis is very rough, and was never really meant to be public.\nif you run into issues create an issue on github: https://github.com/larsl2005/StandConsoleInjector\nI am NOT associated with Stand and/or Calamity Inc.\nPasted by 0harmony\nOPTIONS\n[1] Check for Stand updates\n[2] Inject\n[3] Exit");
		ConsoleKey key = Console.ReadKey().Key;
        if (key == ConsoleKey.D1)
            UpdateStart();
		if (key == ConsoleKey.D2)
			inject();
		else
			Main();
    }

    static void EnsureStandFolderExists()
    {
        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand"))
        {
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand");
        }
        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin"))
        {
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin");
        }
    }

    static void UpdateStart()
    {
        Console.Clear();
        EnsureStandFolderExists();
        Console.WriteLine("Checking latest stand version, please hold");
		Console.WriteLine("Latest version: " + GetLatestStandVersion());
		Thread.Sleep(2000);
        downloadStandDll();
		Main();
    }
    
	static string GetLatestStandVersion()
	{
        WebRequest webRequest = WebRequest.Create(new Uri("https://stand.gg/versions.txt"));
        WebResponse response = webRequest.GetResponse();
        StreamReader streamReader = new StreamReader(response.GetResponseStream());
        string text = streamReader.ReadToEnd();
		string[] versions = text.Split(":");
		Stand_DLL_Version = versions[1];
		return versions[1];
    }

    private static string generateRandomString(int length)
    {
	    return new string((from s in Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length)
		    select s[random.Next(s.Length)]).ToArray());
    }
    
    private static void onDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
    {
	    Console.WriteLine("Download Progress: " + e.ProgressPercentage);
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
	    //IL_00a9: Unknown result type (might be due to invalid IL or missing references)
	    bool result = true;
	    Console.WriteLine("Downloading Stand");
	    download_progress = 0;
	    Task task = Task.Run(delegate
	    {
		    WebClient webClient = new WebClient();
		    webClient.DownloadProgressChanged += onDownloadProgress;
		    webClient.DownloadFileCompleted += onDownloadComplete;
		    object obj = new object();
		    lock (obj)
		    {
			    webClient.DownloadFileAsync(new Uri("https://stand.gg/Stand%20" + Stand_DLL_Version + ".dll"), stand_dll + ".tmp", obj);
			    Monitor.Wait(obj);
		    }
	    });
	    do
	    {
	    } while (!task.Wait(20));
	    if(File.Exists(stand_dll))
		    File.Delete(stand_dll);
	    File.Move(stand_dll + ".tmp", stand_dll);
	    Console.WriteLine(stand_dll);
	    if (new FileInfo(stand_dll).Length < 1024)
	    {
		    File.Delete(stand_dll);
		    Console.WriteLine("It looks like the DLL download has failed. Ensure you have no anti-virus program interfering.");
		    result = false;
	    }
	    return result;
    }

    private static unsafe void inject()
	{

		updateGtaPid();
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		
		bool flag = false;
		List<string> list = new List<string>();
		list.Add(stand_dll);
		int num = 0;
		IntPtr intPtr = OpenProcess(1082u, 1, (uint)gta_pid);
		if (intPtr == IntPtr.Zero)
		{
			Console.WriteLine("Failed to get a hold of the game's process.");
		}
		else
		{
			Console.WriteLine("Doing stuff 1");
			IntPtr procAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
			if (procAddress == IntPtr.Zero)
			{
				Console.WriteLine("Failed to find LoadLibraryW.");
			}
			else
			{
                Console.WriteLine("Doing stuff 2");
                string text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Stand\\Bin\\Temp";
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				try
				{
					foreach (string item in list)
					{
						if (!File.Exists(item))
						{
							Console.WriteLine("Couldn't inject " + item + " because the file doesn't exist.\n!!THIS MEANS YOU NEED TO CHECK FOR UPDATES!!");
							continue;
						}
						string text2 = text + "\\SL_" + generateRandomString(5) + ".dll";
						File.Copy(item, text2);
						byte[] bytes = Encoding.Unicode.GetBytes(text2);
						IntPtr intPtr2 = VirtualAllocEx(intPtr, (IntPtr)(void*)null, (IntPtr)bytes.Length, 12288u, 64u);
						if (intPtr2 == IntPtr.Zero)
						{
							Console.WriteLine("Couldn't allocate the bytes to represent " + item);
						}
						else if (WriteProcessMemory(intPtr, intPtr2, bytes, (uint)bytes.Length, 0) == 0)
						{
							Console.WriteLine("Couldn't write " + text2 + " to allocated memory");
						}
						else if (CreateRemoteThread(intPtr, (IntPtr)(void*)null, IntPtr.Zero, procAddress, intPtr2, 0u, (IntPtr)(void*)null) == IntPtr.Zero)
						{
							Console.WriteLine("Failed to create remote thread for " + item);
						}
						else
						{
							num++;
							Console.WriteLine(num);
						}
					}
				}
				catch (IOException)
				{
					Console.WriteLine("Antivirus error or something");
				}
			}
			CloseHandle(intPtr);
		}
		Console.WriteLine("Probably Injected");
		if (num == 0)
		{
			if (!any_successful_injection && list.Count != 0 && !flag)
			{
				Console.WriteLine("Nothing was injected.");
			}
			//EnableReInject();
		}
		Main();

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
                {
                    return;
                }
                gta_pid = process.Id;
                game_was_open = true;
				Console.WriteLine("Gta found: PID " + gta_pid);
                return;
            }
            else
            {
                i++;
            }
        }
        Console.WriteLine("Gta not found");
        bool result = gta_pid != 0;
        gta_pid = 0;
    }

}