using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LaunchThenClose
{
    /*
        [ADDING]
        ScheduledTaskHelper.CreateLogonTask(
            taskName: "LaunchThenClose AutoStart",
            exePath: @"D:\source\repos\LaunchThenClose\bin\debug\LaunchThenClose.exe",
            arguments: "--logging",
            runAsSystem: true);

        [REMOVING]
        ScheduledTaskHelper.DeleteTask("LaunchThenClose AutoStart");

        [NOTE]
        SchTasks.exe does NOT expose most of the advanced Task Scheduler settings, including:
         - AllowStartOnDemand
         - StartWhenAvailable
         - DisallowStartIfOnBatteries
         - StopIfGoingOnBatteries
         - MultipleInstancesPolicy
         - ExecutionTimeLimit
         - IdleSettings
         - WakeToRun
         - Hidden
         - RunOnlyIfNetworkAvailable
         - Priority

        It can create basic tasks, but not fully configured tasks.

        You could import a fully featured XML task definition using:
         - schtasks /create /tn "LaunchThenClose AutoStart" /xml LaunchThenCloseTask.xml /f

        [EXAMPLE XML]
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Date>2025-01-01T00:00:00</Date>
            <Author>USERNAME</Author>
            <Description>Auto-start elevated console app at logon</Description>
          </RegistrationInfo>
          <Triggers>
            <LogonTrigger>
              <Enabled>true</Enabled>
            </LogonTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author">
              <UserId>S-1-5-18</UserId> <!-- SYSTEM account -->
              <RunLevel>HighestAvailable</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <AllowHardTerminate>true</AllowHardTerminate>
            <StartWhenAvailable>true</StartWhenAvailable>
            <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
            <IdleSettings>
              <StopOnIdleEnd>false</StopOnIdleEnd>
              <RestartOnIdle>false</RestartOnIdle>
            </IdleSettings>
            <AllowStartOnDemand>true</AllowStartOnDemand>
            <Enabled>true</Enabled>
            <Hidden>false</Hidden>
            <RunOnlyIfIdle>false</RunOnlyIfIdle>
            <WakeToRun>false</WakeToRun>
            <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
            <Priority>7</Priority>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>C:\Path\To\YourApp.exe</Command>
              <WorkingDirectory>C:\Path\To</WorkingDirectory>
            </Exec>
          </Actions>
        </Task>
    */
    public static class ScheduledTaskHelper
    {
        /// <summary>
        /// Creates an elevated scheduled task that runs at user logon.
        /// </summary>
        /// <param name="taskName">Name of the scheduled task.</param>
        /// <param name="exePath">Full path to the executable.</param>
        /// <param name="arguments">Optional command-line arguments.</param>
        /// <param name="runAsSystem">If true, runs as SYSTEM. Otherwise runs as current user.</param>
        public static void CreateLogonTask(
            string taskName,
            string exePath,
            string arguments = "",
            bool runAsSystem = true)
        {
            if (string.IsNullOrWhiteSpace(taskName))
                throw new ArgumentException("Task name cannot be empty.", nameof(taskName));

            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path cannot be empty.", nameof(exePath));

            // Build the command line
            var sb = new StringBuilder();
            sb.Append($"/create /tn \"{taskName}\" ");
            sb.Append("/sc onlogon ");
            if (string.IsNullOrEmpty(arguments))
                sb.Append($"/tr \"\\\"{exePath}\\\"");
            else
                sb.Append($"/tr \"\\\"{exePath}\\\" {arguments}\" ");
            sb.Append("/rl highest ");

            if (runAsSystem)
            {
                sb.Append("/ru SYSTEM ");
            }
            else
            {
                sb.Append($"/ru \"{Environment.UserName}\" ");
            }
            /*
             If you need to run the task as a specific admin account:
             C:\>schtasks /create /ru AdminUser /rp AdminPassword ...
            */

            sb.Append("/f");

            RunSchtasks(sb.ToString());
        }

        /// <summary>
        /// Deletes the scheduled task.
        /// </summary>
        /// <param name="taskName">Name of the scheduled task.</param>
        public static void DeleteTask(string taskName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
                throw new ArgumentException("Task name cannot be empty.", nameof(taskName));

            RunSchtasks($"/delete /tn \"{taskName}\" /f");
        }

        /// <summary>
        /// Determines whether a scheduled task with the specified name exists on the local system.<br/>
        /// Exit code 0 (true) => <paramref name="taskName"/> exists<br/>
        /// Exit code 1 (false) => <paramref name="taskName"/> does not exist<br/>
        /// </summary>
        /// <remarks>
        /// This method queries the local Windows Task Scheduler using the schtasks.exe utility.
        /// The check is case-insensitive and applies only to tasks on the local machine. If the task name contains path
        /// separators (\), it is interpreted as a folder path within the Task Scheduler hierarchy.
        /// </remarks>
        /// <param name="taskName">The name of the scheduled task to check for existence. Cannot be null or empty.</param>
        /// <returns>true if a scheduled task with the specified name exists; otherwise, false.</returns>
        public static bool TaskExists(string taskName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/query /tn \"{taskName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            proc.WaitForExit();

            return proc.ExitCode == 0;
        }

        /// <summary>
        /// Runs schtasks.exe with the given arguments.
        /// Throws if exit code is non-zero.
        /// </summary>
        static void RunSchtasks(string arguments, bool logOutput = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            proc.WaitForExit();

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();

            if (logOutput)
            {
                if (!string.IsNullOrWhiteSpace(output))
                    Logger.Write($"SchTasks StandardOutput: {output}", level: LogLevel.Info);
                if (!string.IsNullOrWhiteSpace(error))
                    Logger.Write($"SchTasks StandardError: {error}", level: LogLevel.Info);
            }

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"schtasks.exe failed.\nExitCode: {proc.ExitCode}\nOutput: {output}\nError: {error}");
            }
        }

        /// <summary>
        /// If creating a XML script to run as the current user, gets their SID.<br/>
        /// See notes at top of module for example usage.<br/>
        /// <code>
        ///   schtasks /create /tn "LaunchThenClose AutoStart" /xml LaunchThenCloseTask.xml /f
        /// </code>
        /// </summary>
        /// <returns>Current user's SID</returns>
        public static string GetCurrentUserSid()
        {
            // Can also get the user's SID via command line:
            // C:\>wmic useraccount where name="%USERNAME%" get sid
            try
            {
                return WindowsIdentity.GetCurrent().User.Value;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        #region [XML Section]
        /// <summary>
        /// Creates a scheduled task using an XML template with injected parameters.
        /// </summary>
        public static bool CreateTaskFromXml(
            string appName,
            string exePath,
            string workingDirectory,
            string description,
            string author,
            string userSid = "S-1-5-18"   // default SYSTEM
        )
        {
            /*
            ScheduledTaskHelper.CreateTaskFromXml(
                appName: "AsyncTea",
                exePath: @"C:\Apps\AsyncTea\AsyncTea.exe",
                workingDirectory: @"C:\Apps\AsyncTea",
                description: "Auto-start AsyncTea at logon",
                author: Environment.UserName,
                userSid: "S-1-5-18" // SYSTEM
            );
            */
            if (string.IsNullOrWhiteSpace(appName))
                throw new ArgumentException("App name cannot be empty.", nameof(appName));
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path cannot be empty.", nameof(exePath));
            if (string.IsNullOrWhiteSpace(workingDirectory))
                throw new ArgumentException("Working directory cannot be empty.", nameof(workingDirectory));
            if (string.IsNullOrWhiteSpace(author))
                throw new ArgumentException("Author cannot be empty.", nameof(author));
            if (string.IsNullOrEmpty(userSid))
                userSid = GetCurrentUserSid();

            string xml = BuildXmlTemplate(appName, exePath, workingDirectory, description, author, userSid);

            string xmlFile = $"{appName}.xml";
            System.IO.File.WriteAllText(xmlFile, xml, Encoding.Unicode);

            string taskName = $"{appName} AutoStart";

            return RunXmlSchtasks($"/create /tn \"{taskName}\" /xml \"{xmlFile}\" /f", logOutput: true);
        }

        /// <summary>
        /// Removes the scheduled task created earlier.
        /// </summary>
        public static bool RemoveTask(string appName)
        {
            string taskName = $"{appName} AutoStart";
            return RunXmlSchtasks($"/delete /tn \"{taskName}\" /f", logOutput: true);
        }

        /// <summary>
        /// Builds the XML template with injected parameters.
        /// </summary>
        static string BuildXmlTemplate(
            string appName,
            string exePath,
            string workingDirectory,
            string description,
            string author,
            string userSid
            )
        {
            /*
            <Priority>
            ✔ 0    =>  Idle
            ✔ 1    =>  Below Normal
            ✔ 2    =>  Normal
            ✔ 3    =>  Above Normal
            ✔ 4    =>  High
            ✔ 5    =>  Real‑time
            ✔ 6-10 =>  Reserved / treated as Normal (Scheduler ignores differences)

            <ExecutionTimeLimit>
            ✔ PT0S
                Means no time limit (run indefinitely). This is the most common value for background utilities.
            ✔ PTxxS
                Run for N seconds. Example: PT30S → 30 seconds
            ✔ PTxxM
                Run for N minutes. Example: PT10M → 10 minutes
            ✔ PTxxH
                Run for N hours. Example: PT2H → 2 hours
            ✔ PxxD
                Run for N days. Example: P1D → 1 day
            ✔ Full combinations
                Example: P1DT2H30M10S → 1 day, 2 hours, 30 minutes, 10 seconds

            <RunLevel>
            ✔ LeastPrivilege
                - Runs the task without elevation
                - Equivalent to running normally as the user
                - UAC is not bypassed
                - The task will run with whatever rights the user already has
                This is the default if <RunLevel> is omitted.
            
            ✔ HighestAvailable
                - Runs the task with the highest privileges the account can obtain
                - If the account is an administrator → runs elevated
                - If the account is standard → runs non‑elevated
                - This is the setting you use for auto‑elevated startup apps
            */
            return $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.4"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Date>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}</Date>
    <Author>{author}</Author>
    <Description>{description}</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>{userSid}</UserId>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{exePath}</Command>
      <WorkingDirectory>{workingDirectory}</WorkingDirectory>
    </Exec>
  </Actions>
</Task>";
        }

        /// <summary>
        /// Runs schtasks.exe with the given arguments.
        /// </summary>
        static bool RunXmlSchtasks(string arguments, bool logOutput = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            proc.WaitForExit();

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            
            if (logOutput)
            {
                if (!string.IsNullOrWhiteSpace(output))
                    Logger.Write($"SchTasks StandardOutput: {output}", level: LogLevel.Info);
                if (!string.IsNullOrWhiteSpace(error))
                    Logger.Write($"SchTasks StandardError: {error}", level: LogLevel.Info);
            }

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"schtasks.exe failed.\nExitCode: {proc.ExitCode}\nOutput: {output}\nError: {error}");
            }

            return true;
        }

        #endregion
    }

}
