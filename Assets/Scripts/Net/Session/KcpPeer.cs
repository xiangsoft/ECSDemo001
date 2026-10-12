using System;

namespace Xiangsoft.Game.Net
{
    /// <summary>
    /// One reliable KCP conversation over a shared UDP socket. The transport is injected
    /// as a raw send callback, so client and server reuse this unchanged.
    /// NOTE: ET's flush writes packets at REVERSED_HEAD offset inside its buffer —
    /// the output callback here slices the real packet out before handing it to UDP.
    /// </summary>
    public sealed class KcpPeer
    {
        private readonly Kcp kcp;
        private readonly byte[] sendBuf = new byte[65536];

        /// <summary>A complete wire frame arrived (msgId + payload).</summary>
        public event Action<byte[], int> FrameReceived;

        public uint Conv => kcp.ConversationId;
        public uint WaitSendCount => kcp.WaitSendCount;

        // stage-1 diagnostics
        public int UdpPacketsOut { get; private set; }
        public int UpdateCalls { get; private set; }
        public int LastInputResult { get; private set; }
        public int FramesDecoded { get; private set; }

        public KcpPeer(uint conv, Action<byte[], int> udpSend)
        {
            kcp = new Kcp(conv, (buffer, length) =>
            {
                Buffer.BlockCopy(buffer, (int)KCPBASIC.REVERSED_HEAD, sendBuf, 0, length);
                UdpPacketsOut++;
                udpSend(sendBuf, length);
            });
            kcp.SetNoDelay(1, 10, 2, 1);   // low-latency profile
            kcp.SetWindowSize(64, 64);      // 4-player traffic is tiny
        }

        /// <summary>Feed one UDP datagram that belongs to this conversation.</summary>
        public void OnUdpPacket(byte[] data, int length)
        {
            LastInputResult = kcp.Input(data, length);
        }

        public void SendRaw(byte[] data, int length)
        {
            kcp.Send(data, 0, length);
        }

        /// <summary>Pump timers and drain received frames. Drive this from your loop.</summary>
        public void Update(uint millis)
        {
            UpdateCalls++;
            kcp.Update(millis);

            int peek;
            while ((peek = kcp.PeekSize()) > 0)
            {
                var buffer = new byte[peek];
                int n = kcp.Receive(buffer);
                if (n <= 0) break;
                FramesDecoded++;
                FrameReceived?.Invoke(buffer, n);
            }
        }
    }
}
