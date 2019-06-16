using IOTLinkAPI.Configs;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;

namespace IOTLinkAPI.Helpers
{
#pragma warning disable 1591
    public class LoggerHelper
    {
        private static LoggerHelper _instance;
        private StreamWriter _logWriter;
        private Timer _flushTimer;
        private DateTime _lastMessage;

        public enum LogLevel
        {
            DISABLED,
            CRITICAL,
            ERROR,
            WARNING,
            INFO,
            VERBOSE,
            DEBUG,
            DEBUG_LOOP,
            TRACE,
            TRACE_LOOP,
            DATA_DUMP
        }

        private LoggerHelper()
        {
            OpenLogFile();

            _flushTimer = new Timer();
            _flushTimer.Interval = 1000;
            _flushTimer.Elapsed += OnFlushInterval;
        }

        ~LoggerHelper()
        {
            CloseLogFile();
        }

        public void Flush()
        {
            try
            {
                if (_logWriter != null)
                    _logWriter.Flush();
            }
            catch (Exception)
            {
                //TODO: Cry
            }
        }

        private void WriteFile(string message = null)
        {
            try
            {
                if (_logWriter != null)
                {
                    _logWriter.WriteLine(message);
                    _flushTimer.Stop();
                    _flushTimer.Start();
                    _lastMessage = DateTime.Now;
                }
            }
            catch (Exception)
            {
                //TODO: Cry again
            }
        }

        private void OpenLogFile()
        {
            try
            {
                string logsPath = PathHelper.LogsPath();
                if (!Directory.Exists(logsPath))
                    Directory.CreateDirectory(logsPath);

                string prefix = Environment.UserInteractive ? "ConsoleLog" : "ServiceLog";
                string date = DateTime.Now.ToString("yyyy_MM_dd");
                string filename = string.Format("{0}_{1}.log", prefix, date);
                string path = Path.Combine(logsPath, filename);

                if (!File.Exists(path))
                    _logWriter = File.CreateText(path);
                else
                    _logWriter = File.AppendText(path);
            }
            catch (Exception)
            {
                // Cry
            }
        }

        private void CloseLogFile()
        {
            try
            {
                if (_logWriter != null)
                {
                    _flushTimer.Stop();
                    _logWriter.Flush();
                    _logWriter.Close();
                    _logWriter = null;
                }
            }
            catch (Exception)
            {
                //TODO: Cry
            }
        }

        private void OnFlushInterval(object sender, ElapsedEventArgs e)
        {
            Flush();
        }

        private void WriteLog(LogLevel logLevel, string messageTag, string message, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(messageTag) || string.IsNullOrWhiteSpace(message))
                return;

            ApplicationConfig config = ConfigHelper.GetEngineConfig();
            if (config == null || config.Logging == null || !config.Logging.Enabled || config.Logging.Level < logLevel)
                return;

            string formatedMessage;
            if (args == null || args.Length == 0)
                formatedMessage = message;
            else
                formatedMessage = string.Format(message, args);

            string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
            string machineName = PlatformHelper.GetFullMachineName();
            string finalMessage = string.Format("[{0}][{1}][{2}][{3}]: {4}", machineName, datetime, logLevel.ToString(), messageTag, formatedMessage);

            if (_lastMessage != null && _lastMessage.DayOfYear != DateTime.Now.DayOfYear)
            {
                CloseLogFile();
                OpenLogFile();
            }

            WriteFile(finalMessage);
        }

        public static void Critical(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.CRITICAL, GetCallerInformation(), message, args);
        }

        public static void Error(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.ERROR, GetCallerInformation(), message, args);
        }

        public static void Warn(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.WARNING, GetCallerInformation(), message, args);
        }

        public static void Info(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.INFO, GetCallerInformation(), message, args);
        }

        public static void Verbose(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.VERBOSE, GetCallerInformation(), message, args);
        }

        public static void Debug(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.DEBUG, GetCallerInformation(), message, args);
        }

        public static void Trace(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.TRACE, GetCallerInformation(), message, args);
        }

        public static void TraceLoop(string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.TRACE_LOOP, GetCallerInformation(), message, args);
        }

        public static void EmptyLine()
        {
            GetInstance().WriteFile();
        }

        private static string GetCallerInformation()
        {
            try
            {
                string fullName;
                Type declaringType;
                int skipFrames = 2;
                do
                {
                    StackFrame stackFrame = new StackFrame(skipFrames, false);
                    MethodBase method = stackFrame?.GetMethod();
                    declaringType = method?.DeclaringType;
                    if (declaringType == null)
                    {
                        return method?.Name;
                    }
                    skipFrames++;
                    fullName = declaringType?.FullName;
                }
                while (declaringType != null && declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

                return Regex.Replace(fullName, "(.*)\\+([A-Za-z0-9_<>]+)(.*)", "$1$3");
            }
            catch (Exception)
            {
                return "GetCallerInformationError";
            }
        }

        public static LoggerHelper GetInstance()
        {
            if (_instance == null)
                _instance = new LoggerHelper();

            return _instance;
        }
    }
}
