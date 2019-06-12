using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Windows;
using IOTLinkService.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace IOTLink
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            // Service Run
            if (!Environment.UserInteractive)
            {
                ServiceBase service = new IOTLinkService();
                ServiceBase.Run(service);
                return 0;
            }

            if (args.Length == 0)
            {
                WindowsAPI.ShowMessage("IOT Link", "Missing command-line parameters.");
                return 1;
            }

            // Get Command Instances
            Dictionary<string, ICommand> commands = GetCommands();

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

                // Run command if available
                if (commands.ContainsKey(command))
                {
                    int result = commands[command].ExecuteCommand(commandArgs.ToArray());
                    if (result != 0)
                        return result;
                }
            }

            return 0;
        }

        private static Dictionary<string, ICommand> GetCommands()
        {
            var commands = new Dictionary<string, ICommand>();

            var interfaceType = typeof(ICommand);
            var interfaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract && !p.IsInterface);

            foreach (Type type in interfaces)
            {
                ICommand command = (ICommand)Activator.CreateInstance(type);
                string key = command.GetCommandLine().ToLowerInvariant();
                commands.Add(key, command);
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