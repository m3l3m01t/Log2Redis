using Microsoft.VisualStudio.TestTools.UnitTesting;
using Log4Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using log4net;
using log4net.Layout;
using log4net.Config;
using System.Threading;

namespace Log4Redis.Tests
{
    [TestClass()]
    public class RedisLoggerTests
    {
        private ILog _logger;

        [AssemblyInitialize]
        public static void Initialize(TestContext tc)
        {
            var appender = new RedisPubAppender() {Topic="KGD",
                Host ="csj-oq-kgd01.sdcorp.global.sandisk.com"};
            appender.Layout = new PatternLayout("%d %-5level %logger - %m%n"); // set pattern
            BasicConfigurator.Configure(appender);
        }

        [TestInitialize()]
        public void Setup()
        {
            _logger = LogManager.GetLogger(nameof(RedisLoggerTests)); // obtain logger
            _logger.Info("Hello World");
        }

        [TestMethod()]
        public void RedisLoggerTest()
        {
            for(var i = 0; i < 1000; i++)
            {
                _logger.DebugFormat("Hello world {0}", i);
                _logger.InfoFormat("Hello world {0}", i);
                Thread.Sleep(1000 * 1);
            }
        }
    }
}