using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayersOnlineValheim
{
    [BepInPlugin("marmotic.playersonline", "Players Online", "0.1.0.0")]
    public class PlayersOnlinePlugin : BaseUnityPlugin
    {
        private static PlayersOnlinePlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<Color> fontColor;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<string> fontName;

        public static Text text;
        private static Font font;
        private static GUIStyle style;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            fontName = Config.Bind<string>("General", "FontName", "AveriaSerifLibre-Bold", "Name of font");
            fontSize = Config.Bind<int>("General", "FontSize", 24, "Size of font");
            fontColor = Config.Bind<Color>("General", "FontColor", Color.white, "Font color");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void OnGUI()
        {
            if (modEnabled.Value && Player.m_localPlayer && EnvMan.instance)
            {
                float alpha = 1f;
                string playerListString = GetPlayerListString();
                style.normal.textColor = new Color(fontColor.Value.r, fontColor.Value.g, fontColor.Value.b, fontColor.Value.a * alpha);
                GUI.Label(new Rect(new Vector2(Screen.width-200, (Screen.height/2 - 100)), new Vector2(200, 200)), playerListString, style);
            }
        }

        [HarmonyPatch(typeof(ZNet))]
        public class HookZNet
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(ZNet), "GetOtherPublicPlayers", new Type[] { typeof(List<ZNet.PlayerInfo>) })]
            public static void GetOtherPublicPlayers(object instance, List<ZNet.PlayerInfo> playerList) => throw new NotImplementedException();
        }

        private static void ApplyConfig()
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font font in fonts)
            {
                if (font.name == fontName.Value)
                {
                    PlayersOnlinePlugin.font = font;
                    break;
                }
            }
            style = new GUIStyle
            {
                richText = true,
                fontSize = fontSize.Value,
                alignment = TextAnchor.MiddleLeft,
                font = font
            };
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                ApplyConfig();
            }
        }

        private string GetPlayerListString()
        {
            List<ZNet.PlayerInfo> ___m_tempPlayerInfo = new List<ZNet.PlayerInfo>();
            HookZNet.GetOtherPublicPlayers(ZNet.instance, ___m_tempPlayerInfo);

            string playerList = "";

            if (___m_tempPlayerInfo.Count > 0)
            {
                foreach (ZNet.PlayerInfo m_Player in ___m_tempPlayerInfo)
                {
                    playerList += m_Player.m_name + " ";
                    float distance = Vector3.Distance(m_Player.m_position, Player.m_localPlayer.transform.position);
                    playerList += String.Format("{0:0.0}", distance) + "m";
                    playerList += "\n";
                }
            }
            return playerList;
        }
    }
}
