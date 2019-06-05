using System;
using System.IO;
using System.Timers;

namespace IOTLink.Helpers
{
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
            DEBUG,
            TRACE,
            HELP_ME
        }

        public static LoggerHelper GetInstance()
        {
            if (_instance == null)
                _instance = new LoggerHelper();

            return _instance;
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

                string filename = string.Format("ServiceLog_{0}.log", DateTime.Now.Date.ToShortDateString().Replace('/', '_'));
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

        private void WriteLog(LogLevel logLevel, Type origin, string message, params object[] args)
        {
            if (origin == null || string.IsNullOrWhiteSpace(message))
                return;

            string messageTag = origin.Name;
            string formatedMessage;
            if (args == null || args.Length == 0)
                formatedMessage = message;
            else
                formatedMessage = string.Format(message, args);

            string finalMessage = string.Format("[{0}][{1}][{2}][{3}]: {4}", WindowsHelper.GetFullMachineName(), DateTime.Now, logLevel.ToString(), messageTag, formatedMessage);

            if (_lastMessage != null && _lastMessage.DayOfYear != DateTime.Now.DayOfYear)
            {
                CloseLogFile();
                OpenLogFile();
            }

            WriteFile(finalMessage);
        }

        public static void Critical(Type origin, string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.CRITICAL, origin, message, args);
        }

        public static void Error(Type origin, string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.ERROR, origin, message, args);
        }

        public static void Warn(Type origin, string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.WARNING, origin, message, args);
        }

        public static void Info(Type origin, string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.INFO, origin, message, args);
        }

        public static void Debug(Type origin, string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.DEBUG, origin, message, args);
        }

        public static void Trace(Type origin, string message, params object[] args)
        {
            GetInstance().WriteLog(LogLevel.TRACE, origin, message, args);
        }

        internal static void EmptyLine()
        {
            GetInstance().WriteFile();
        }
    }
}
