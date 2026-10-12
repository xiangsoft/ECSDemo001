using System;
using System.Net;
using System.Net.Sockets;

namespace Xiangsoft.Game.Net
{
    /// <summary>
    /// Lockstep relay client: raw-UDP handshake (XRHI/XRHK) to obtain a conv id,
    /// then a reliable KCP conversation. Drive Update() from your main loop.
    /// </summary>
    public sealed class KcpClient : IDisposable
    {
        private static readonly byte[] HelloMagic = { (byte)'X', (byte)'R', (byte)'H', (byte)'I' };
        private static readonly byte[] AckMagic = { (byte)'X', (byte)'R', (byte)'H', (byte)'K' };

        private Socket udp;
        private IPEndPoint serverEp;
        private KcpPeer peer;
        private readonly byte[] recvBuf = new byte[65536];
        private readonly Random rng = new Random();
        private uint nonce;
        private long lastHandshakeSent;

        public bool Connected { get; private set; }

        /// <summary>本连接的会话号（握手分配；0 = 未连接）。</summary>
        public uint Conv => peer?.Conv ?? 0;

        // stage-1 diagnostics
        public int UdpPacketsIn { get; private set; }
        public uint PendingSend => peer?.WaitSendCount ?? 0;
        public int UdpPacketsOut => peer?.UdpPacketsOut ?? 0;
        public string LastSendError { get; private set; } = "";

        /// <summary>A complete wire frame arrived from the server.</summary>
        public event Action<byte[], int> FrameReceived;
        public event Action ConnectedOnce;

        public void Connect(string host, int port)
        {
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Blocking = false;
            serverEp = new IPEndPoint(IPAddress.Parse(host), port);
            SendHandshake();
        }

        private void SendHandshake()
        {
            if (nonce == 0) nonce = (uint)rng.Next(1, int.MaxValue);
            var packet = new byte[8];
            Array.Copy(HelloMagic, 0, packet, 0, 4);
            packet[4] = (byte)(nonce >> 24);
            packet[5] = (byte)(nonce >> 16);
            packet[6] = (byte)(nonce >> 8);
            packet[7] = (byte)nonce;
            try { udp.SendTo(packet, serverEp); } catch (SocketException) { }
            lastHandshakeSent = DateTime.UtcNow.Ticks;
        }

        /// <summary>Poll UDP + pump KCP. Call every frame / tick.</summary>
        public void Update()
        {
            if (udp == null) return;

            if (!Connected && DateTime.UtcNow.Ticks - lastHandshakeSent > TimeSpan.TicksPerSecond)
                SendHandshake();   // handshake retry, 1/s

            try
            {
                while (udp.Available > 0)
                {
                    EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    int n = udp.ReceiveFrom(recvBuf, ref from);
                    if (n <= 0) break;
                    if (!Connected)
                    {
                        TryCompleteHandshake(n);
                        continue;
                    }
                    UdpPacketsIn++;
                    peer?.OnUdpPacket(recvBuf, n);
                }
            }
            catch (SocketException) { }

            if (Connected)
                peer?.Update((uint)Environment.TickCount);
        }

        private void TryCompleteHandshake(int n)
        {
            if (n < 8) return;
            for (int i = 0; i < 4; i++)
                if (recvBuf[i] != AckMagic[i]) return;
            uint ackNonce = ((uint)recvBuf[4] << 24) | ((uint)recvBuf[5] << 16) | ((uint)recvBuf[6] << 8) | recvBuf[7];
            if (ackNonce != nonce) return;
            if (n < 12) return;
            uint conv = ((uint)recvBuf[8] << 24) | ((uint)recvBuf[9] << 16) | ((uint)recvBuf[10] << 8) | recvBuf[11];

            peer = new KcpPeer(conv, (buffer, length) =>
            {
                try { udp.SendTo(buffer, length, SocketFlags.None, serverEp); }
                catch (SocketException ex) { LastSendError = ex.SocketErrorCode + ": " + ex.Message; }
                catch (Exception ex) { LastSendError = ex.GetType().Name + ": " + ex.Message; }
            });
            peer.FrameReceived += (data, len) => FrameReceived?.Invoke(data, len);
            Connected = true;
            ConnectedOnce?.Invoke();
        }

        public void Send(MsgId id, object message)
        {
            if (!Connected || peer == null) return;
            var frame = Wire.Encode(id, message);
            peer.SendRaw(frame, frame.Length);
        }

        public void Dispose()
        {
            udp?.Close();
            udp = null;
        }
    }
}
