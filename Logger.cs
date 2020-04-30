using System;
using log4net;

namespace MigrateDocuments
{
    public static class Logger
    {
        private readonly static ILog _logger;

        static Logger()
        {
            log4net.Config.XmlConfigurator.Configure();
            _logger = LogManager.GetLogger(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
        }

        public static void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public static void LogDebug(Exception ex, string message = null)
        {
            _logger.Debug(message ?? ex.Message, ex);
        }

        public static void LogWarning(string message)
        {
            _logger.Warn(message);
        }
        public static void LogWarning(Exception ex, string message)
        {
            _logger.Warn(message ?? ex.Message, ex);
        }

        public static void LogError(Exception ex, string message = null)
        {
            _logger.Error(message ?? ex.Message, ex);
        }

        public static void LogError(string message)
        {
            _logger.Error(message);
        }
    }
}
