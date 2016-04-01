using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using KSP.UI.Screens;
using KSPAssets;
using KSPAssets.Loaders;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class UIPrefabLoader : MonoBehaviour
    {
        internal static GameObject controlWindowPrefab;
        internal static GameObject controlWindowGroupLinePrefab;
        internal static GameObject controlWindowServoLinePrefab;

        internal static List<Texture2D> iconAssets;

        public static bool controlWindowPrefabReady = false;
        public static bool controlWindowGroupLinePrefabReady = false;
        public static bool controlWindowServoLinePrefabReady = false;

        bool once = false;

        public void Start()
        {
            
        }
            
        public void OnPrefabsLoaded(AssetLoader.Loader loader)
        {
            Logger.Log ("OnPrefabsLoaded was called");

            for (int i = 0; i < loader.definitions.Length; i++ )
            {

                Logger.Log ("Examining definition: " + loader.definitions[i].name + 
                     ", objects[i] = " + (loader.objects[i] ==null ? "null" : loader.objects[i].name));
                if(loader.definitions[i].name == "FlightServoControlWindowPrefab" && loader.objects[i] != null)
                {
                    controlWindowPrefab = loader.objects [i] as GameObject;
                    controlWindowPrefabReady = true;
                    Logger.Log ("Successfully loaded control window prefab");
                }
                if(loader.definitions[i].name == "FlightControllerServoGroupLine" && loader.objects[i] != null)
                {
                    controlWindowGroupLinePrefabReady = loader.objects [i] as GameObject;
                    controlWindowGroupLinePrefabReady = true;
                    Logger.Log ("Successfully loaded control window Group prefab");
                }

                if(loader.definitions[i].name == "FlightControllerServoLine" && loader.objects[i] != null)
                {
                    controlWindowServoLinePrefab = loader.objects [i] as GameObject;
                    controlWindowServoLinePrefabReady = true;
                    Logger.Log ("Successfully loaded control window Servo prefab");
                }
            }

        }

        public void Update()
        {
            if(!once)
            {
                if(AssetLoader.Ready)
                {
                    BundleDefinition bd = AssetLoader.GetBundleDefinition ("MagicSmokeIndustries/AssetBundles/ircontrolwindow");

                    if (bd != null)
                    {
                        Logger.Log ("Found IR UI Bundle Definition. Listing Assets");
                        foreach(var ad in bd.assets)
                        {
                            Logger.Log ("Found AssetDefinition:" + ad.name + ", type = " + ad.type);

                            if(ad.type == "GameObject")
                            {
                                AssetLoader.LoadAssets(OnPrefabsLoaded, ad);
                            }    
                        }
                    }

                    bd = AssetLoader.GetBundleDefinition ("MagicSmokeIndustries/AssetBundles/ircontrolwindowgroupline");

                    if (bd != null)
                    {
                        Logger.Log ("Found IR UI Bundle Definition. Listing Assets");
                        foreach(var ad in bd.assets)
                        {
                            Logger.Log ("Found AssetDefinition:" + ad.name + ", type = " + ad.type);

                            if(ad.type == "GameObject")
                            {
                                AssetLoader.LoadAssets(OnPrefabsLoaded, ad);
                            }    
                        }
                    }

                    bd = AssetLoader.GetBundleDefinition ("MagicSmokeIndustries/AssetBundles/ircontrolwindowservoline");

                    if (bd != null)
                    {
                        Logger.Log ("Found IR UI Bundle Definition. Listing Assets");
                        foreach(var ad in bd.assets)
                        {
                            Logger.Log ("Found AssetDefinition:" + ad.name + ", type = " + ad.type);

                            if(ad.type == "GameObject")
                            {
                                AssetLoader.LoadAssets(OnPrefabsLoaded, ad);
                            }    
                        }
                    }

                    once = true;
                }
            }
        }
    }
}

