using UnityEngine;

namespace InfernalRobotics
{
    //18.3
    public class MuUtils
    {
        private static GUISkin _defaultSkin;
        public static GUISkin DefaultSkin
        {
            get
            {
                if (_defaultSkin == null)
                {
                    _defaultSkin = AssetBase.GetGUISkin("KSP window 2");
                }
                return _defaultSkin;
            }
        }

        public static void SelectDefaultSkin(string skin)
        {
            _defaultSkin = AssetBase.GetGUISkin(skin);
        }
    }
}
