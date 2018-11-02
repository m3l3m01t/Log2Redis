using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;
using System.IO;

namespace Log2Redis
{
    /// <summary>
    /// Displays a MessageBox for all log messages.
    /// </summary>
    public class RedisPubAppender : AppenderSkeleton
    {
        private RedisLogger _logger;

        public int Port { get; set; } = 6379;

        public string Host { get; set; }

        public string Topic { get; set; }

        /// <summary>
        /// Writes the logging event to redis
        /// </summary>
        protected override void Append(LoggingEvent evt)
        {
            if (_logger != null)
            {
                var writer = new StringWriter();
                Layout.Format(writer, evt);

                var message = writer.ToString();

                new PatternLayout(Topic).Format(writer, evt);

                _logger.Log(writer.ToString(), message);
            }
        }

        protected override bool PreAppendCheck()
        {
            if (_logger == null)
            {
                if (Host == null || Topic == null)
                    return false;

                _logger = RedisLogger.Get(Host, Port, Topic, true);
            }
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
