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
        private static readonly bool isDebug = true;

        private static PlayersOnlinePlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> showList;
        public static ConfigEntry<string> playerListLocationString;
        public static ConfigEntry<float> playerListNameWidth;
        public static ConfigEntry<float> playerListDistanceWidth;

        public static ConfigEntry<string> toggleListKey;

        public static ConfigEntry<Color> nameFontColor;
        public static ConfigEntry<Color> distanceFontColor;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<string> fontName;

        public static Text text;
        private static Font font;
        private static GUIStyle style;
        private static Vector2 playerListPosition;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            showList = Config.Bind<bool>("General", "ShowList", true, "Show the list");
            playerListLocationString = Config.Bind<string>("General", "PlayerListLocationString", "85%,30%", "Location on the screen to show the list (x,y) or (x%,y%)");
            playerListNameWidth = Config.Bind<float>("General", "PlayerListNameWidth", 150, "Width of the player name part of list");
            playerListDistanceWidth = Config.Bind<float>("General", "PlayerListDistanceWidth", 120, "Width of the player distance part of list");

            toggleListKey = Config.Bind<string>("General", "ShowListKey", "f7", "Key used to toggle the list display. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");

            fontName = Config.Bind<string>("General", "FontName", "AveriaSerifLibre-Bold", "Name of font");
            fontSize = Config.Bind<int>("General", "FontSize", 24, "Size of font");
            nameFontColor = Config.Bind<Color>("General", "NamefontColor", Color.white, "Name font color");
            distanceFontColor = Config.Bind<Color>("General", "DistancefontColor", Color.gray, "Distance font color");

            Config.Save();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (!modEnabled.Value || !PressedToggleKey())
                return;

            bool show = showList.Value;
            showList.Value = !show;
            Config.Save();
        }

        private void OnGUI()
        {
            if (modEnabled.Value && showList.Value && Player.m_localPlayer && EnvMan.instance)
            {
                float alpha = 1f;

                List<ZNet.PlayerInfo> ___m_tempPlayerInfo = new List<ZNet.PlayerInfo>();

                if (!isDebug)
                {
                    HookZNet.GetOtherPublicPlayers(ZNet.instance, ___m_tempPlayerInfo);
                } else
                {
                    for (int j = 0; j < 10; j++)
                    {
                        ZNet.PlayerInfo player = new ZNet.PlayerInfo();
                        player.m_name = "PLAYER" + j;
                        player.m_position = new Vector3(j * 100, j * 200);
                        ___m_tempPlayerInfo.Add(player);
                    }
                }

                if (___m_tempPlayerInfo.Count > 0)
                {
                    int i = 0;
                    foreach (ZNet.PlayerInfo m_Player in ___m_tempPlayerInfo)
                    {
                        // add name label
                        Vector2 nameLabelPosition = new Vector2(playerListPosition.x, playerListPosition.y + fontSize.Value * i);
                        style.normal.textColor = new Color(nameFontColor.Value.r, nameFontColor.Value.g, nameFontColor.Value.b, nameFontColor.Value.a * alpha);
                        GUI.Label(new Rect(nameLabelPosition, new Vector2(playerListNameWidth.Value, fontSize.Value)), m_Player.m_name, style);

                        // add distance label
                        Vector2 distanceLabelPosition = new Vector2(playerListPosition.x + playerListNameWidth.Value, playerListPosition.y + fontSize.Value * i);
                        style.normal.textColor = new Color(distanceFontColor.Value.r, distanceFontColor.Value.g, distanceFontColor.Value.b, distanceFontColor.Value.a * alpha);
                        float distance = Vector3.Distance(m_Player.m_position, Player.m_localPlayer.transform.position);
                        string distanceString = String.Format("{0:0.0}", distance) + "m";
                        GUI.Label(new Rect(distanceLabelPosition, new Vector2(playerListDistanceWidth.Value, fontSize.Value)), distanceString, style);

                        i++;
                    }
                }
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
            string[] split = playerListLocationString.Value.Split(',');
            playerListPosition = new Vector2(split[0].Trim().EndsWith("%") ? (float.Parse(split[0].Trim().Substring(0, split[0].Trim().Length - 1)) / 100f) * Screen.width : float.Parse(split[0].Trim()), split[1].Trim().EndsWith("%") ? (float.Parse(split[1].Trim().Substring(0, split[1].Trim().Length - 1)) / 100f) * Screen.height : float.Parse(split[1].Trim()));

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
                alignment = TextAnchor.UpperRight,
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
        private static bool CheckKeyHeld(string value)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return true;
            }
        }
        private bool PressedToggleKey()
        {
            try
            {
                return Input.GetKeyDown(toggleListKey.Value.ToLower()) && CheckKeyHeld(toggleListKey.Value);
            }
            catch
            {
                return false;
            }
        }
    }
}
