using System;
using System.Collections.Generic;
using System.Linq;
using Xiangsoft.Game.Net;

namespace CrossRoadServer
{
    /// <summary>
    /// 服务器侧房间成员：conv（KCP 会话号）关联连接，Name 供展示。
    /// </summary>
    internal sealed class RoomMember
    {
        public uint Conv;
        public string Name;
    }

    /// <summary>
    /// 服务器侧房间。房主 = Members[0]（加入序）；房主离开时转移给下一个成员。
    /// 对局态：Seed（开局广播的世界种子）与 HopSeq（跳格定序号，单调递增）。
    /// </summary>
    internal sealed class Room
    {
        public string Code;
        public List<RoomMember> Members = new();
        public int Seed;
        public int HopSeq;

        public int SlotOf(uint conv)
        {
            for (int i = 0; i < Members.Count; i++)
                if (Members[i].Conv == conv) return i;
            return -1;
        }
    }

    /// <summary>
    /// 阶段 2 房间信令：内存态房间注册表 + 消息处理。
    /// 房间生命周期：CreateRoom 建 → Join/Leave 变动 → 最后一人离开销毁。
    /// 全员广播统一走 S2C_RoomState（每接收者一份，IsYou 按其 conv 计算）。
    /// </summary>
    public sealed class RoomRegistry
    {
        private const int MaxPlayers = 4;
        private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // 去除易混淆字符（I/O/0/1）

        private readonly KcpServer server;
        private readonly Dictionary<string, Room> roomsByCode = new();
        private readonly Dictionary<uint, Room> roomByConv = new();   // 成员反查（一个 conv 同时只在一个房间）
        private readonly Dictionary<uint, KcpPeer> peersByConv = new();   // conv → 连接（会话层零改动，自维护索引）
        private readonly Dictionary<uint, DateTime> lastSeenByConv = new();   // 心跳判活：收任何包刷新
        private readonly Dictionary<uint, string> nameByConv = new();   // 服务器分配的玩家名（Hello 时登记）
        private readonly Random rng = new();

        /// <summary>Hello 握手时登记服务器分配的玩家名（建房/加入一律使用，忽略客户端自报）。</summary>
        public void AssignName(uint conv, string name) => nameByConv[conv] = name;

        /// <summary>取 conv 的登记名；未登记时兜底（不应发生——Hello 先于房间消息）。</summary>
        private string NameOf(uint conv) => nameByConv.TryGetValue(conv, out var name) ? name : $"玩家{conv}";

        /// <summary>判定连接死亡的静默时长（秒）——超过即踢出并清理房间。</summary>
        public static readonly TimeSpan SilenceTimeout = TimeSpan.FromSeconds(30);

        public RoomRegistry(KcpServer server)
        {
            this.server = server;
            server.PeerConnected += peer =>
            {
                peersByConv[peer.Conv] = peer;
                lastSeenByConv[peer.Conv] = DateTime.UtcNow;
            };
            server.PeerDisconnected += peer =>
            {
                peersByConv.Remove(peer.Conv);
                lastSeenByConv.Remove(peer.Conv);
                nameByConv.Remove(peer.Conv);
                OnPeerDisconnected(peer);
            };
            server.FrameReceived += (peer, id, payload) =>
                lastSeenByConv[peer.Conv] = DateTime.UtcNow;   // any packet counts as alive
        }

        /// <summary>
        /// 心跳清扫（主循环定期调用）：静默超时的连接判死——踢出会话并走既有断开清理
        /// （清出房间、房主转移、空房销毁）。解决"死连接占房"。
        /// </summary>
        public void Sweep()
        {
            var now = DateTime.UtcNow;
            List<uint> dead = null;
            foreach (var kv in lastSeenByConv)
            {
                if (now - kv.Value > SilenceTimeout)
                {
                    (dead ??= new List<uint>()).Add(kv.Key);
                }
            }
            if (dead == null) return;

            foreach (var conv in dead)
            {
                if (peersByConv.TryGetValue(conv, out var peer))
                {
                    Console.WriteLine($"[room] conv={conv} silent >{SilenceTimeout.TotalSeconds:F0}s — kicking");
                    server.Disconnect(peer);   // triggers PeerDisconnected → room cleanup
                }
            }
        }

        /// <summary>入口：分发房间相关消息（非房间消息由调用方自行处理）。</summary>
        public void Handle(KcpPeer peer, MsgId id, byte[] payload)
        {
            switch (id)
            {
                case MsgId.C2S_CreateRoom: HandleCreate(peer, payload); break;
                case MsgId.C2S_ListRooms: HandleList(peer); break;
                case MsgId.C2S_JoinRoom: HandleJoin(peer, payload); break;
                case MsgId.C2S_LeaveRoom: HandleLeave(peer); break;
                case MsgId.C2S_StartGame: HandleStart(peer); break;
                case MsgId.C2S_Hop: HandleHop(peer, payload); break;
                case MsgId.C2S_Died: HandleDied(peer, payload); break;
            }
        }

        /// <summary>连接断开时清理：等同离开房间。</summary>
        public void OnPeerDisconnected(KcpPeer peer)
        {
            if (roomByConv.TryGetValue(peer.Conv, out var room))
            {
                RemoveFrom(room, peer.Conv, disconnect: true);
                Console.WriteLine($"[room] conv={peer.Conv} dropped from room {room.Code} (disconnect)");
            }
        }

        // ------------------------------ 各消息处理 ------------------------------

        private void HandleCreate(KcpPeer peer, byte[] payload)
        {
            var req = Wire.Decode<C2S_CreateRoom>(payload);
            LeaveIfNeeded(peer.Conv);   // 防呆：已在房间则先离开

            var room = new Room { Code = GenerateCode() };
            room.Members.Add(new RoomMember { Conv = peer.Conv, Name = NameOf(peer.Conv) });
            roomsByCode[room.Code] = room;
            roomByConv[peer.Conv] = room;

            Console.WriteLine($"[room] conv={peer.Conv} \"{req.PlayerName}\" created {room.Code}");
            BroadcastState(room);
        }

        private void HandleList(KcpPeer peer)
        {
            var list = new S2C_RoomList
            {
                Rooms = roomsByCode.Values
                    .Select(r => new RoomEntry
                    {
                        Code = r.Code,
                        HostName = r.Members.Count > 0 ? r.Members[0].Name : "?",
                        PlayerCount = r.Members.Count,
                    })
                    .ToList(),
            };
            server.SendTo(peer, MsgId.S2C_RoomList, list);
            Console.WriteLine($"[room] conv={peer.Conv} listed {list.Rooms.Count} rooms");
        }

        private void HandleJoin(KcpPeer peer, byte[] payload)
        {
            var req = Wire.Decode<C2S_JoinRoom>(payload);

            if (!roomsByCode.TryGetValue(req.RoomCode, out var room) || room.Members.Count == 0)
            {
                server.SendTo(peer, MsgId.S2C_JoinRejected, new S2C_JoinRejected { Reason = "房间不存在" });
                Console.WriteLine($"[room] conv={peer.Conv} join \"{req.RoomCode}\" rejected: not found");
                return;
            }
            if (room.Members.Count >= MaxPlayers)
            {
                server.SendTo(peer, MsgId.S2C_JoinRejected, new S2C_JoinRejected { Reason = "房间已满" });
                Console.WriteLine($"[room] conv={peer.Conv} join {room.Code} rejected: full");
                return;
            }

            LeaveIfNeeded(peer.Conv);   // 从旧房间切过来
            room.Members.Add(new RoomMember { Conv = peer.Conv, Name = NameOf(peer.Conv) });
            roomByConv[peer.Conv] = room;

            Console.WriteLine($"[room] conv={peer.Conv} \"{req.PlayerName}\" joined {room.Code} ({room.Members.Count}/{MaxPlayers})");
            BroadcastState(room);
        }

        private void HandleLeave(KcpPeer peer)
        {
            if (roomByConv.TryGetValue(peer.Conv, out var room))
            {
                RemoveFrom(room, peer.Conv, disconnect: false);
                Console.WriteLine($"[room] conv={peer.Conv} left {room.Code}");
            }
        }

        private void HandleStart(KcpPeer peer)
        {
            if (!roomByConv.TryGetValue(peer.Conv, out var room)) return;
            if (room.Members[0].Conv != peer.Conv) return;        // 只有房主能开局
            if (room.Members.Count < 2) return;                    // 至少 2 人（防刷：校验失败静默丢弃）

            // stage 3: the broadcast carries the world seed + slot map so every client
            // runs the same deterministic simulation from the same starting state
            room.Seed = rng.Next();
            room.HopSeq = 0;

            Console.WriteLine($"[room] {room.Code} game started by conv={peer.Conv} ({room.Members.Count} players, seed={room.Seed})");
            var started = new S2C_GameStarted
            {
                ServerTime = DateTime.UtcNow.Ticks,
                Seed = room.Seed,
                Slots = room.Members.Select((m, i) => new GameSlot { Slot = i, Name = m.Name, Conv = m.Conv }).ToList(),
            };
            foreach (var member in room.Members)
                SendToPeerConv(member.Conv, MsgId.S2C_GameStarted, started);
        }

        /// <summary>跳格定序：赋予服务器序号后向全员广播（含发起者，发起端按槽位跳过自己的回声）。</summary>
        private void HandleHop(KcpPeer peer, byte[] payload)
        {
            if (!roomByConv.TryGetValue(peer.Conv, out var room)) return;
            var hop = Wire.Decode<C2S_Hop>(payload);

            room.HopSeq++;
            int slot = room.SlotOf(peer.Conv);
            var broadcast = new S2C_HopBroadcast
            {
                Slot = slot,
                Direction = hop.Direction,
                ServerSeq = room.HopSeq,
                ClientSeq = hop.ClientSeq,
            };
            foreach (var member in room.Members)
                SendToPeerConv(member.Conv, MsgId.S2C_HopBroadcast, broadcast);
        }

        /// <summary>死亡转发：转发给房间全员（各端确定性判定一致，广播只是触发表现）。</summary>
        private void HandleDied(KcpPeer peer, byte[] payload)
        {
            if (!roomByConv.TryGetValue(peer.Conv, out var room)) return;
            var died = Wire.Decode<C2S_Died>(payload);

            int slot = room.SlotOf(peer.Conv);
            var broadcast = new S2C_PlayerDied
            {
                Slot = slot,
                Reason = died.Reason,
                DeathKind = died.DeathKind,
            };
            foreach (var member in room.Members)
                SendToPeerConv(member.Conv, MsgId.S2C_PlayerDied, broadcast);
            Console.WriteLine($"[room] {room.Code} slot={slot} died: {died.Reason}");
        }

        // ------------------------------ 内部工具 ------------------------------

        /// <summary>把成员移出房间：房主离开则转移房主；房间清空则销毁。</summary>
        private void RemoveFrom(Room room, uint conv, bool disconnect)
        {
            var member = room.Members.FirstOrDefault(m => m.Conv == conv);
            if (member == null) return;
            room.Members.Remove(member);
            roomByConv.Remove(conv);

            if (room.Members.Count == 0)
            {
                roomsByCode.Remove(room.Code);
                Console.WriteLine($"[room] {room.Code} destroyed (empty)");
                return;
            }

            // 剩余成员收到新状态（房主已转移：Members[0] 即新房主）
            BroadcastState(room);
        }

        /// <summary>向房间全员广播房间状态（每人一份，IsYou 按接收者计算）。</summary>
        private void BroadcastState(Room room)
        {
            foreach (var member in room.Members)
            {
                var state = new S2C_RoomState
                {
                    RoomCode = room.Code,
                    YouAreHost = room.Members[0].Conv == member.Conv,
                    Members = room.Members.Select(m => new MemberEntry
                    {
                        Name = m.Name,
                        IsHost = room.Members[0].Conv == m.Conv,
                        IsYou = m.Conv == member.Conv,
                    }).ToList(),
                };
                SendToPeerConv(member.Conv, MsgId.S2C_RoomState, state);
            }
        }

        /// <summary>按 conv 找到连接并发送（找不到则丢弃——连接可能已断）。</summary>
        private void SendToPeerConv(uint conv, MsgId id, object message)
        {
            if (peersByConv.TryGetValue(conv, out var peer))
                server.SendTo(peer, id, message);
        }

        private void LeaveIfNeeded(uint conv)
        {
            if (roomByConv.TryGetValue(conv, out var old))
                RemoveFrom(old, conv, disconnect: false);
        }

        private string GenerateCode()
        {
            string code;
            do
            {
                var chars = new char[4];
                lock (rng) for (int i = 0; i < 4; i++) chars[i] = CodeChars[rng.Next(CodeChars.Length)];
                code = new string(chars);
            } while (roomsByCode.ContainsKey(code));   // 撞码重试（4 位码空间 32^4 = 100 万，实际撞率极低）
            return code;
        }
    }
}
