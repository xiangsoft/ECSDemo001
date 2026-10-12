using System.Collections.Generic;
using UnityEngine;
using Xiangsoft.Game;
using Xiangsoft.Game.Net;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>
    /// 阶段 2 网络客户端：封装 KcpClient，主循环驱动收发，
    /// 把服务器推送（房间状态/列表/开局）转成 RoomCatalog 的数据与事件。
    /// 默认离线——单机玩法与全部离线测试不受影响；
    /// ConnectServer() 之后进入联机模式（eval 驱动或将来由 UI 触发）。
    /// </summary>
    public class NetClient : MonoBehaviour
    {
        public static NetClient Instance { get; private set; }

        private KcpClient client;
        private CrossRoadEvents events;
        private string playerName = "Player";

        /// <summary>是否已连接服务器（RoomCatalog 据此切换真/假数据源）。</summary>
        public bool Online { get; private set; }

        /// <summary>KCP 握手是否完成（Online=已发起连接，Connected=链路已建立）。</summary>
        public bool Connected => client != null && client.Connected;

        /// <summary>本端在对局中的槽位号（S2C_GameStarted 解析；-1 = 不在对局）。</summary>
        public int LocalSlot { get; private set; } = -1;

        /// <summary>开局广播携带的槽位分配（Bootstrap 据此生成远程鸡）。</summary>
        public List<Xiangsoft.Game.Net.GameSlot> Slots { get; private set; }

        private int hopSeq;   // local hop sequence for debugging reconciliation

        // heartbeat: ping every 5s; if no pong for 15s the link is dead → reset to menu state
        private const float PingInterval = 5f;
        private const float PongTimeout = 15f;
        private float pingTimer;
        private float pongTimer;
        private bool awaitingPong;

        // messages sent before the KCP handshake completes are queued and flushed on connect
        // (the handshake window is real: UI flows call create/join right after ConnectServer)
        private readonly List<System.ValueTuple<MsgId, object>> pendingWhileConnecting =
            new List<System.ValueTuple<MsgId, object>>();

        /// <summary>联机房间列表缓存（S2C_RoomList 到达时整体替换，UI 打开列表时读）。</summary>
        public readonly List<RoomInfo> CachedRooms = new List<RoomInfo>();

        private void Awake() => Instance = this;

        /// <summary>装配（GameBootstrap 调用）：注入事件通道，开局广播经此触发。</summary>
        public void Init(CrossRoadEvents crossRoadEvents) => events = crossRoadEvents;

        /// <summary>服务器分配的玩家名（HelloAck 下发；连接中为 null）。</summary>
        public string AssignedName { get; private set; }

        // stage 4: auto-reconnect — after the first connect, a dropped link retries every 5s
        private bool autoConnect;
        private float retryTimer;

        /// <summary>
        /// 连接服务器（阶段 4：玩家名由服务器分配，name 参数仅作 UI 兼容保留、被忽略）。
        /// RoomCatalog.OnlineMode 在收到 HelloAck（拿到分配名）时才置 true——
        /// 未完成握手的阶段 UI 走“等待连接”提示，测试环境（无服务器）保持假数据模式。
        /// </summary>
        public void ConnectServer(string host, int port, string name = null)
        {
            playerName = "Player";   // self-reported name is dead: the server assigns real names now
            client = new KcpClient();
            client.ConnectedOnce += OnHandshakeComplete;
            client.FrameReceived += OnFrame;
            client.Connect(host, port);
            Online = true;
            autoConnect = true;
            retryTimer = 0f;
            GameLog.Info("net", $"NetClient connecting {host}:{port} (server will assign the name)");
        }

        /// <summary>握手完成：先发 Hello（领分配名），排队的消息随后发。</summary>
        private void OnHandshakeComplete()
        {
            SendOrQueue(MsgId.C2S_Hello, new C2S_Hello());   // no self-reported name — the server assigns
            FlushPending();
        }

        /// <summary>排队发送：握手完成立即发，否则入队（连接成功时 flush）。</summary>
        private void SendOrQueue(MsgId id, object message)
        {
            if (client != null && client.Connected) client.Send(id, message);
            else pendingWhileConnecting.Add((id, message));
        }

        private void FlushPending()
        {
            foreach (var pair in pendingWhileConnecting)
                client?.Send(pair.Item1, pair.Item2);
            pendingWhileConnecting.Clear();
        }

        private void Update()
        {
            client?.Update();
            Heartbeat();
            AutoReconnect();
        }

        /// <summary>自动重连：首次连接后链路断开（心跳判死）则每 5 秒重试，直到成功。</summary>
        private void AutoReconnect()
        {
            if (!autoConnect || client != null) return;
            retryTimer += UnityEngine.Time.deltaTime;
            if (retryTimer >= 5f)
            {
                retryTimer = 0f;
                GameLog.Info("net", "自动重连服务器...");
                ConnectServer("127.0.0.1", 7777);
            }
        }

        /// <summary>发心跳 + 检测断线（Pong 超时 → 复位联机状态，回单机）。</summary>
        private void Heartbeat()
        {
            if (client == null || !Online || !client.Connected) return;

            pingTimer += UnityEngine.Time.deltaTime;
            pongTimer += UnityEngine.Time.deltaTime;

            if (pingTimer >= PingInterval)
            {
                pingTimer = 0f;
                SendOrQueue(MsgId.C2S_Ping, new C2S_Ping());
                if (!awaitingPong)
                {
                    awaitingPong = true;   // waiting for this ping's pong
                    pongTimer = 0f;
                }
            }

            if (awaitingPong && pongTimer >= PongTimeout)
            {
                GameLog.Warn("net", "服务器无响应超过 15 秒，判定断线——已复位，请重新进入房间");
                awaitingPong = false;
                ResetOnline();
            }
        }

        /// <summary>断线复位：关闭连接、清状态（不自动跳 UI——用户手动回主菜单/重进房间）。</summary>
        private void ResetOnline()
        {
            try { client?.Dispose(); } catch { }
            client = null;
            Online = false;
            LocalSlot = -1;
            Slots = null;
            AssignedName = null;
            RoomCatalog.OnlineMode = false;
            RoomCatalog.CurrentSession = null;
            RoomCatalog.InGame = false;
            awaitingPong = false;
            pingTimer = 0f;
            pongTimer = 0f;
        }

        private void OnDestroy()
        {
            client?.Dispose();
            if (RoomCatalog.OnlineMode) RoomCatalog.OnlineMode = false;
            Instance = null;
        }

        // ------------------------------ 对外操作（RoomCatalog 转发） ------------------------------

        /// <summary>请求建房（响应走 S2C_RoomState 推送）。</summary>
        public void CreateRoom() =>
            SendOrQueue(MsgId.C2S_CreateRoom, new C2S_CreateRoom { PlayerName = playerName });

        /// <summary>请求房间列表（响应走 S2C_RoomList）。</summary>
        public void ListRooms() =>
            SendOrQueue(MsgId.C2S_ListRooms, new C2S_ListRooms());

        /// <summary>请求加入指定房间码（成功走 S2C_RoomState，失败走 S2C_JoinRejected）。</summary>
        public void JoinRoom(string roomCode) =>
            SendOrQueue(MsgId.C2S_JoinRoom, new C2S_JoinRoom { RoomCode = roomCode, PlayerName = playerName });

        /// <summary>离开当前房间（无专属响应；房内其他人会收到新状态广播）。</summary>
        public void LeaveRoom() =>
            SendOrQueue(MsgId.C2S_LeaveRoom, new C2S_LeaveRoom());

        /// <summary>房主请求开局（服务器校验通过后全员收到 S2C_GameStarted）。</summary>
        public void RequestStartGame() =>
            SendOrQueue(MsgId.C2S_StartGame, new C2S_StartGame());

        // ------------------------------ 对局内同步（阶段 3） ------------------------------

        /// <summary>本地乐观跳已执行：上报方向供服务器定序广播（dx/dz → 0=前 1=后 2=左 3=右）。</summary>
        public void ReportHop(int dx, int dz)
        {
            int dir = dz > 0 ? 0 : dz < 0 ? 1 : dx < 0 ? 2 : 3;
            hopSeq++;
            SendOrQueue(MsgId.C2S_Hop, new C2S_Hop { Direction = dir, ClientSeq = hopSeq });
        }

        /// <summary>本端判定的死亡：上报（各端确定性判定一致，广播触发其他端播放表现）。</summary>
        public void ReportDied(string reason, DeathKind kind) =>
            SendOrQueue(MsgId.C2S_Died, new C2S_Died { Reason = reason, DeathKind = (int)kind });

        /// <summary>按开局槽位生成远程鸡（除自己外每个槽位一只）；由 Bootstrap 在 GameStarted 时调用。</summary>
        public void SpawnRemotes(LaneManager lanes)
        {
            RemotePlayer.ClearAll();
            if (Slots == null) return;
            foreach (var slot in Slots)
            {
                if (slot.Slot == LocalSlot) continue;
                var go = new GameObject("RemotePlayer");
                go.AddComponent<RemotePlayer>().Init(slot.Slot, slot.Name, lanes);
            }
        }

        // ------------------------------ 服务器推送处理 ------------------------------

        private void OnFrame(byte[] data, int length)
        {
            if (!Wire.TryDecode(data, length, out var id, out var payload)) return;

            switch (id)
            {
                case MsgId.S2C_RoomState:
                    RoomCatalog.ApplyRoomState(Wire.Decode<S2C_RoomState>(payload));
                    break;

                case MsgId.S2C_RoomList:
                    var list = Wire.Decode<S2C_RoomList>(payload);
                    CachedRooms.Clear();
                    foreach (var room in list.Rooms)
                        CachedRooms.Add(new RoomInfo { Code = room.Code, HostName = room.HostName, PlayerCount = room.PlayerCount });
                    RoomCatalog.NotifyRoomsChanged();
                    break;

                case MsgId.S2C_JoinRejected:
                    var rejected = Wire.Decode<S2C_JoinRejected>(payload);
                    GameLog.Warn("net", $"加入被拒：{rejected.Reason}");
                    RoomCatalog.NotifyJoinRejected(rejected.Reason);
                    break;

                case MsgId.S2C_HelloAck:
                {
                    var ack = Wire.Decode<S2C_HelloAck>(payload);
                    AssignedName = ack.AssignedName;
                    RoomCatalog.OnlineMode = true;   // real online from here: names assigned, room flows go to the network
                    GameLog.Info("net", $"服务器连接成功，分配玩家名：{ack.AssignedName}");
                    break;
                }

                case MsgId.S2C_Pong:
                    awaitingPong = false;   // link alive — the pending ping was answered
                    pongTimer = 0f;
                    break;

                case MsgId.S2C_GameStarted:
                {
                    var started = Wire.Decode<S2C_GameStarted>(payload);
                    Slots = started.Slots;
                    LocalSlot = -1;
                    if (Slots != null)
                        foreach (var slot in Slots)
                            if (slot.Conv == client.Conv) { LocalSlot = slot.Slot; break; }

                    // deterministic world: seed the sim BEFORE the local GameStarted flow runs
                    Xiangsoft.Game.CrossRoad.Sim.Begin(started.Seed);

                    GameLog.Info("net", $"服务器广播开局：seed={started.Seed} slots={Slots?.Count} local={LocalSlot}");
                    RoomCatalog.InGame = true;
                    events?.GameStarted.Raise();
                    break;
                }

                case MsgId.S2C_HopBroadcast:
                {
                    var hop = Wire.Decode<S2C_HopBroadcast>(payload);
                    if (hop.Slot == LocalSlot) break;   // echo of our own optimistic hop — already executed
                    Xiangsoft.Game.CrossRoad.RemotePlayer.Apply(hop.Slot, hop.Direction);
                    break;
                }

                case MsgId.S2C_PlayerDied:
                {
                    var died = Wire.Decode<S2C_PlayerDied>(payload);
                    if (died.Slot == LocalSlot) break;   // our own death — already handled locally
                    Xiangsoft.Game.CrossRoad.RemotePlayer.ApplyDeathRemote(died.Slot, (DeathKind)died.DeathKind);
                    GameLog.Info("net", $"玩家死亡广播：slot={died.Slot} reason={died.Reason}");
                    break;
                }
            }
        }
    }
}
