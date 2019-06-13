using IOTLinkAgent.Agent;
using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IOTLinkAgent
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            LoggerHelper.Debug("Agent Initialized");

            // Service Run
            if (!Environment.UserInteractive)
            {
                LoggerHelper.Error("Agent has been initialized in a non-interactive environment. Finishing.");
                return -1;
            }

            Task myTask = new Task(() =>
            {
                // Parse commands
                Dictionary<string, List<string>> commands = ParseCommandLine(args);

                // Init
                MainAgent.GetInstance().Init(commands);
            });

            myTask.Start();

            new ManualResetEvent(false).WaitOne();
            return 0;
        }

        private static Dictionary<string, List<string>> ParseCommandLine(string[] args)
        {
            Dictionary<string, List<string>> commands = new Dictionary<string, List<string>>();
            if (args == null || args.Length == 0)
                return commands;

            // Run through all arguments to find runnable commands
            Queue<string> argsQueue = new Queue<string>(args);
            while (argsQueue.Count > 0)
            {
                // Ignore all command line arguments until we find an argument
                // which starts with '--' characters
                while (argsQueue.Count > 0 && !argsQueue.Peek().StartsWith("--"))
                    argsQueue.Dequeue();

                string command = argsQueue.Dequeue().ToLowerInvariant().Remove(0, 2);

                // Parse command line to get all commands arguments
                List<string> commandArgs = new List<string>();
                while (argsQueue.Count > 0 && !argsQueue.Peek().StartsWith("--"))
                    commandArgs.Add(argsQueue.Dequeue());

                commands.Add(command, commandArgs);
            }

            return commands;
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggerHelper.Critical("Critical Unhandled Exception: " + e.ExceptionObject.ToString());
            LoggerHelper.GetInstance().Flush();
        }
    }
}
