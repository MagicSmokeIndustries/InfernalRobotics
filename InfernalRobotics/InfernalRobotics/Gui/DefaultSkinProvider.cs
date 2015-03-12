using UnityEngine;

namespace InfernalRobotics.Gui
{
    public class DefaultSkinProvider
    {
        private static GUISkin defaultSkin;

        public static GUISkin DefaultSkin
        {
            get { return defaultSkin ?? (defaultSkin = AssetBase.GetGUISkin("KSP window 2")); }
        }

        public static void SelectDefaultSkin(string skin)
        {
            defaultSkin = AssetBase.GetGUISkin(skin);
        }
    }
}