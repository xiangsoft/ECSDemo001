using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    /// <summary>One room as shown in the browser list (the server will provide these).</summary>
    public class RoomInfo
    {
        public string Code;
        public string HostName;
        public int PlayerCount;
    }

    /// <summary>A player inside the room lobby.</summary>
    public class RoomMember
    {
        public string Name;    // null = the local player (rendered as "you")
        public bool IsHost;
    }

    /// <summary>The room you are currently in — the lobby renders this. 4 players max.</summary>
    public class RoomSession
    {
        public const int MaxPlayers = 4;

        public string Code;
        public bool YouAreHost;
        public List<RoomMember> Members = new List<RoomMember>();
    }

    /// <summary>
    /// 房间数据源（UI 唯一入口）。双模式：
    ///   · 离线（默认）：本地假数据——单机玩法与离线测试全走这里；
    ///   · 联机（OnlineMode）：NetClient 的网络数据——三个方法返回“乐观 pending session”，
    ///     服务器推送到达后 ApplyRoomState 更新 CurrentSession 并触发 SessionChanged，
    ///     界面订阅该事件重渲染，UI 调用点零改动。
    /// </summary>
    public static class RoomCatalog
    {
        /// <summary>联机开关（NetClient 连接成功后置 true，销毁时复位）。</summary>
        public static bool OnlineMode;

        /// <summary>是否已进入对局（S2C_GameStarted 置位；用于 Back 时区分“在对局”与“在房间”）。</summary>
        public static bool InGame;

        /// <summary>当前房间会话（联机时为服务器推送的最新状态）。</summary>
        public static RoomSession CurrentSession;

        /// <summary>房间会话变化（建房/加入/成员变动推送）——RoomLobbyUi 订阅后实时刷新。</summary>
        public static event Action SessionChanged;

        /// <summary>房间列表到达（JoinRoomUi 打开时读 CachedRooms）。</summary>
        public static event Action RoomsChanged;

        /// <summary>加入被拒（原因文案）。</summary>
        public static event Action<string> JoinRejected;

        /// <summary>房间列表快照：联机返回 NetClient 缓存，离线返回本地假数据。</summary>
        public static List<RoomInfo> FetchRooms()
        {
            if (OnlineMode && NetClient.Instance != null)
            {
                NetClient.Instance.ListRooms();   // 发一次请求，下次打开列表时已是新数据
                return new List<RoomInfo>(NetClient.Instance.CachedRooms);
            }

            return new List<RoomInfo>
            {
                new RoomInfo { Code = "A7F2", HostName = "马路小王子", PlayerCount = 2 },
                new RoomInfo { Code = "B3K9", HostName = "ChickenRun", PlayerCount = 1 },
                new RoomInfo { Code = "C8D1", HostName = "小鸡快跑", PlayerCount = 3 },
                new RoomInfo { Code = "D5M7", HostName = "过马路大师", PlayerCount = 4 },   // full -> disabled
                new RoomInfo { Code = "E9X2", HostName = "FroggerFan", PlayerCount = 2 },
                new RoomInfo { Code = "F2H4", HostName = "呱呱", PlayerCount = 1 },
            };
        }

        /// <summary>建房：返回乐观 pending session（仅自己），服务器推送后 SessionChanged 刷新为真实房间码。</summary>
        public static RoomSession CreateAsHost()
        {
            if (OnlineMode && NetClient.Instance != null)
            {
                InGame = false;
                NetClient.Instance.CreateRoom();
                CurrentSession = new RoomSession
                {
                    Code = "....",   // 占位：等服务器分配的真实房间码
                    YouAreHost = true,
                    Members = { new RoomMember { Name = null, IsHost = true } },
                };
                return CurrentSession;
            }

            return new RoomSession
            {
                Code = RandomCode(),
                YouAreHost = true,
                Members = { new RoomMember { Name = null, IsHost = true } },
            };
        }

        /// <summary>加入房间：返回乐观 pending session（仅自己），服务器推送后刷新为全员真实状态。</summary>
        public static RoomSession JoinAsGuest(RoomInfo room)
        {
            if (OnlineMode && NetClient.Instance != null)
            {
                InGame = false;
                NetClient.Instance.JoinRoom(room.Code);
                CurrentSession = new RoomSession
                {
                    Code = room.Code,
                    YouAreHost = false,
                    Members = { new RoomMember { Name = null, IsHost = false } },
                };
                return CurrentSession;
            }

            var session = new RoomSession
            {
                Code = room.Code,
                YouAreHost = false,
            };
            session.Members.Add(new RoomMember { Name = room.HostName, IsHost = true });
            if (room.PlayerCount >= 3) session.Members.Add(new RoomMember { Name = "Clucky", IsHost = false });
            if (room.PlayerCount >= 4) session.Members.Add(new RoomMember { Name = "黄小鸡", IsHost = false });
            session.Members.Add(new RoomMember { Name = null, IsHost = false });   // you
            return session;
        }

        // ------------------------------ 网络 session 的 UI 操作转发 ------------------------------

        /// <summary>开局请求：联机走服务器校验+广播（S2C_GameStarted 到达时才 raise），离线直接 raise。</summary>
        public static void RequestStartGame(CrossRoadEvents events)
        {
            if (OnlineMode && NetClient.Instance != null)
            {
                NetClient.Instance.RequestStartGame();
                return;   // 本地不 raise；等全员广播统一触发（保证多端同时开局）
            }
            events.GameStarted.Raise();
        }

        /// <summary>离开房间（房间界面返回时调）：联机通知服务器，离线无操作。</summary>
        public static void LeaveOnline()
        {
            if (OnlineMode && NetClient.Instance != null)
            {
                NetClient.Instance.LeaveRoom();
                CurrentSession = null;
            }
        }

        // ------------------------------ NetClient 回调（网络数据落地） ------------------------------

        /// <summary>服务器房间状态推送落地：转模型 + 广播 SessionChanged。</summary>
        public static void ApplyRoomState(Xiangsoft.Game.Net.S2C_RoomState state)
        {
            var session = new RoomSession
            {
                Code = state.RoomCode,
                YouAreHost = state.YouAreHost,
            };
            foreach (var member in state.Members)
                session.Members.Add(new RoomMember { Name = member.IsYou ? null : member.Name, IsHost = member.IsHost });
            CurrentSession = session;
            SessionChanged?.Invoke();
        }

        /// <summary>房间列表到达通知（NetClient 缓存已更新）。</summary>
        public static void NotifyRoomsChanged() => RoomsChanged?.Invoke();

        /// <summary>加入被拒通知。</summary>
        public static void NotifyJoinRejected(string reason) => JoinRejected?.Invoke(reason);

        private static string RandomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new char[4];
            for (int i = 0; i < code.Length; i++)
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            return new string(code);
        }
    }
}
