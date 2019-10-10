using System;
using System.Net;
using System.Net.Sockets;

using Metrics.Utils;

namespace Metrics.Graphite
{
    public sealed class PickleGraphiteSender : GraphiteSender
    {
        public PickleGraphiteSender(string host, int port, int batchSize = DefaultPickleJarSize)
        {
            this.host = host;
            this.port = port;
            pickleJarSize = batchSize;
        }

        public const int DefaultPickleJarSize = 100;

        public override void Send(string name, string value, string timestamp)
        {
            jar.Append(name, value, timestamp);

            if (jar.Size >= pickleJarSize)
            {
                WriteCurrentJar();
                jar = new PickleJar();
            }
        }

        private void WriteCurrentJar()
        {
            try
            {
                if (client == null)
                {
                    client = InitClient(host, port);
                }

                jar.WritePickleData(client.GetStream());
            }
            catch (Exception x)
            {
                using (client)
                {
                }
                client = null;
                MetricsErrorHandler.Handle(x, $"Error sending Pickled data to graphite endpoint {host}:{port}");
            }
        }

        protected override void SendData(string data)
        {
        }

        public override void Flush()
        {
            try
            {
                WriteCurrentJar();
                client?.GetStream().Flush();
            }
            catch (Exception x)
            {
                using (client)
                {
                }
                client = null;
                MetricsErrorHandler.Handle(x, $"Error sending Pickled data to graphite endpoint {host}:{port}");
            }
        }

        private static TcpClient InitClient(string host, int port)
        {
            var endpoint = new IPEndPoint(HostResolver.Resolve(host), port);
            var client = new TcpClient();
            client.Connect(endpoint);
            return client;
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
            using (client)
            {
                try
                {
                    client.Close();
                }
                catch
                {
                }
            }
            client = null;
            base.Dispose(disposing);
        }

        private readonly string host;
        private readonly int port;
        private readonly int pickleJarSize;

        private TcpClient client;
        private PickleJar jar = new PickleJar();
    }
}