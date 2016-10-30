using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class UIAssetsLoader : MonoBehaviour
    {
        internal static AssetBundle IRAssetBundle;

        internal static GameObject controlWindowPrefab;
        internal static GameObject controlWindowGroupLinePrefab;
        internal static GameObject controlWindowServoLinePrefab;

        internal static GameObject uiSettingsWindowPrefab;

        internal static GameObject editorWindowPrefab;
        internal static GameObject editorWindowGroupLinePrefab;
        internal static GameObject editorWindowServoLinePrefab;

        internal static GameObject presetWindowPrefab;
        internal static GameObject presetLinePrefab;

        internal static GameObject basicTooltipPrefab;

        internal static List<Texture2D> iconAssets;
        internal static List<UnityEngine.Sprite> spriteAssets;
        
        public static bool allPrefabsReady = false;

        public IEnumerator LoadBundle(string location)
        {
            while (!Caching.ready)
                yield return null;
            using (WWW www = WWW.LoadFromCacheOrDownload(location, 1))
            {
                yield return www;
                IRAssetBundle = www.assetBundle;

                LoadBundleAssets();

                //IRAssetBundle.Unload(false);
            }
        }
        
        private void LoadBundleAssets()
        {
            var prefabs = IRAssetBundle.LoadAllAssets<GameObject>();
            int prefabCounter = 0;

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i].name == "FlightWindowPrefab")
                {
                    controlWindowPrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded control window prefab", Logger.Level.Debug);
                }
                if (prefabs[i].name == "FlightWindowGroupLinePrefab")
                {
                    controlWindowGroupLinePrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded control window Group prefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "FlightWindowServoLinePrefab")
                {
                    controlWindowServoLinePrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded control window Servo prefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "UISettingsWindowPrefab")
                {
                    uiSettingsWindowPrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded UI settings window prefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "EditorWindowPrefab")
                {
                    editorWindowPrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded EditorWindowPrefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "EditorGroupLinePrefab")
                {
                    editorWindowGroupLinePrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded EditorGroupLinePrefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "EditorServoLinePrefab")
                {
                    editorWindowServoLinePrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded EditorServoLinePrefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "PresetsWindowPrefab")
                {
                    presetWindowPrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded PresetsWindowPrefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "PresetLinePrefab")
                {
                    presetLinePrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded PresetLinePrefab", Logger.Level.Debug);
                }

                if (prefabs[i].name == "BasicTooltipPrefab")
                {
                    basicTooltipPrefab = prefabs[i] as GameObject;
                    prefabCounter++;
                    Logger.Log("Successfully loaded BasicTooltipPrefab", Logger.Level.Debug);
                }
            }

            allPrefabsReady = (prefabCounter > 8);

            spriteAssets = new List<UnityEngine.Sprite>();
            var sprites = IRAssetBundle.LoadAllAssets<UnityEngine.Sprite>();

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    spriteAssets.Add(sprites[i]);
                    Logger.Log("Successfully loaded Sprite " + sprites[i].name, Logger.Level.Debug);
                }
            }

            iconAssets = new List<Texture2D>();
            var icons = IRAssetBundle.LoadAllAssets<Texture2D>();

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null)
                {
                    iconAssets.Add(icons[i]);
                    Logger.Log("Successfully loaded texture " + icons[i].name, Logger.Level.Debug);
                }

            }

        }

        public void Start()
        {
            if (allPrefabsReady)
                return;

            var assemblyFile = Assembly.GetExecutingAssembly().Location;
            var bundlePath = "file://" + assemblyFile.Replace(new FileInfo(assemblyFile).Name, "").Replace("\\","/") + "../AssetBundles/";
            //var filePath = assemblyFile.Replace(new FileInfo(assemblyFile).Name, "") + "../AssetBundles/";

            Logger.Log("Loading bundles from BundlePath: " + bundlePath, Logger.Level.Debug);

            //need to clean cache
            Caching.CleanCache();

            StartCoroutine(LoadBundle(bundlePath + "ir_ui_objects.bundle"));
            
            /*if(IRAssetBundle==null)
            {
                Logger.Log("Loading bundles from filePath: " + filePath, Logger.Level.Debug);
                LoadBundleFromDisk(filePath + "ir_ui_objects.ksp"); //bundle must be uncompressed for this function to work
            }
            */
        }

        public void OnDestroy()
        {
            /*if(IRAssetBundle)
            {
                Logger.Log("Unloading bundle", Logger.Level.Debug);
                IRAssetBundle.Unload(false);
            }*/
        }

    }
}

