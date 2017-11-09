using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Targets;
using NLog.Config;

namespace TPzip.Core
{
    static class Logging
    {
        private static Logger logger;

        public static Logger Logger
        {
            get {
                if (logger == null)
                {
                    logger = buildLogger();
                }
                return logger;
            }
            private set { logger = value; }
        }

        private static Logger buildLogger()
        {
            LoggingConfiguration config = new LoggingConfiguration();
            string layout = @"${date:format=dd/MM/yy HH\:mm\:ss} ${logger} ${message}";

#if DEBUGCONSOLE
            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            
            consoleTarget.Layout = layout;
            
           LoggingRule rule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule);
#elif DEBUG
            FileTarget fileTarget = new FileTarget();
            fileTarget.FileName = "${basedir}/log.txt";
            fileTarget.Layout = layout;

            LoggingRule rule = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule);
#else
            //!\ ATTENTION : il faut être enregistré pour logger dans le journal d'évennement
            EventLogTarget eventLogTarget = new EventLogTarget();
            eventLogTarget.Source = "TPzip";
            eventLogTarget.Log = "Application";
            eventLogTarget.MachineName = ".";
            eventLogTarget.Layout = layout;

            LoggingRule rule = new LoggingRule("*", LogLevel.Trace, eventLogTarget);
            config.LoggingRules.Add(rule);
#endif

            LogManager.Configuration = config;
            
            return LogManager.GetLogger("TPzip");
        }

    }
}
