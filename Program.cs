using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LaunchThenClose
{
    public class Program
    {
        static string _launchPath = string.Empty;
        static string _processKill = string.Empty;
        static double _closeDelay = 12d;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Logger.Write($"🔔 CurrentUserSID returns \"{ScheduledTaskHelper.GetCurrentUserSid()}\"", level: LogLevel.Debug);
           
            LogDomainAssemblies();

            #region [Exception Events]
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (e.Exception != null &&
                   !e.Exception.Message.StartsWith("Could not load file or assembly") &&
                   !e.Exception.Message.StartsWith("Could not find a part of the path") &&
                   !e.Exception.Message.StartsWith("The symbolic link cannot be followed") &&
                   !e.Exception.Message.StartsWith("The process cannot access the file") &&
                   !e.Exception.Message.StartsWith("The operation was canceled") &&
                   !e.Exception.Message.StartsWith("Access to the path"))
                {
                    Console.WriteLine($"\r\n⚠️ FirstChanceException: {e.Exception.Message}");
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine($"⚠️ Unhandled exception: {e.ExceptionObject}");
                Console.WriteLine();
                Console.WriteLine($"🔔 Press any key to close.");
                _ = Console.ReadKey();
            };
            #endregion

            var trust = AppDomain.CurrentDomain.ApplicationTrust;
            if (!trust.IsApplicationTrustedToRun)
            {
                AppDomain.CurrentDomain.ApplicationTrust.IsApplicationTrustedToRun = true; // Ensure the application is trusted to run
            }

            Console.WriteLine($"🔔 Checking principal identity…");
            if (!IsRunningAsAdmin())
            {
                RelaunchAsAdmin();
                return; // Stop the non-admin instance
            }

            var addTask = CommandLineHelper.GetFirstTaskValue(args);
            if (!string.IsNullOrWhiteSpace(addTask) && addTask.StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"🔔 Adding scheduled task to run at logon…");
                var exePath = Process.GetCurrentProcess().MainModule.FileName;
                var taskName = "LaunchThenClose";
                var taskExists = ScheduledTaskHelper.TaskExists(taskName);
                if (taskExists)
                {
                    Console.WriteLine($"⚠️ Scheduled task \"{taskName}\" already exists.");
                }
                else
                {
                    // XML template method is more flexible.
                    var success = ScheduledTaskHelper.CreateTaskFromXml(
                        appName: "LaunchThenClose",
                        exePath: exePath,
                        workingDirectory: AppDomain.CurrentDomain.BaseDirectory,
                        description: "Launches GIGABYTE Control Center and closes it after a delay.",
                        author: Environment.UserName,
                        userSid: "S-1-5-18"); // SYSTEM

                    if (success)
                    {
                        Console.WriteLine($"✅ Scheduled task \"{taskName}\" created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"❎ Failed to create scheduled task \"{taskName}\". Check log for more details.");
                    }
                }
                Console.WriteLine();
                Console.WriteLine($"🔔 Press any key to close.");
                _ = Console.ReadKey();
                return; // Exit after adding the task
            }
            else if (!string.IsNullOrWhiteSpace(addTask) && addTask.StartsWith("false", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"🔔 Checking if scheduled task exists…");
                var taskName = "LaunchThenClose";
                var taskExists = ScheduledTaskHelper.TaskExists(taskName);
                if (!taskExists)
                {
                    Console.WriteLine($"⚠️ Scheduled task \"{taskName}\" not found.");
                }
                else
                {
                    Console.WriteLine($"🔔 Removing scheduled task…");
                    var success = ScheduledTaskHelper.RemoveTask(taskName);
                    if (success)
                    {
                        Console.WriteLine($"✅ Scheduled task \"{taskName}\" removed successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"❎ Failed to remove scheduled task \"{taskName}\". Check log for more details.");
                    }

                }
            }

            ConfigManager.OnError += OnConfigError;

            Console.WriteLine($"🔔 Reading local config…");
            _launchPath = ConfigManager.Get("LaunchPath", defaultValue: @"C:\Program Files\GIGABYTE\Control Center\LaunchGCC.exe");
            _processKill = ConfigManager.Get("ProcessToKill", defaultValue: "GCC");
            _closeDelay = ConfigManager.Get("DelaySeconds", defaultValue: 18d); // This may beed to be adjusted on a startup due to other apps loading.

            IntPtr handle = NativeMethods.GetConsoleWindow();
            if (handle != IntPtr.Zero)
                NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);

            Console.WriteLine($"🔔 Starting process…");
            await LaunchAndCloseAsync(_launchPath, TimeSpan.FromSeconds(_closeDelay));
            
            ConfigManager.Set("LaunchPath", value: _launchPath);
            ConfigManager.Set("ProcessToKill", value: _processKill);
            ConfigManager.Set("DelaySeconds", value: _closeDelay);
        }

        static void OnConfigError(object sender, Exception e)
        {
            IntPtr handle = NativeMethods.GetConsoleWindow();
            if (handle != IntPtr.Zero)
                NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);

            Console.WriteLine($"⚠️ ConfigManager error: {e.Message}");
            Console.WriteLine();
            Console.WriteLine($"🔔 Press any key to close.");
            _ = Console.ReadKey();
        }

        /// <summary>
        /// Launches an application, waits for it to start, then closes it after a delay.
        /// </summary>
        /// <param name="filePath">Full path to the executable.</param>
        /// <param name="delayMilliseconds">How long to wait before closing the app.</param>
        static async Task LaunchAndCloseAsync(string filePath, TimeSpan delay)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("⚠️ File path cannot be null or empty.");
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                }
            };

            process.Start();

            // Wait for the process to be fully running
            process.WaitForInputIdle(); // Works for GUI apps; safe to call even if it doesn't apply

            // Delay for the specified time
            await Task.Delay(delay);

            try
            {
                var nextProcess = GetProcessRunning(_processKill);
                if (nextProcess != null)
                {
                    Console.WriteLine($"🔔 Closing process…");
                    nextProcess.CloseMainWindow(); // Ask it to exit nicely
                    nextProcess.WaitForExit(3000); // Give it 3 seconds to close
                    // Force close just in case
                    try 
                    {
                        if (!nextProcess.HasExited) 
                        {
                            nextProcess.Kill(); 
                            Console.WriteLine($"✅ Killed");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Closed gracefully");
                        }
                    }
                    catch (Exception inex)
                    {
                        Console.WriteLine($"❎ {inex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Process has already exited.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error closing process: {ex.Message}");
            }
            await Task.Delay(3000);
        }

        #region ◁ Helpers ▷
        /// <summary>
        /// Gets the assemblies loaded in the current execution context of the application domain.
        /// </summary>
        static void LogDomainAssemblies()
        {
            //var framework = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName; // ".NETFramework,Version=v4.8"
            //var appbase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            //var appname = AppDomain.CurrentDomain.SetupInformation.ApplicationName;
            //var cfgfile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            //var cachepath = AppDomain.CurrentDomain.SetupInformation.CachePath ?? Path.GetTempPath();
            //AppDomain.CurrentDomain.SetupInformation.AppDomainInitializer += (string[] arguments) => { Console.WriteLine($"[AppDomainInitializer] Length={arguments.Length}"); };
            //AppDomain.CurrentDomain.ProcessExit += (sender, e) => { Console.WriteLine($"Process exited at {DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt", CultureInfo.InvariantCulture)}"); };

            try
            {
                Logger.Write($"🔔 CurrentDomain.BaseDirectory returns \"{AppDomain.CurrentDomain.BaseDirectory}\"", level: LogLevel.Debug);
                Logger.Write($"🔔 Traget framework returns \"{AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName}\"", level: LogLevel.Debug);
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assem in assemblies)
                {
                    var name = assem.GetName().FullName;
                    var pkt = assem.GetName().GetPublicKeyToken();
                    if (pkt != null && pkt.Length > 0) // ignore null/empty keys
                    {
                        Logger.Write($"🎛️ Assembly: {name}", level: LogLevel.Info);
                    }
                }
                Console.WriteLine();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Gets the assemblies loaded in the current execution context of the application domain.
        /// </summary>
        static void LogDomainAssembliesUsingThread()
        {
            //var framework = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName; // ".NETFramework,Version=v4.8"
            //var appbase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            //var appname = AppDomain.CurrentDomain.SetupInformation.ApplicationName;
            //var cfgfile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            //var cachepath = AppDomain.CurrentDomain.SetupInformation.CachePath ?? Path.GetTempPath();
            //AppDomain.CurrentDomain.SetupInformation.AppDomainInitializer += (string[] arguments) => { Console.WriteLine($"[AppDomainInitializer] Length={arguments.Length}"); };
            //AppDomain.CurrentDomain.ProcessExit += (sender, e) => { Console.WriteLine($"Process exited at {DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt", CultureInfo.InvariantCulture)}"); };

            try
            {
                Logger.Write($"🔔 CurrentDomain.BaseDirectory returns \"{AppDomain.CurrentDomain.BaseDirectory}\"", level: LogLevel.Debug);
                Logger.Write($"🔔 Directory.GetCurrentDirectory returns \"{Directory.GetCurrentDirectory()}\"", level: LogLevel.Debug);
                Assembly[] assemblies = Thread.GetDomain().GetAssemblies();
                foreach (var assem in assemblies)
                {
                    var name = assem.GetName().FullName;
                    var pkt = assem.GetName().GetPublicKeyToken();
                    if (pkt != null && pkt.Length > 0) // ignore null/empty keys
                    {
                        Logger.Write($"🎛️ Assembly: {name}", level: LogLevel.Info);
                    }
                }
                Console.WriteLine();
            }
            catch (Exception) { }
        }

        static bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RelaunchAsAdmin()
        {
            var exeName = Process.GetCurrentProcess().MainModule.FileName;

            var startInfo = new ProcessStartInfo(exeName)
            {
                UseShellExecute = true,
                Verb = "runas"   // This triggers the UAC prompt
            };

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                Console.WriteLine("⚠️ User declined the elevation request.");
            }
        }

        static void ForceSelfExitNow(bool useEnvironment = false)
        {
            if (useEnvironment)
                Environment.Exit(0);
            else
                Process.GetCurrentProcess().Kill();
        }

        static Process GetProcessRunning(string name) => Process.GetProcesses().Where(p => p.ProcessName == name).FirstOrDefault();
        static bool IsProcessRunning(int pid) => Process.GetProcesses().Any(p => p.Id == pid);
        static bool IsProcessRunning(string name) => Process.GetProcesses().Any(p => p.ProcessName == name);
        #endregion
    }

    static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;  // Also SW_NORMAL
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;  // Also SW_MAXIMIZE
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11; // Also SW_MAX
    }
}
