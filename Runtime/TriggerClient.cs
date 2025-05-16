using System;
using System.Net.Sockets;
using System.Text;

namespace Trgen
{
    public class TrgenClient
    {
        private readonly string ip;
        private readonly int port;
        private readonly int timeout;
        public TrgenImplementation Implementation { get; private set; }

        public TrgenClient(string ip = "192.168.123.1", int port = 4242, int timeout = 2000)
        {
            this.ip = ip;
            this.port = port;
            this.timeout = timeout;
        }

        public void Connect()
        {
            int packed = RequestImplementation();
            this.Implementation = new TrgenImplementation(packed);
        }

        public int RequestImplementation()
        {
            var ack = SendPacket(0x04); // CMD_REQ_IMPL
            if (!ack.StartsWith("ACK4.")) throw new Exception("ACK4 expected");
            return int.Parse(ack.Substring(5));
        }

        public string SendPacket(int command, uint[] payload = null)
        {
            byte[] header = BitConverter.GetBytes(command);
            byte[] payloadBytes = payload != null ? BuildPayload(payload) : Array.Empty<byte>();

            byte[] raw = new byte[header.Length + payloadBytes.Length];
            Buffer.BlockCopy(header, 0, raw, 0, header.Length);
            if (payloadBytes.Length > 0)
                Buffer.BlockCopy(payloadBytes, 0, raw, header.Length, payloadBytes.Length);

            uint crc = Crc32.Compute(raw);
            byte[] crcBytes = BitConverter.GetBytes(crc);

            byte[] packet = new byte[raw.Length + crcBytes.Length];
            Buffer.BlockCopy(raw, 0, packet, 0, raw.Length);
            Buffer.BlockCopy(crcBytes, 0, packet, raw.Length, 4);

            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(ip, port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(timeout))
                    throw new TimeoutException("Timeout connecting to TRGEN");

                NetworkStream stream = client.GetStream();
                stream.Write(packet, 0, packet.Length);

                byte[] buffer = new byte[64];
                int read = stream.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, read);
            }
        }

        private byte[] BuildPayload(uint[] words)
        {
            byte[] payload = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                byte[] word = BitConverter.GetBytes(words[i]);
                Buffer.BlockCopy(word, 0, payload, i * 4, 4);
            }
            return payload;
        }
    }
}