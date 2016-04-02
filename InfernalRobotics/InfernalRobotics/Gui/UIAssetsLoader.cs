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
        private AssetBundle IRAssetBundle;

        internal static GameObject controlWindowPrefab;
        internal static GameObject controlWindowGroupLinePrefab;
        internal static GameObject controlWindowServoLinePrefab;

        internal static List<Texture2D> iconAssets;

        public static bool controlWindowPrefabReady = false;
        public static bool controlWindowGroupLinePrefabReady = false;
        public static bool controlWindowServoLinePrefabReady = false;

        public IEnumerator LoadBundle(string location)
        {
            while (!Caching.ready)
                yield return null;
            using (WWW www = WWW.LoadFromCacheOrDownload(location, 1))
            {
                yield return www;
                IRAssetBundle = www.assetBundle;
                var prefabs = IRAssetBundle.LoadAllAssets<GameObject>();
                for (int i=0; i< prefabs.Length; i++)
                {
                    if(prefabs[i].name == "FlightServoControlWindowPrefab")
                    {
                        controlWindowPrefab = prefabs[i] as GameObject;
                        controlWindowPrefabReady = true;
                        Logger.Log("Successfully loaded control window prefab");
                    }
                    if (prefabs[i].name == "FlightControllerServoGroupLine")
                    {
                        controlWindowGroupLinePrefab = prefabs[i] as GameObject;
                        controlWindowGroupLinePrefabReady = true;
                        Logger.Log("Successfully loaded control window Group prefab");
                    }

                    if (prefabs[i].name == "FlightControllerServoLine")
                    {
                        controlWindowServoLinePrefab = prefabs[i] as GameObject;
                        controlWindowServoLinePrefabReady = true;
                        Logger.Log("Successfully loaded control window Servo prefab");
                    }
                }
                var icons = IRAssetBundle.LoadAllAssets<Texture2D>();

                iconAssets = new List<Texture2D>();

                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] != null)
                    {
                        iconAssets.Add(icons[i]);
                        Logger.Log("Successfully loaded texture " + icons[i].name);
                    }
                    
                }
                
            }
        }
        
        public void Start()
        {
            var assemblyFile = Assembly.GetExecutingAssembly().Location;
            var bundlePath = "file://" + assemblyFile.Replace(new FileInfo(assemblyFile).Name, "").Replace("\\","/") + "../AssetBundles/";

            Logger.Log("Loading bundles from BundlePath: " + bundlePath);

            //need to clean cache
            Caching.CleanCache();

            StartCoroutine(LoadBundle(bundlePath + "ir_ui_objects.ksp"));
        }

    }
}

