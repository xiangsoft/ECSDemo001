using kcp2k;
using MemoryPack;
using System;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Xiangsoft.Game.Network
{
    public class LockstepClient : MonoBehaviour
    {
        public static LockstepClient Instance { get; private set; }

        public string ServerIP = "127.0.0.1";
        public ushort Port = 7777;

        private KcpClient client;

        // ★ 锁步引擎的“粮仓”：历史帧队列
        public Queue<FrameData> FrameQueue = new Queue<FrameData>();

        public bool IsConnected { get { return client != null && client.connected; } }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            KcpConfig config = new KcpConfig
            {
                NoDelay = true,
                Interval = 10,
                Timeout = 2000,
                SendWindowSize = 4096,
                ReceiveWindowSize = 4096
            };
            client = new KcpClient(onConnected, onDataReceived, onDisconnected, onError, config);
            client.Connect(ServerIP, Port);
        }

        private void Update()
        {
            client.TickIncoming();
            client.TickOutgoing();
        }

        public void SendLocalCommand(PlayerCommand cmd)
        {
            if (!IsConnected)
                return;

            byte[] data = MemoryPackSerializer.Serialize(cmd);
            client.Send(new ArraySegment<byte>(data), KcpChannel.Reliable);
        }

        private void onConnected()
        {
            Debug.Log("[Client] 已连接到服务器！");
        }

        private void onDataReceived(ArraySegment<byte> message, KcpChannel channel)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(message.Array, message.Offset, message.Count);
            FrameData frame = MemoryPackSerializer.Deserialize<FrameData>(span);

            // 将服务器下发的确定性帧塞入队列，等待 TimeManager 消费
            FrameQueue.Enqueue(frame);
        }

        private void onDisconnected()
        {
            Debug.Log("[Client] 已断开连接！");
        }

        private void onError(ErrorCode error, string message)
        {
            Debug.LogWarning($"[Client] 错误发生：{error} - {message}");
        }
    }
}