using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xiangsoft.Game.CrossRoad
{
    public enum GameLanguage
    {
        SimplifiedChinese,
        TraditionalChinese,
        English,
        French,
        German,
        Spanish,
        Russian,
        Japanese,
        Korean,
        Vietnamese,
    }

    /// <summary>Text keys — the single vocabulary of the game. UI shows L(key), never literals.</summary>
    public static class LKey
    {
        public const string Title = "title";
        public const string Subtitle = "subtitle";
        public const string Score = "score";
        public const string Hint = "hint";
        public const string YouDied = "you_died";
        public const string PressRestart = "press_restart";

        public const string MenuHost = "menu_host";
        public const string MenuJoin = "menu_join";
        public const string MenuSettings = "menu_settings";
        public const string MenuQuit = "menu_quit";

        // ---- online connect panel (stage 4: real-client entry) ----
        public const string OnlineTitle = "online_title";
        public const string OnlineAddress = "online_address";
        public const string OnlineName = "online_name";
        public const string OnlineConnect = "online_connect";
        public const string OnlineOffline = "online_offline";
        public const string OnlineConnecting = "online_connecting";
        public const string OnlineConnected = "online_connected";

        // ---- auto-connect UX (stage 4) ----
        public const string OnlineWaitServer = "online_wait_server";       // 服务器尚未连接成功请稍等…
        public const string OnlineConnectingServer = "online_connecting_server";   // 正在连接服务器…
        public const string OnlineConnectedAs = "online_connected_as";    // 已连接 · {0}（服务器分配名）

        public const string HostTitle = "host_title";
        public const string HostDesc = "host_desc";
        public const string HostStatus = "host_status";
        public const string StartGame = "start_game";
        public const string Back = "back";

        public const string JoinTitle = "join_title";
        public const string JoinDesc = "join_desc";
        public const string JoinPlaceholder = "join_placeholder";
        public const string JoinButton = "join_button";
        public const string Connecting = "connecting";
        public const string NoAddress = "no_address";
        public const string ConnectFailed = "connect_failed";
        public const string JoinRefresh = "join_refresh";
        public const string JoinJoining = "join_joining";
        public const string JoinEmpty = "join_empty";

        public const string SettingsTitle = "settings_title";
        public const string SettingsLanguage = "settings_language";

        public const string LobbyTitle = "lobby_title";
        public const string LobbyRoomCode = "lobby_room_code";
        public const string LobbyCount = "lobby_count";
        public const string LobbyMinPlayers = "lobby_min_players";
        public const string LobbyWaitingSlot = "lobby_waiting_slot";
        public const string LobbyHostTag = "lobby_host_tag";
        public const string LobbyYou = "lobby_you";
        public const string LobbyGuestWait = "lobby_guest_wait";
        public const string LobbyFull = "lobby_full";

        public const string DeathCar = "death_car";
        public const string DeathTrain = "death_train";
        public const string DeathDrown = "death_drown";
        public const string DeathDrift = "death_drift";
        public const string DeathBehind = "death_behind";
    }

    /// <summary>
    /// Runtime localization: one table per language, switchable at any moment.
    /// LanguageChanged fires so every screen can re-render its texts in place.
    /// The chosen language persists via PlayerPrefs.
    /// </summary>
    public static class LocalizationService
    {
        private const string PrefsKey = "CrossRoad.Language";

        public static GameLanguage Current { get; private set; } = GameLanguage.SimplifiedChinese;

        /// <summary>Raised after the language (and table) switched — re-render your texts.</summary>
        public static event Action LanguageChanged;

        private static Dictionary<string, string> table;

        /// <summary>Loads the persisted language (default: Simplified Chinese).</summary>
        public static void Init()
        {
            GameLanguage lang = Current;
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out GameLanguage parsed))
                lang = parsed;
            LoadTable(lang, save: false);
        }

        public static void SetLanguage(GameLanguage lang)
        {
            if (lang == Current && table != null) return;
            LoadTable(lang, save: true);
            LanguageChanged?.Invoke();
        }

        /// <summary>Translate a key; unknown keys fall back to the key itself (visible in QA).</summary>
        public static string L(string key)
        {
            if (table != null && table.TryGetValue(key, out var value)) return value;
            return key;
        }

        /// <summary>Translate and format: "connecting {0}" style.</summary>
        public static string L(string key, object arg) => string.Format(L(key), arg);

        /// <summary>Translate and format: "players {0}/{1}" style.</summary>
        public static string L(string key, object arg0, object arg1) => string.Format(L(key), arg0, arg1);

        private static void LoadTable(GameLanguage lang, bool save)
        {
            Current = lang;
            table = TranslationTables.Get(lang);
            if (save)
            {
                PlayerPrefs.SetString(PrefsKey, lang.ToString());
                PlayerPrefs.Save();
            }
        }

        /// <summary>Native display name for the language picker ("简体中文", "English", …).</summary>
        public static string DisplayName(GameLanguage lang) => lang switch
        {
            GameLanguage.SimplifiedChinese => "简体中文",
            GameLanguage.TraditionalChinese => "繁體中文",
            GameLanguage.English => "English",
            GameLanguage.French => "Français",
            GameLanguage.German => "Deutsch",
            GameLanguage.Spanish => "Español",
            GameLanguage.Russian => "Русский",
            GameLanguage.Japanese => "日本語",
            GameLanguage.Korean => "한국어",
            _ => "Tiếng Việt",
        };
    }
}
