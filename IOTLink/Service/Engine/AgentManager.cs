using IOTLink.Service.WSServer;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace IOTLink.Service.Engine
{
    public class AgentManager
    {
        private static AgentManager _instance;

        public static AgentManager GetInstance()
        {
            if (_instance == null)
                _instance = new AgentManager();

            return _instance;
        }

        private AgentManager()
        {

        }

        public List<AgentInfo> GetAgents()
        {
            string wmiQuery = string.Format("SELECT SessionId, ProcessID, CommandLine FROM Win32_Process WHERE Name='{0}.exe'", PathHelper.APP_AGENT_NAME);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();
            List<AgentInfo> agents = new List<AgentInfo>();

            foreach (ManagementObject processInfo in objCollection)
            {
                int sessionId = Convert.ToInt32(processInfo.Properties["SessionId"].Value);
                int processId = Convert.ToInt32(processInfo.Properties["ProcessID"].Value);
                string commandLine = (string)processInfo.Properties["CommandLine"].Value;

                agents.Add(new AgentInfo
                {
                    SessionId = sessionId,
                    ProcessId = processId,
                    CommandLine = commandLine,
                    Username = PlatformHelper.GetUsername(sessionId)
                });
            }

            return agents;
        }

        public void StartAgent(int sessionId, string username = null)
        {
            AgentManager agentManager = AgentManager.GetInstance();
            List<AgentInfo> agents = agentManager.GetAgents();
            foreach (AgentInfo agentInfo in agents)
            {
                if (agentInfo.SessionId == sessionId && agentInfo.CommandLine.Contains("--agent"))
                {
                    LoggerHelper.Trace("StartAgent - Agent instance is already running for this user. Skipping.");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(username))
                username = PlatformHelper.GetUsername(sessionId);

            RunInfo runInfo = new RunInfo
            {
                Application = Path.Combine(PathHelper.BaseAppPath(), PathHelper.APP_AGENT_NAME + ".exe"),
                CommandLine = "--agent " + WebSocketServerManager.WEBSOCKET_URI,
                WorkingDir = PathHelper.BaseAppPath(),
                Username = username,
                Visible = false,
                Fallback = false
            };

            PlatformHelper.Run(runInfo);
        }

        public void StopAgents()
        {
            AgentManager agentManager = AgentManager.GetInstance();
            List<AgentInfo> agents = agentManager.GetAgents();
            foreach (AgentInfo agentInfo in agents)
            {
                try
                {
                    Process process = Process.GetProcessById(agentInfo.ProcessId);
                    if (process != null)
                        process.Kill();
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("StopAgents - Failed to Kill PID {0}: {1}", agentInfo.ProcessId, ex.ToString());
                }
            }
        }
    }
}
