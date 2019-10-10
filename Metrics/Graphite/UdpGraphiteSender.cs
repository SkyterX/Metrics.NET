using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Metrics.Utils;

namespace Metrics.Graphite
{
    public sealed class UdpGraphiteSender : GraphiteSender
    {
        public UdpGraphiteSender(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        protected override void SendData(string data)
        {
            try
            {
                if (client == null)
                {
                    client = InitClient(host, port);
                }

                var bytes = Encoding.UTF8.GetBytes(data);
                client.Send(bytes, bytes.Length);
            }
            catch (Exception x)
            {
                using (client)
                {
                }
                client = null;
                MetricsErrorHandler.Handle(x, $"Error sending UDP data to graphite endpoint {host}:{port}");
            }
        }

        public override void Flush()
        {
        }

        private static UdpClient InitClient(string host, int port)
        {
            var endpoint = new IPEndPoint(HostResolver.Resolve(host), port);
            var client = new UdpClient();
            client.Connect(endpoint);
            return client;
        }

        protected override void Dispose(bool disposing)
        {
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
        private UdpClient client;
    }
}