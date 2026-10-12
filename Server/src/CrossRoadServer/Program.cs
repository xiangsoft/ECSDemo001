using System;
using System.Threading;
using Xiangsoft.Game.Net;

namespace CrossRoadServer
{
    /// <summary>
    /// Cross Road 中继服务器 —— 阶段 2：房间信令（在阶段 1 echo 之上叠加）。
    /// 房间逻辑全部委托 RoomRegistry；echo 保留作传输层健康探针。
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            int port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 7777;

            var server = new KcpServer();
            var registry = new RoomRegistry(server);

            // stage 4: server-assigned player names ("玩家1001" onwards) — clients no longer self-report
            int playerSeq = 1000;

            server.PeerConnected += peer =>
                Console.WriteLine($"[conn] peer conv={peer.Conv} ({server.PeerCount} online)");
            server.PeerDisconnected += peer =>
                Console.WriteLine($"[disc] peer conv={peer.Conv} ({server.PeerCount} online)");

            server.FrameReceived += (peer, id, payload) =>
            {
                switch (id)
                {
                    case MsgId.C2S_Hello:
                        var hello = Wire.Decode<C2S_Hello>(payload);
                        var assigned = $"玩家{++playerSeq}";
                        registry.AssignName(peer.Conv, assigned);
                        Console.WriteLine($"[hello] conv={peer.Conv} assigned={assigned} (self-reported \"{hello.PlayerName}\" ignored)");
                        server.SendTo(peer, MsgId.S2C_HelloAck, new S2C_HelloAck
                        {
                            Conv = peer.Conv,
                            ServerTime = DateTime.UtcNow.Ticks,
                            AssignedName = assigned,
                        });
                        break;

                    case MsgId.C2S_Echo:
                        var echo = Wire.Decode<C2S_Echo>(payload);
                        server.SendTo(peer, MsgId.S2C_EchoReply, new S2C_EchoReply
                        {
                            Text = echo.Text,
                            ClientTime = echo.ClientTime,
                            ServerTime = DateTime.UtcNow.Ticks,
                        });
                        Console.WriteLine($"[echo] conv={peer.Conv} \"{echo.Text}\"");
                        break;

                    case MsgId.C2S_Ping:
                        server.SendTo(peer, MsgId.S2C_Pong, new S2C_Pong { ServerTime = DateTime.UtcNow.Ticks });
                        break;

                    default:
                        registry.Handle(peer, id, payload);   // 房间消息（7-15）
                        break;
                }
            };

            server.Start(port);
            Console.WriteLine($"CrossRoad relay listening on UDP {port} (stage 2: rooms)");

            // 10ms pump — KCP interval is 10ms, this keeps latency honest
            var diagSw = System.Diagnostics.Stopwatch.StartNew();
            var sweepSw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                server.Update();
                if (sweepSw.ElapsedMilliseconds > 10000)
                {
                    sweepSw.Restart();
                    registry.Sweep();   // heartbeat cleanup: kick silent peers, free their rooms
                }
                if (diagSw.ElapsedMilliseconds > 2000)
                {
                    diagSw.Restart();
                    Console.WriteLine($"[diag] udpIn={server.UdpIn} routed={server.RoutedToPeer} peers={server.PeerCount}");
                }
                Thread.Sleep(10);
            }
        }
    }
}
