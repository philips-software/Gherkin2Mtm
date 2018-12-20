using System;
using System.Globalization;
using log4net;

namespace Gherkin2MtmApi.Helpers
{
    public class Logger
    {
        private static bool IsConfigured { get; set; }
        private ILog Log { get; }

        private Logger(string name)
            : this()
        {
            Log = LogManager.GetLogger(name);
        }

        private Logger(Type type)
            : this()
        {
            Log = LogManager.GetLogger(type);
        }

        private Logger()
        {
            if (IsConfigured) return;

            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("log4net.config"));
            IsConfigured = true;
        }

        public static Logger GetLogger(string name)
        {
            return new Logger(name);
        }

        public static Logger GetLogger(Type type)
        {
            return new Logger(type);
        }

        public void Debug(string message)
        {
            Log.Debug(message);
        }

        public void Info(string message)
        {
            Log.Info(message);
        }

        public void Info(string text, params object[] interpolations)
        {
            Log.Info(string.Format(CultureInfo.InvariantCulture, text, interpolations));
        }

        public void Warning(string message)
        {
            Log.Warn(message);
        }

        public void Error(string message)
        {
            Log.Error(message);
        }

        public void Error(string text, params object[] interpolations)
        {
            Log.Error(string.Format(CultureInfo.InvariantCulture, text, interpolations));
        }

        public void Fatal(string message)
        {
            Log.Fatal(message);
        }
    }
}
