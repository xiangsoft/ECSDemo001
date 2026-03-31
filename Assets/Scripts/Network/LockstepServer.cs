using kcp2k;
using MemoryPack;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.Network
{
    public class LockstepServer : MonoBehaviour
    {
        public ushort Port = 7777;
        public int LogicFrameRate = 30;

        private KcpServer server;
        private float logicTickTime;
        private float accumulator;
        private int currentFrameId;

        private List<PlayerCommand> currentFrameCommands = new List<PlayerCommand>(64);
        private Dictionary<int, int> connectedPlayers = new Dictionary<int, int>(64);

        private void Start()
        {
            logicTickTime = 1f / LogicFrameRate;
            accumulator = 0f;

            KcpConfig config = new KcpConfig
            {
                NoDelay = true,
                DualMode = false,
                Interval = 10,
                Timeout = 2000,
                SendWindowSize = 4096,
                ReceiveWindowSize = 4096
            };

            server = new KcpServer(onConnected, onDataReceived, onDisconnected, onError, config);
            server.Start(Port);
        }

        private void Update()
        {
            if (!server.IsActive())
                return;

            server.TickIncoming();

            accumulator += Time.deltaTime;
            while (accumulator >= logicTickTime)
            {
                accumulator = 0f;
                currentFrameId++;
                broadcastFrameData();
            }

            server.TickOutgoing();
        }

        private void broadcastFrameData()
        {
            FrameData frameData = new FrameData { FrameID = currentFrameId };
            frameData.Commands.AddRange(currentFrameCommands);

            byte[] data = MemoryPackSerializer.Serialize(frameData);
            ArraySegment<byte> segment = new ArraySegment<byte>(data);

            foreach (var connectionId in connectedPlayers.Keys)
            {
                server.Send(connectionId, segment, KcpChannel.Reliable);
            }

            currentFrameCommands.Clear();
        }

        private void onConnected(int connectionId)
        {
            int playerId = connectedPlayers.Count + 1;
            connectedPlayers.Add(connectionId, playerId);
            Debug.Log($"[Server] 玩家已连接，分配 PlayerID: {playerId} ConnectionID:{connectionId}");
        }

        private void onDataReceived(int connectionId, ArraySegment<byte> message, KcpChannel channel)
        {
            // 解析客户端上行的操作
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(message.Array, message.Offset, message.Count);
            PlayerCommand cmd = MemoryPackSerializer.Deserialize<PlayerCommand>(span);
            currentFrameCommands.Add(cmd);
        }

        private void onDisconnected(int connectionId)
        {
            Debug.Log($"[Server] 玩家PlayerID: {connectedPlayers[connectionId]} 已断开连接");
            connectedPlayers.Remove(connectionId);
        }

        private void onError(int connectionId, ErrorCode error, string reason)
        {
            Debug.LogWarning($"[Server] 连接ID: {connectionId} 错误: {error} 原因: {reason}");
        }
    }
}