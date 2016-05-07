using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace InfernalRobotics.Utility
{
    public class TextureLoader
    {
        private static bool isReady;
        internal static Texture2D EditorBackgroundText { get; private set; }
        internal static Texture2D StopButtonIcon { get; private set; }
        internal static Texture2D CogButtonIcon { get; private set; }

        internal static Texture2D ExpandIcon { get; private set; }
        internal static Texture2D CollapseIcon { get; private set; }
        internal static Texture2D LeftIcon { get; private set; }
        internal static Texture2D RightIcon { get; private set; }
        internal static Texture2D LeftToggleIcon { get; private set; }
        internal static Texture2D RightToggleIcon { get; private set; }
        internal static Texture2D RevertIcon { get; private set; }
        internal static Texture2D AutoRevertIcon { get; private set; }
        internal static Texture2D DownIcon { get; private set; }
        internal static Texture2D UpIcon { get; private set; }
        internal static Texture2D TrashIcon { get; private set; }
        internal static Texture2D PresetsIcon { get; private set; }
        internal static Texture2D PresetModeIcon { get; private set; }
        internal static Texture2D LockedIcon { get; private set; }
        internal static Texture2D UnlockedIcon { get; private set; }
        internal static Texture2D InvertedIcon { get; private set; }
        internal static Texture2D NoninvertedIcon { get; private set; }
        internal static Texture2D NextIcon { get; private set; }
        internal static Texture2D PrevIcon { get; private set; }
        internal static Texture2D CloneIcon { get; private set; }

        protected static TextureLoader LoaderInstance;

        public static TextureLoader Instance
        {
            get { return LoaderInstance; }
        }

        public static bool Ready { get { return isReady; } }

        /// <summary>
        ///     Load the textures from files to memory
        /// </summary>
        public static void InitTextures()
        {
            if (!isReady)
            {
                EditorBackgroundText = CreateTextureFromColor(1, 1, new Color32(81, 86, 94, 255));

                StopButtonIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                LoadImageFromFile(StopButtonIcon, "icon_stop.png");

                CogButtonIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(CogButtonIcon, "icon_cog.png");

                ExpandIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(ExpandIcon, "expand.png");

                CollapseIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(CollapseIcon, "collapse.png");

                LeftIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(LeftIcon, "left.png");

                RightIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(RightIcon, "right.png");

                LeftToggleIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(LeftToggleIcon, "left_toggle.png");

                RightToggleIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(RightToggleIcon, "right_toggle.png");

                RevertIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(RevertIcon, "revert.png");

                AutoRevertIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(AutoRevertIcon, "auto_revert.png");

                DownIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(DownIcon, "down.png");

                UpIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(UpIcon, "up.png");

                TrashIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(TrashIcon, "trash.png");

                PresetsIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(PresetsIcon, "presets.png");

                PresetModeIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(PresetModeIcon, "presetmode.png");

                LockedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(LockedIcon, "locked.png");

                UnlockedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(UnlockedIcon, "unlocked.png");

                InvertedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(InvertedIcon, "inverted.png");

                NoninvertedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(NoninvertedIcon, "noninverted.png");

                NextIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(NextIcon, "next.png");

                PrevIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(PrevIcon, "prev.png");

                CloneIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(CloneIcon, "clone.png");

                isReady = true;
            }
        }

        /// <summary>
        ///     Use System.IO.File to read a file into a texture in RAM. Path is relative to the DLL
        ///     Do it this way so the images are not affected by compression artifacts or Texture quality settings
        /// </summary>
        /// <param name="tex">Texture to load</param>
        /// <param name="fileName">Filename of the image in side the Textures folder</param>
        /// <returns></returns>
        internal static bool LoadImageFromFile(Texture2D tex, string fileName)
        {
            //Set the Path variables
            string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pathPluginTextures = string.Format("{0}/../Textures", pluginPath);
            bool blnReturn = false;
            try
            {
                //File Exists check
                if (File.Exists(string.Format("{0}/{1}", pathPluginTextures, fileName)))
                {
                    try
                    {
                        //Logger.Log(string.Format("[GUI] Loading: {0}/{1}", pathPluginTextures, fileName));
                        tex.LoadImage(File.ReadAllBytes(string.Format("{0}/{1}", pathPluginTextures, fileName)));
                        blnReturn = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(string.Format("[GUI] Failed to load the texture:{0} ({1})",
                            string.Format("{0}/{1}", pathPluginTextures, fileName), ex.Message));
                    }
                }
                else
                {
                    Logger.Log(string.Format("[GUI] Cannot find texture to load:{0}",
                        string.Format("{0}/{1}", pathPluginTextures, fileName)));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("[GUI] Failed to load (are you missing a file):{0} ({1})",
                    string.Format("{0}/{1}", pathPluginTextures, fileName), ex.Message));
            }
            return blnReturn;
        }

        /// <summary>
        /// Creates the solid texture of given size and Color.
        /// </summary>
        /// <returns>The texture from color.</returns>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="col">Color</param>
        private static Texture2D CreateTextureFromColor(int width, int height, Color col)
        {
            var pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

    }
}
