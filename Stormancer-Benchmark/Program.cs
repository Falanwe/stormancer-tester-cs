using Stormancer;
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Plugins;
using System.Reactive.Linq;
using System.IO;

namespace Stormancer_Benchmark
{
    class Program
    {
        private const string ACCOUNTID = "997bc6ac-9021-2ad6-139b-da63edee8c58";
        private const string APPLICATIONNAME = "tester";
        private const string SCENENAME = "main";

        private static int _maxPendingRpcPings = 1;

        static void Main(string[] args)
        {
            MainImpl().Wait();
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private async static Task MainImpl()
        {
            var config = ClientConfiguration.ForAccount(ACCOUNTID, APPLICATIONNAME);

            var client = new Client(config);

            var scene = await client.GetPublicScene(SCENENAME, "");

            var pendingRpcPing = 0;

            await scene.Connect();

            //wait for the sync clock to synchronize.
            await Task.Delay(TimeSpan.FromSeconds(10));

            var timer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            var measures = new List<Measure>();

            using (timer.Subscribe(async _ =>
            {
                if (pendingRpcPing < _maxPendingRpcPings)
                {
                    _maxPendingRpcPings++;

                    var measure = new Measure();

                    measure.RequestTime = client.Clock;
                    measure.ServerReceptionTime = await scene.RpcTask<object, long>("rpcping", null);
                    measure.ReceptionTime = client.Clock;

                    measures.Add(measure);
                }
            }))
            {
                await Task.Delay(60000);
            }


            //write the measures into a CSV
            using (var writer = File.CreateText("output.csv"))
            {
                foreach (var measure in measures)
                {
                    writer.WriteLine($"{measure.RequestTime}, {measure.ServerReceptionTime}, {measure.ReceptionTime}");
                }                
            }
        }
    }

    class Measure
    {
        public long RequestTime { get; set; }
        public long ServerReceptionTime { get; set; }
        public long ReceptionTime { get; set; }
    }
}
