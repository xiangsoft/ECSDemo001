using System.Collections.Generic;
using ProtoBuf;

namespace Xiangsoft.Game.Net
{
    // =====================================================================
    // 网络协议 · 消息体定义（protobuf-net 契约）
    // ---------------------------------------------------------------------
    // · 本文件由 Unity 客户端与 .NET 服务器【共享同一份源码】编译
    //   （Server 的 csproj 通过 <Compile Include> 链接本目录），
    //   修改即双端同时生效，协议永不漂移。
    // · 【ProtoMember(n) 的字段编号 = 线上协议契约】：
    //     - 编号一经投入使用【不可更改、不可复用】，改动会破坏新旧版本互通；
    //     - 只能新增字段（占用未用编号）；删除字段时保留编号空缺，不补位；
    //     - 字段名 / 属性写法可在兼容前提下调整，编号不可动。
    // · 命名约定：C2S_ 前缀 = 客户端发往服务器；S2C_ 前缀 = 服务器发往客户端。
    // · 时间戳统一使用 DateTime.UtcNow.Ticks（1 tick = 100ns，
    //   1ms = 10,000 ticks）；换算秒时除以 TimeSpan.TicksPerSecond。
    // · 每条消息注明其 MsgId 与用途，新消息延续此规范。
    // =====================================================================

    // ------------------------- 阶段 1：传输层验证 -------------------------

    /// <summary>【C2S，MsgId=1】连接建立后的第一笔业务消息：上报玩家名，供服务器登记与展示。</summary>
    [ProtoContract]
    public class C2S_Hello
    {
        /// <summary>玩家昵称（UTF-8 字符串，建议非空；长度上限由服务器裁定）。</summary>
        [ProtoMember(1)] public string PlayerName { get; set; }
    }

    /// <summary>【S2C，MsgId=2】服务器对 Hello 的确认：回分配的会话号、服务器时间与服务器分配的玩家名。</summary>
    [ProtoContract]
    public class S2C_HelloAck
    {
        /// <summary>服务器分配的会话号（conversation id），与 KCP 握手层的 conv 一致，标识本连接。</summary>
        [ProtoMember(1)] public uint Conv { get; set; }

        /// <summary>服务器当前时间（UtcNow.Ticks）：客户端可用于粗略对时或计算时钟偏移。</summary>
        [ProtoMember(2)] public long ServerTime { get; set; }

        /// <summary>服务器分配的玩家名（阶段 4 起客户端不再自报，统一由服务器生成，如“玩家1024”）。</summary>
        [ProtoMember(3)] public string AssignedName { get; set; }
    }

    /// <summary>【C2S，MsgId=3】阶段 1 传输层连通性探针：请求服务器原样回显。</summary>
    [ProtoContract]
    public class C2S_Echo
    {
        /// <summary>回显文本（任意内容，用于核对往返内容一致）。</summary>
        [ProtoMember(1)] public string Text { get; set; }

        /// <summary>客户端发送时刻（UtcNow.Ticks）：服务器原样带回，用于计算 RTT。</summary>
        [ProtoMember(2)] public long ClientTime { get; set; }
    }

    /// <summary>【S2C，MsgId=4】Echo 应答：原样回显文本与客户端时间戳，并附服务器收到时刻。</summary>
    [ProtoContract]
    public class S2C_EchoReply
    {
        /// <summary>回显文本（应与 C2S_Echo.Text 完全一致）。</summary>
        [ProtoMember(1)] public string Text { get; set; }

        /// <summary>客户端发送时刻（原样带回）：RTT = 当前时间 - ClientTime。</summary>
        [ProtoMember(2)] public long ClientTime { get; set; }

        /// <summary>服务器收到请求的时刻（UtcNow.Ticks）：可用于分离上行 / 下行延迟。</summary>
        [ProtoMember(3)] public long ServerTime { get; set; }
    }

    // ------------------------- 阶段 2：房间信令 -------------------------

    /// <summary>
    /// 【嵌套类型，随 S2C_RoomState / S2C_RoomList 携带】房间成员条目。
    /// IsYou = true 的那一条即当前客户端自己（每条房间状态里有且仅有一个）。</summary>
    [ProtoContract]
    public class MemberEntry
    {
        /// <summary>成员昵称（房间内唯一，服务器保证）。</summary>
        [ProtoMember(1)] public string Name { get; set; }

        /// <summary>是否房主（房主离开时转移给下一个成员）。</summary>
        [ProtoMember(2)] public bool IsHost { get; set; }

        /// <summary>是否当前客户端自己（客户端据此把该行渲染为“你”）。</summary>
        [ProtoMember(3)] public bool IsYou { get; set; }
    }

    /// <summary>【嵌套类型，随 S2C_RoomList 携带】房间列表条目（不含成员明细）。</summary>
    [ProtoContract]
    public class RoomEntry
    {
        /// <summary>4 位房间码（大写字母+数字，去易混淆字符）。</summary>
        [ProtoMember(1)] public string Code { get; set; }

        /// <summary>房主昵称（列表展示用）。</summary>
        [ProtoMember(2)] public string HostName { get; set; }

        /// <summary>当前人数（1-4；满员的房间客户端应禁用加入）。</summary>
        [ProtoMember(3)] public int PlayerCount { get; set; }
    }

    /// <summary>
    /// 【C2S，MsgId=7】创建房间：玩家上报昵称，服务器生成 4 位房间码并返回首个房间状态。
    /// 成功响应见 S2C_RoomState（建房场景下成员仅自己一人且为房主）。</summary>
    [ProtoContract]
    public class C2S_CreateRoom
    {
        /// <summary>建房玩家的昵称（成为该房间的房主）。</summary>
        [ProtoMember(1)] public string PlayerName { get; set; }
    }

    /// <summary>
    /// 【S2C，MsgId=8】房间状态推送（统一响应）：建房成功 / 加入成功 / 成员加入离开的变动广播。
    /// 客户端收到后应整体替换本地房间会话数据并刷新界面。</summary>
    [ProtoContract]
    public class S2C_RoomState
    {
        /// <summary>4 位房间码。</summary>
        [ProtoMember(1)] public string RoomCode { get; set; }

        /// <summary>当前客户端是否房主（决定“开始游戏”按钮是否可用）。</summary>
        [ProtoMember(2)] public bool YouAreHost { get; set; }

        /// <summary>全员列表（按加入顺序，含自己；IsYou 标记自己）。</summary>
        [ProtoMember(3)] public List<MemberEntry> Members { get; set; }
    }

    /// <summary>【C2S，MsgId=9】请求房间列表快照（加入房间界面打开时调用）。</summary>
    [ProtoContract]
    public class C2S_ListRooms
    {
        // 无字段：纯请求，响应见 S2C_RoomList。
    }

    /// <summary>【S2C，MsgId=10】房间列表快照（仅未满员排序规则由服务器定，客户端原样展示）。</summary>
    [ProtoContract]
    public class S2C_RoomList
    {
        /// <summary>当前所有存活房间（含满员，客户端负责禁用满员条目的加入按钮）。</summary>
        [ProtoMember(1)] public List<RoomEntry> Rooms { get; set; }
    }

    /// <summary>
    /// 【C2S，MsgId=11】加入指定房间：成功响应见 S2C_RoomState（房间全员广播），
    /// 失败响应见 S2C_JoinRejected。</summary>
    [ProtoContract]
    public class C2S_JoinRoom
    {
        /// <summary>目标房间码（4 位）。</summary>
        [ProtoMember(1)] public string RoomCode { get; set; }

        /// <summary>加入玩家的昵称。</summary>
        [ProtoMember(2)] public string PlayerName { get; set; }
    }

    /// <summary>【S2C，MsgId=12】加入被拒：携带人类可读的原因（本地化交由客户端）。</summary>
    [ProtoContract]
    public class S2C_JoinRejected
    {
        /// <summary>拒绝原因（服务器原文，如“房间已满”/“房间不存在”；仅提示展示用）。</summary>
        [ProtoMember(1)] public string Reason { get; set; }
    }

    /// <summary>
    /// 【C2S，MsgId=13】离开当前房间（返回主菜单/关闭界面时发送）。
    /// 无专属响应：房内剩余成员会收到新的 S2C_RoomState 广播；
    /// 房主离开则房主转移给下一个成员；最后一人离开则房间销毁。</summary>
    [ProtoContract]
    public class C2S_LeaveRoom
    {
        // 无字段。
    }

    /// <summary>
    /// 【C2S，MsgId=14】房主请求开局：服务器校验（房主身份 + 人数 ≥ 2）后
    /// 向房间全员广播 S2C_GameStarted；校验失败无响应（防刷）。</summary>
    [ProtoContract]
    public class C2S_StartGame
    {
        // 无字段：房间上下文由会话号定位。
    }

    /// <summary>【C2S，MsgId=5】心跳：无字段（纯保活；服务器收任何包即刷新 LastSeen）。</summary>
    [ProtoContract]
    public class C2S_Ping
    {
        // 无字段：发送频率见 NetClient.PingInterval。
    }

    /// <summary>【S2C，MsgId=6】心跳应答：客户端 15s 未收到即判定断线并复位到主菜单。</summary>
    [ProtoContract]
    public class S2C_Pong
    {
        /// <summary>服务器当前时间（UtcNow.Ticks）：可用于粗略对时或时钟偏移估算。</summary>
        [ProtoMember(1)] public long ServerTime { get; set; }
    }

    /// <summary>
    /// 【嵌套类型，随 S2C_GameStarted 携带】开局槽位分配：conv → 槽位号（0-3）+ 昵称。
    /// 各端以槽位号标识对局内玩家（本地知道自己是哪个槽）。</summary>
    [ProtoContract]
    public class GameSlot
    {
        /// <summary>玩家槽位号（0-3，开局时按房间成员顺序分配）。</summary>
        [ProtoMember(1)] public int Slot { get; set; }

        /// <summary>该槽位的玩家昵称。</summary>
        [ProtoMember(2)] public string Name { get; set; }

        /// <summary>该槽位玩家的会话号（客户端比对 conv 得知“这是我”）。</summary>
        [ProtoMember(3)] public uint Conv { get; set; }
    }

    /// <summary>
    /// 【S2C，MsgId=15】开局广播：携带世界种子与槽位分配，各端用同一份输入流
    /// （服务器定序的 hop 广播）驱动确定性模拟，得到完全一致的对局。
    /// 死亡后该玩家进入幽灵观战（本地可继续 R 重开单机体验）。</summary>
    [ProtoContract]
    public class S2C_GameStarted
    {
        /// <summary>开局时刻的服务器时间（UtcNow.Ticks，对时参考，阶段 3 可能用到）。</summary>
        [ProtoMember(1)] public long ServerTime { get; set; }

        /// <summary>世界种子（车道序列/车辆/火车共用的确定性随机源种子）。</summary>
        [ProtoMember(2)] public int Seed { get; set; }

        /// <summary>槽位分配（全员；客户端按 conv 定位自己的槽位号）。</summary>
        [ProtoMember(3)] public List<GameSlot> Slots { get; set; }
    }

    /// <summary>
    /// 【C2S，MsgId=16】本地玩家跳格上报。本地已乐观执行（格子跳幂等），
    /// 服务器只做定序（赋 ServerSeq）后广播，不校验游戏规则。</summary>
    [ProtoContract]
    public class C2S_Hop
    {
        /// <summary>方向：0=前(+Z) 1=后(-Z) 2=左(-X) 3=右(+X)。</summary>
        [ProtoMember(1)] public int Direction { get; set; }

        /// <summary>本地序号（单调递增；供调试对账，执行以服务器序为准）。</summary>
        [ProtoMember(2)] public int ClientSeq { get; set; }
    }

    /// <summary>
    /// 【S2C，MsgId=17】跳格广播（服务器定序）：各端按 ServerSeq 升序执行同一跳格流；
    /// 该消息也会发回发起者（发起端比对 seq 后跳过本地已乐观执行的那一跳）。</summary>
    [ProtoContract]
    public class S2C_HopBroadcast
    {
        /// <summary>执行的玩家槽位号（0-3）。</summary>
        [ProtoMember(1)] public int Slot { get; set; }

        /// <summary>方向：0=前(+Z) 1=后(-Z) 2=左(-X) 3=右(+X)。</summary>
        [ProtoMember(2)] public int Direction { get; set; }

        /// <summary>服务器定序号（房间对局内单调递增）。</summary>
        [ProtoMember(3)] public int ServerSeq { get; set; }

        /// <summary>发起者的本地序号（发起端回声比对用）。</summary>
        [ProtoMember(4)] public int ClientSeq { get; set; }
    }

    /// <summary>
    /// 【C2S，MsgId=18】本端玩家死亡上报：确定性世界下各端判定结果一致，
    /// 上报仅作触发器（让各端播放该玩家的死亡动画并转幽灵观战）。</summary>
    [ProtoContract]
    public class C2S_Died
    {
        /// <summary>死亡原因（本地化 key 原文，如 LKey.DeathCar；各端用相同 key 渲染文案）。</summary>
        [ProtoMember(1)] public string Reason { get; set; }

        /// <summary>死亡表现：0=Squash（压扁）1=Drown（落水沉没）2=Fade（漂移淡出）。</summary>
        [ProtoMember(2)] public int DeathKind { get; set; }
    }

    /// <summary>【S2C，MsgId=19】死亡广播：各端播放该槽位玩家的死亡动画，随后该玩家进入幽灵观战。</summary>
    [ProtoContract]
    public class S2C_PlayerDied
    {
        /// <summary>死亡的玩家槽位号（0-3）。</summary>
        [ProtoMember(1)] public int Slot { get; set; }

        /// <summary>死亡原因（本地化 key 原文；渲染文案用）。</summary>
        [ProtoMember(2)] public string Reason { get; set; }

        /// <summary>死亡表现：0=Squash（压扁）1=Drown（落水沉没）2=Fade（漂移淡出）。</summary>
        [ProtoMember(3)] public int DeathKind { get; set; }
    }
}
