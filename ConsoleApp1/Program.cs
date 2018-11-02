using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = LogManager.GetLogger(nameof(ConsoleApp1));

            var idx = 0;
            while (idx < 10)
            {
                Console.WriteLine("{0:D6}: {1}", idx, DateTime.Now.ToString());
                logger.InfoFormat("{0:D6}: {1}", idx, DateTime.Now.ToString());
                Thread.Sleep(1000);

                idx++;
            }

            logger = null;
            GC.Collect();
            Thread.Sleep(1000 * 10240);
        }
    }
}
