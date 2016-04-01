using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class UIPrefabLoader : MonoBehaviour
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
            using (WWW www = WWW.LoadFromCacheOrDownload(location, 0))
            {
                yield return www;
                IRAssetBundle = www.assetBundle;
                var t = IRAssetBundle.LoadAllAssets<GameObject>();
                for (int i=0; i< t.Length; i++)
                {
                    if(t[i].name == "FlightServoControlWindowPrefab")
                    {
                        controlWindowPrefab = t[i] as GameObject;
                        controlWindowPrefabReady = true;
                        Logger.Log("Successfully loaded control window prefab");
                    }
                    if (t[i].name == "FlightControllerServoGroupLine")
                    {
                        controlWindowGroupLinePrefab = t[i] as GameObject;
                        controlWindowGroupLinePrefabReady = true;
                        Logger.Log("Successfully loaded control window Group prefab");
                    }

                    if (t[i].name == "FlightControllerServoLine")
                    {
                        controlWindowServoLinePrefab = t[i] as GameObject;
                        controlWindowServoLinePrefabReady = true;
                        Logger.Log("Successfully loaded control window Servo prefab");
                    }
                }
            }
        }
        
        public void Start()
        {
            var assemblyFile = Assembly.GetExecutingAssembly().Location;
            var bundlePath = "file://" + assemblyFile.Replace(new FileInfo(assemblyFile).Name, "").Replace("\\","/") + "../AssetBundles/";

            Logger.Log("Loading bundles from BundlePath: " + bundlePath);

            StartCoroutine(LoadBundle(bundlePath + "ir_ui_objects.ksp"));
        }

    }
}

