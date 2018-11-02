using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;
using ServiceStack.Redis;
using System;
using System.Diagnostics;
using System.IO;

namespace Log4Redis
{
    /// <summary>
    /// Displays a MessageBox for all log messages.
    /// </summary>
    public class RedisPubAppender : AppenderSkeleton
    {
        public int Port { get; set; } = 6379;

        public string Host { get; set; }

        public string Topic { get; set; }

        /// <summary>
        /// Writes the logging event to redis
        /// </summary>
        protected override void Append(LoggingEvent evt)
        {
            var logger = RedisLogger.Get(Host, Port, Topic, true);
            if (logger != null)
            {
                var writer = new StringWriter();
                Layout.Format(writer, evt);

                logger.Log($"{Topic}/{evt.LoggerName}/{evt.Level}", writer.ToString());
            }
        }

        protected override bool PreAppendCheck()
        {
            if (Host == null || Topic == null)
                return false;

            return base.PreAppendCheck();
        }

        protected override void OnClose()
        {
            base.OnClose();

            var logger = RedisLogger.Get(Host, Port, Topic);
            if (logger != null)
            {
                logger.Dispose();
            }
        }

        /// <summary>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </summary>
        protected override bool RequiresLayout => true;
    }
}
