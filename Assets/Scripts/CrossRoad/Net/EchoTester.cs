using System;
using UnityEngine;
using Xiangsoft.Game;
using Xiangsoft.Game.Net;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// Stage-1 connectivity probe: connects to the local relay and can fire echo
    /// rounds from eval. Remove once the real client flow (rooms) takes over.
    /// All output goes through GameLog (channel "net") — silence via
    /// GameLog.SetChannel("net", false) or GameLog.Level = LogLevel.None.
    /// </summary>
    public class EchoTester : MonoBehaviour
    {
        private KcpClient client;
        private int replies;
        private double lastRttMs;

        public bool IsConnected => client?.Connected ?? false;
        public int Replies => replies;
        public double LastRttMs => lastRttMs;

        private void Start()
        {
            client = new KcpClient();
            client.FrameReceived += (data, len) => OnFrame(data, len);
            client.Connect("127.0.0.1", 7777);
            GameLog.Info("net", "EchoTester connecting to 127.0.0.1:7777");
        }

        private void Update() => client?.Update();

        private void OnFrame(byte[] data, int len)
        {
            if (!Wire.TryDecode(data, len, out var id, out var payload)) return;
            if (id == MsgId.S2C_EchoReply)
            {
                var reply = Wire.Decode<S2C_EchoReply>(payload);
                lastRttMs = (DateTime.UtcNow.Ticks - reply.ClientTime) * 1000.0 / TimeSpan.TicksPerSecond;
                replies++;
                GameLog.Debug("net", $"echo #{replies} \"{reply.Text}\" rtt={lastRttMs:F1}ms");
            }
            else if (id == MsgId.S2C_HelloAck)
            {
                GameLog.Debug("net", $"handshake acked, conv={Wire.Decode<S2C_HelloAck>(payload).Conv}");
            }
        }

        /// <summary>Fire one echo round; call from eval or a debug key.</summary>
        public void FireEcho(string text)
        {
            if (client == null || !client.Connected)
            {
                GameLog.Warn("net", "not connected yet");
                return;
            }
            client.Send(MsgId.C2S_Echo, new C2S_Echo { Text = text, ClientTime = DateTime.UtcNow.Ticks });
        }

        public void SendHello(string name)
        {
            client?.Send(MsgId.C2S_Hello, new C2S_Hello { PlayerName = name });
        }

        private void OnDestroy() => client?.Dispose();
    }
}
