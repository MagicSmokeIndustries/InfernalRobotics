using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace InfernalRobotics.Utility
{
    public class TextureLoader
    {
        private static bool isReady = false;
        private static bool useKSPAssets = false;
        internal static Texture2D editorBGTex;
        internal static Texture2D stopButtonIcon;
        internal static Texture2D cogButtonIcon;

        internal static Texture2D expandIcon;
        internal static Texture2D collapseIcon;
        internal static Texture2D leftIcon;
        internal static Texture2D rightIcon;
        internal static Texture2D leftToggleIcon;
        internal static Texture2D rightToggleIcon;
        internal static Texture2D revertIcon;
        internal static Texture2D autoRevertIcon;
        internal static Texture2D downIcon;
        internal static Texture2D upIcon;
        internal static Texture2D trashIcon;
        internal static Texture2D presetsIcon;
        internal static Texture2D presetModeIcon;
        internal static Texture2D lockedIcon;
        internal static Texture2D unlockedIcon;
        internal static Texture2D invertedIcon;
        internal static Texture2D noninvertedIcon;
        internal static Texture2D nextIcon;
        internal static Texture2D prevIcon;

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
                editorBGTex = CreateTextureFromColor(1, 1, new Color32(81, 86, 94, 255));

                stopButtonIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                LoadImageFromFile(stopButtonIcon, "icon_stop.png");

                cogButtonIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(cogButtonIcon, "icon_cog.png");

                expandIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(expandIcon, "expand.png");

                collapseIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(collapseIcon, "collapse.png");

                leftIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(leftIcon, "left.png");

                rightIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(rightIcon, "right.png");

                leftToggleIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(leftToggleIcon, "left_toggle.png");

                rightToggleIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(rightToggleIcon, "right_toggle.png");

                revertIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(revertIcon, "revert.png");

                autoRevertIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(autoRevertIcon, "auto_revert.png");

                downIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(downIcon, "down.png");

                upIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(upIcon, "up.png");

                trashIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(trashIcon, "trash.png");

                presetsIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(presetsIcon, "presets.png");

                presetModeIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(presetModeIcon, "presetmode.png");

                lockedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(lockedIcon, "locked.png");

                unlockedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(unlockedIcon, "unlocked.png");

                invertedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(invertedIcon, "inverted.png");

                noninvertedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(noninvertedIcon, "noninverted.png");

                nextIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(nextIcon, "next.png");

                prevIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                LoadImageFromFile(prevIcon, "prev.png");

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
                        Logger.Log(string.Format("[GUI] Loading: {0}/{1}", pathPluginTextures, fileName));
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
