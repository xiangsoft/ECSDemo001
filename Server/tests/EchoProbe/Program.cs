using System.Diagnostics;
using System.Threading;
using Xiangsoft.Game.Net;

namespace EchoProbe
{
    /// <summary>
    /// Stage-2 second-player probe: connects over KCP and can create/join/list rooms
    /// next to the Unity client — validates the room signalling without a second editor.
    /// Usage: dotnet run [create <name>] | [join <code> <name>] | [list]   (default: echo rounds)
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string host = "127.0.0.1";
            int port = 7777;

            using var client = new KcpClient();
            var connected = new ManualResetEventSlim(false);
            var done = new ManualResetEventSlim(false);
            bool gameStarted = false;   // stage-3 play flow state (captured by the frame handler below)
            int hopsSent = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();   // connection clock; restarted when the game starts
            string mode = args.Length > 0 ? args[0] : "echo";
            string roomCode = args.Length > 1 ? args[1] : null;
            string playerName = args.Length > 2 ? args[2] : "ProbePlayer";

            int stateCount = 0;

            client.ConnectedOnce += () =>
            {
                Console.WriteLine($"[probe] connected, conv established");
                connected.Set();
            };

            client.FrameReceived += (data, len) =>
            {
                if (!Wire.TryDecode(data, len, out var id, out var payload)) return;
                switch (id)
                {
                    case MsgId.S2C_RoomState:
                        var state = Wire.Decode<S2C_RoomState>(payload);
                        stateCount++;
                        Console.WriteLine($"[probe] room-state #{stateCount}: code={state.RoomCode} host={(state.YouAreHost ? "yes" : "no")} " +
                            $"members=[{string.Join(",", state.Members.ConvertAll(m => m.Name + (m.IsHost ? "*" : "") + (m.IsYou ? "(you)" : "")))}]");
                        break;

                    case MsgId.S2C_RoomList:
                        var list = Wire.Decode<S2C_RoomList>(payload);
                        Console.WriteLine($"[probe] room-list: {list.Rooms.Count} rooms: " +
                            string.Join(", ", list.Rooms.ConvertAll(r => $"{r.Code}:{r.HostName} {r.PlayerCount}/4")));
                        done.Set();
                        break;

                    case MsgId.S2C_JoinRejected:
                        Console.WriteLine($"[probe] join REJECTED: {Wire.Decode<S2C_JoinRejected>(payload).Reason}");
                        done.Set();
                        break;

                    case MsgId.S2C_GameStarted:
                    {
                        var started = Wire.Decode<S2C_GameStarted>(payload);
                        int mySlot = -1;
                        if (started.Slots != null)
                            foreach (var slot in started.Slots)
                                if (slot.Conv == client.Conv) { mySlot = slot.Slot; break; }
                        Console.WriteLine($"[probe] GAME STARTED: seed={started.Seed} mySlot={mySlot} players={started.Slots?.Count}");
                        if (mode == "play")
                        {
                            gameStarted = true;
                            hopsSent = 0;
                            sw.Restart();   // match clock starts NOW (was connection time — caused burst sends)
                        }
                        else done.Set();
                        break;
                    }

                    case MsgId.S2C_HopBroadcast:
                    {
                        var hop = Wire.Decode<S2C_HopBroadcast>(payload);
                        Console.WriteLine($"[probe] hop-broadcast: slot={hop.Slot} dir={hop.Direction} serverSeq={hop.ServerSeq} clientSeq={hop.ClientSeq}");
                        break;
                    }

                    case MsgId.S2C_PlayerDied:
                    {
                        var died = Wire.Decode<S2C_PlayerDied>(payload);
                        Console.WriteLine($"[probe] player-died broadcast: slot={died.Slot} reason={died.Reason} kind={died.DeathKind}");
                        if (mode == "play") done.Set();   // ghost-watch verified — end the probe
                        break;
                    }

                    case MsgId.S2C_EchoReply:
                        var reply = Wire.Decode<S2C_EchoReply>(payload);
                        double rttMs = (DateTime.UtcNow.Ticks - reply.ClientTime) * 1000.0 / TimeSpan.TicksPerSecond;
                        Console.WriteLine($"[probe] echo \"{reply.Text}\" rtt={rttMs:F1}ms");
                        break;
                }
            };

            Console.WriteLine($"[probe] connecting to {host}:{port} (mode={mode}) ...");
            client.Connect(host, port);

            var echoes = 0;
            bool fired = false;
            while (!done.IsSet)
            {
                client.Update();

                if (connected.IsSet && !fired && sw.ElapsedMilliseconds > 600)
                {
                    // fire the mode action once, right after the connection settles
                    fired = true;
                    if (mode == "create")
                        client.Send(MsgId.C2S_CreateRoom, new C2S_CreateRoom { PlayerName = playerName });
                    else if ((mode == "join" || mode == "play") && roomCode != null)
                        client.Send(MsgId.C2S_JoinRoom, new C2S_JoinRoom { RoomCode = roomCode, PlayerName = playerName });
                    else if (mode == "list")
                        client.Send(MsgId.C2S_ListRooms, new C2S_ListRooms());
                }

                // stage-3 play mode: after the game starts, hop forward every 600ms x5, then die
                if (mode == "play" && gameStarted && hopsSent < 5 && sw.ElapsedMilliseconds > 6000 + hopsSent * 600)
                {
                    hopsSent++;
                    client.Send(MsgId.C2S_Hop, new C2S_Hop { Direction = 0, ClientSeq = hopsSent });   // 0 = forward
                }
                if (mode == "play" && gameStarted && hopsSent >= 5 && sw.ElapsedMilliseconds > 6000 + 5 * 600 + 1500)
                {
                    Console.WriteLine("[probe] reporting death (ghost-watch test)");
                    client.Send(MsgId.C2S_Died, new C2S_Died { Reason = "death.train", DeathKind = 0 });
                    mode = "wait";
                }

                if (mode == "echo" && connected.IsSet && sw.ElapsedMilliseconds > echoes * 500)
                {
                    echoes++;
                    if (echoes > 5) { done.Set(); break; }
                    client.Send(MsgId.C2S_Echo, new C2S_Echo { Text = $"hello-{echoes}", ClientTime = DateTime.UtcNow.Ticks });
                }

                if (mode == "list" && fired && sw.ElapsedMilliseconds > 2500)
                    done.Set();   // list may be empty but the reply should have landed

                if ((mode == "join" || mode == "create") && sw.ElapsedMilliseconds > 8000)
                {
                    // room flow: linger so membership broadcasts can arrive, then exit
                    Console.WriteLine(stateCount > 0 ? "[probe] room flow observed" : "[probe] NO room state received");
                    done.Set();
                }

                if (sw.ElapsedMilliseconds > 60000)
                {
                    Console.WriteLine("[probe] TIMEOUT");
                    return 1;
                }

                Thread.Sleep(10);
            }

            Thread.Sleep(300);   // let the last packets flush
            Console.WriteLine("[probe] DONE");
            return 0;
        }
    }
}
