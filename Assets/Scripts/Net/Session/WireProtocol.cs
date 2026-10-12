using System.IO;

namespace Xiangsoft.Game.Net
{
    // =====================================================================
    // 网络协议 · 消息号定义与帧编解码
    // ---------------------------------------------------------------------
    // · 帧格式（wire frame）：
    //       [msgId : 2 字节，大端] [protobuf payload : 变长]
    //     —— 无长度前缀：KCP 工作在消息模式，一次 Receive 即一完整帧，
    //        帧边界由 KCP 自身保证，接收端无需自行拆包。
    // · 本文件与 Shared/Messages.cs 一起由 Unity 客户端与 .NET 服务器
    //   共享同一份源码编译，消息号即协议契约，双端永不错位。
    // =====================================================================

    /// <summary>线上消息号。双端通过此共享枚举保持一致。</summary>
    public enum MsgId : ushort
    {
        // ---- 阶段 1：传输层验证（1-6） ----
        C2S_Hello = 1,      // 客户端 → 服务器：上报玩家名（连接建立后的第一笔业务消息）
        S2C_HelloAck = 2,   // 服务器 → 客户端：确认连接，回分配的会话号 + 服务器时间
        C2S_Echo = 3,       // 客户端 → 服务器：连通性探测（阶段 1 传输层验证用）
        S2C_EchoReply = 4,  // 服务器 → 客户端：Echo 应答（原样回显，用于测量 RTT）
        C2S_Ping = 5,       // 客户端 → 服务器：心跳保活（每 5s 一发；服务器据 LastSeen 判活，30s 无包判死清理）
        S2C_Pong = 6,       // 服务器 → 客户端：心跳应答（客户端 15s 未收到即判定断线并复位）

        // ---- 阶段 2：房间信令（7-15） ----
        C2S_CreateRoom = 7,     // 客户端 → 服务器：创建房间（携带玩家名）
        S2C_RoomState = 8,      // 服务器 → 客户端：房间状态推送（建房/加入成功/成员变动的统一响应）
        C2S_ListRooms = 9,      // 客户端 → 服务器：请求房间列表
        S2C_RoomList = 10,      // 服务器 → 客户端：房间列表快照
        C2S_JoinRoom = 11,      // 客户端 → 服务器：加入指定房间（携带房间码 + 玩家名）
        S2C_JoinRejected = 12,  // 服务器 → 客户端：加入被拒（房满/房间不存在等，携带原因）
        C2S_LeaveRoom = 13,     // 客户端 → 服务器：离开当前房间
        C2S_StartGame = 14,     // 客户端 → 服务器：房主请求开局（服务器校验人数后广播）
        S2C_GameStarted = 15,   // 服务器 → 客户端：开局广播（含种子与玩家槽位，驱动各端确定性模拟）

        // ---- 阶段 3：对局内同步（16-19） ----
        C2S_Hop = 16,           // 客户端 → 服务器：本地玩家跳格（方向 + 本地序号）
        S2C_HopBroadcast = 17,  // 服务器 → 客户端：跳格广播（服务器定序，全员同序执行）
        C2S_Died = 18,          // 客户端 → 服务器：本端玩家死亡上报（确定性世界下各端判定一致，上报仅作通知）
        S2C_PlayerDied = 19,    // 服务器 → 客户端：死亡广播（触发各端播放该玩家的死亡动画，其后进入幽灵观战）

        // ------------------ 编号维护规则（新增消息必读） ------------------
        // · 只增不改不复用：已启用的编号不可改派给别的消息；
        // · C2S 与 S2C 成对的消息建议编号相邻，便于阅读与对照；
        // · 新阶段从下一个十位数段开始（阶段 3 游戏内同步从 16 起）。
        // -------------------------------------------------------------------
    }

    /// <summary>
    /// 帧编解码：[msgId:2B 大端][protobuf payload]。KCP 消息模式保留帧边界，无需长度前缀。
    /// </summary>
    public static class Wire
    {
        /// <summary>把一条消息编码为完整帧：2 字节消息号（大端）+ protobuf 序列化体。</summary>
        public static byte[] Encode(MsgId id, object message)
        {
            using var ms = new MemoryStream();
            ms.WriteByte((byte)((ushort)id >> 8));
            ms.WriteByte((byte)((ushort)id & 0xff));
            ProtoBuf.Serializer.Serialize(ms, message);
            return ms.ToArray();
        }

        /// <summary>拆帧：取出消息号与裸 payload；payload 仍需按消息类型反序列化（见 Decode&lt;T&gt;）。</summary>
        public static bool TryDecode(byte[] data, int length, out MsgId id, out byte[] payload)
        {
            id = 0;
            payload = null;
            if (length < 2) return false;
            id = (MsgId)(((data[0] << 8) | data[1]) & 0xffff);
            payload = new byte[length - 2];
            for (int i = 0; i < payload.Length; i++) payload[i] = data[i + 2];
            return true;
        }

        /// <summary>把 TryDecode 得到的 payload 反序列化为指定消息类型。</summary>
        public static T Decode<T>(byte[] payload)
        {
            using var ms = new MemoryStream(payload);
            return ProtoBuf.Serializer.Deserialize<T>(ms);
        }
    }
}
