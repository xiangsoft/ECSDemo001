using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Xiangsoft.Game.Net
{
    /// <summary>
    /// Lockstep relay server: one UDP socket for everyone. Raw-UDP handshakes hand out
    /// conv ids, each conversation is a KcpPeer. Drive Update() from your loop.
    /// </summary>
    public sealed class KcpServer : IDisposable
    {
        private static readonly byte[] HelloMagic = { (byte)'X', (byte)'R', (byte)'H', (byte)'I' };
        private static readonly byte[] AckMagic = { (byte)'X', (byte)'R', (byte)'H', (byte)'K' };

        private Socket udp;
        private readonly byte[] recvBuf = new byte[65536];
        private readonly Dictionary<IPEndPoint, KcpPeer> peers = new Dictionary<IPEndPoint, KcpPeer>();
        private uint nextConv = 1;

        public int PeerCount => peers.Count;

        // stage-1 diagnostics
        public int UdpIn { get; private set; }
        public int RoutedToPeer { get; private set; }

        /// <summary>A frame arrived from a peer.</summary>
        public event Action<KcpPeer, MsgId, byte[]> FrameReceived;
        public event Action<KcpPeer> PeerConnected;
        public event Action<KcpPeer> PeerDisconnected;

        public void Start(int port)
        {
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Blocking = false;
            udp.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        /// <summary>Poll UDP, answer handshakes, pump all peers. Call in your loop.</summary>
        public void Update()
        {
            if (udp == null) return;

            try
            {
                while (udp.Available > 0)
                {
                    EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    int n = udp.ReceiveFrom(recvBuf, ref from);
                    if (n <= 0) break;
                    var remote = (IPEndPoint)from;
                    UdpIn++;

                    if (IsHandshake(n))
                    {
                        AnswerHandshake(remote, n);
                        continue;
                    }

                    if (peers.TryGetValue(remote, out var peer))
                    {
                        RoutedToPeer++;
                        peer.OnUdpPacket(recvBuf, n);
                    }
                }
            }
            catch (SocketException) { }

            uint now = (uint)Environment.TickCount;
            foreach (var peer in peers.Values)
                peer.Update(now);
        }

        private bool IsHandshake(int n)
        {
            if (n < 8) return false;
            for (int i = 0; i < 4; i++)
                if (recvBuf[i] != HelloMagic[i]) return false;
            return true;
        }

        private void AnswerHandshake(IPEndPoint remote, int n)
        {
            uint nonce = ((uint)recvBuf[4] << 24) | ((uint)recvBuf[5] << 16) | ((uint)recvBuf[6] << 8) | recvBuf[7];

            if (!peers.TryGetValue(remote, out var peer))
            {
                uint conv = nextConv++;
                peer = new KcpPeer(conv, (buffer, length) =>
                {
                    try { udp.SendTo(buffer, length, SocketFlags.None, remote); } catch (SocketException) { }
                });
                peer.FrameReceived += (data, len) =>
                {
                    if (Wire.TryDecode(data, len, out var id, out var payload))
                        FrameReceived?.Invoke(peer, id, payload);
                };
                peers[remote] = peer;
                PeerConnected?.Invoke(peer);
            }

            // always re-ack (idempotent — covers lost acks)
            var ack = new byte[12];
            Array.Copy(AckMagic, 0, ack, 0, 4);
            ack[4] = (byte)(nonce >> 24);
            ack[5] = (byte)(nonce >> 16);
            ack[6] = (byte)(nonce >> 8);
            ack[7] = (byte)nonce;
            uint convId = peer.Conv;
            ack[8] = (byte)(convId >> 24);
            ack[9] = (byte)(convId >> 16);
            ack[10] = (byte)(convId >> 8);
            ack[11] = (byte)convId;
            try { udp.SendTo(ack, remote); } catch (SocketException) { }
        }

        public void SendTo(KcpPeer peer, MsgId id, object message)
        {
            var frame = Wire.Encode(id, message);
            peer.SendRaw(frame, frame.Length);
        }

        public void Broadcast(MsgId id, object message)
        {
            var frame = Wire.Encode(id, message);
            foreach (var peer in peers.Values)
                peer.SendRaw(frame, frame.Length);
        }

        public void Disconnect(KcpPeer peer)
        {
            IPEndPoint key = null;
            foreach (var kv in peers)
                if (kv.Value == peer) { key = kv.Key; break; }
            if (key != null)
            {
                peers.Remove(key);
                PeerDisconnected?.Invoke(peer);
            }
        }

        public void Dispose()
        {
            udp?.Close();
            udp = null;
        }
    }
}
