using log4net.Core;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Log4Redis
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

        private RedisClient _client;
        private Subject<LogData> _subject = new Subject<LogData>();

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public RedisLogger(string host, int port)
        {
            _client = new RedisClient(host, port);
            _client.Init();
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

                    _loggers[key] = logger;

                    logger.Start();
                }
            }

            return logger;
        }

        private void Start()
        {
            _subject.AsObservable()
                .Buffer(TimeSpan.FromSeconds(2), 20).Where(l => l.Any())
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(lst =>
                {
                    try
                    {
                        foreach (var data in lst)
                        {
                            if (!_tokenSource.IsCancellationRequested)
                                _client.PublishMessage(data.Topic, data.Message); 
                            else
                            {
                                _client.Dispose();
                            }
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tokenSource.Cancel();
                    Thread.Sleep(1000);
                    _client.Dispose();
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