using log4net.Core;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Log2Redis
{
    public class RedisLogger:IDisposable
    {
        class LogData
        {
            public LogData(string topic, string message)
            {
                Topic = topic;
                Message = message;
            }

            public string Topic { get; set; }
            public string Message { get; set; }
        }

        private static Dictionary<string, RedisLogger> _loggers = new Dictionary<string, RedisLogger>();

        private ConnectionMultiplexer _client = null;

        private Subject<LogData> _subject = new Subject<LogData>();

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private RedisLogger(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public bool Init()
        {
            try
            {
                if (_client == null)
                    _client = ConnectionMultiplexer.Connect($"{Host}:{Port}");
            } catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static RedisLogger Get(string host, int port, string topic, bool create = false)
        {
            RedisLogger logger;

            var key = $"{host}_{port}_{topic}";
            lock(_loggers)
            {
                if (!_loggers.TryGetValue(key, out logger) && create)
                {
                    logger = new RedisLogger(host, port);
                    logger.Init();
                    _loggers[key] = logger;

                    logger.Start();
                }
            }

            return logger;
        }

        private void Start()
        {
            _subject.AsObservable()
                .Buffer(TimeSpan.FromMilliseconds(1000), 10).Where(l => l.Any())
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(async lst =>
                {
                    if (_client == null)
                        return;

                    try
                    {
                        var db = _client?.GetDatabase();
                        foreach (var data in lst)
                        {
                            if (_tokenSource.IsCancellationRequested)
                                break;
                            await db?.PublishAsync(data.Topic, data.Message, CommandFlags.FireAndForget); 
                        }
                    } catch (Exception)
                    {
                    }
                }, _tokenSource.Token);
        }

        public void Log(string topic, string message)
        {
            _subject.OnNext(new LogData(topic, message));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public string Host { get; }
        public int Port { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tokenSource.Cancel();
                    Thread.Sleep(1000);
                    _client?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RedisLogger() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}