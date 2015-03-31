using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace ModelMessageInterface
{
    public static class MmiEmbeddedModelServer
    {
        public class MmiRunnerInfo
        {
            public string Library;

            public string ConfigFilePath;

            public uint Port;

            public Process Process;
            
            public string LogFilePath { get; set; }
        }

        internal static uint currentPort = 6000;

        private static readonly List<MmiRunnerInfo> runners = new List<MmiRunnerInfo>();

        private static readonly List<MmiModelClient> clients = new List<MmiModelClient>();

        public const string MmiRunnerPath = @"C:\Anaconda\Scripts\mmi-runner.exe";

        public static IEnumerable<MmiModelClient> StartedModels
        {
            get { return clients; }
        }

        public static MmiModelClient StartModel(string library, string configFile)
        {
            // start runner
            var info = new MmiRunnerInfo
            {
                Port = currentPort++,
                ConfigFilePath = configFile,
                Library = library
            };

            var arguments = string.Format("{0} {1} --pause --disable-logger --port-req {2}", info.Library, info.ConfigFilePath, info.Port);

            info.Process = new Process
            {
                StartInfo = new ProcessStartInfo(MmiRunnerPath, arguments)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(configFile),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            info.LogFilePath = "mmi-runner-" + Path.GetFileName(configFile) + "-" + info.Port + ".log";
            
            if (File.Exists(info.LogFilePath))
            {
                File.Delete(info.LogFilePath);
            }

            info.Process.OutputDataReceived += (sender, args) => WriteOutput(info, args.Data);
            info.Process.ErrorDataReceived += (sender, args) => WriteOutput(info, args.Data);
            info.Process.Exited += ProcessOnExited;

            info.Process.Start();

            info.Process.BeginOutputReadLine();
            info.Process.BeginErrorReadLine();

            runners.Add(info);

            if (info.Process.HasExited)
            {
                throw new Exception("Can't start MMI runner");
            }

            // connect client
            var client = new MmiModelClient("tcp://localhost:" + info.Port);
            client.Connect();
            clients.Add(client);

            return client;
        }

        private static void WriteOutput(MmiRunnerInfo info, string str)
        {
            lock (info)
            {
                File.AppendAllText(info.LogFilePath, str + "\n");
            }
        }

        private static void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            // TODO: cleanup
        }

        public static void StopModel(MmiModelClient model)
        {
            var i = clients.IndexOf(model);

            if (i != -1)
            {
                var process = runners[i].Process;
                var id = process.Id;

                if (!process.HasExited)
                {
                    KillProcessAndChildren(id);
                }
            }
        }

        public static void StopAll()
        {
            while (runners.Count > 0)
            {
                if (!runners[0].Process.HasExited)
                {
                    KillProcessAndChildren(runners[0].Process.Id);
                }
                runners.RemoveAt(0);
            }
        }

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}