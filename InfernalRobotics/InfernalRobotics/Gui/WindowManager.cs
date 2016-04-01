using InfernalRobotics.Command;
using InfernalRobotics.Control;
using InfernalRobotics.Utility;
using KSP.IO;
using KSP.UI.Screens;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class IRFlightWindowManager : WindowManager
    {
        public override string AddonName { get { return this.name; } }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class IREditorWindowManager : WindowManager
    {
        public override string AddonName { get { return this.name; } }
    }

    public class WindowManager : MonoBehaviour
    {
        public virtual String AddonName { get; set; }

        private static GameObject _controlWindow;
        private static GameObject _editorWindow;

        private static List<GameObject> _servoGroupLines;
        private static List<GameObject> _servoLines;

        private static bool useElectricCharge;
        private static bool allowServoFlip;

        private static WindowManager _instance;
        private static bool guiSetupDone;
        private ApplicationLauncherButton appLauncherButton;
        private static Texture2D appLauncherButtonTexture;

        private bool guiGroupEditorEnabled;
        private bool guiPresetsEnabled;
        private IServo associatedServo;
        private bool guiPresetMode;
        private bool guiHidden;

        private static int editorWindowWidth = 400;
        private static int controlWindowWidth = 360;

        public bool GUIEnabled { get; set; }

        public static WindowManager Instance
        {
            get { return _instance; }
        }

        static WindowManager()
        {
            useElectricCharge = true;
            guiSetupDone = false;
            appLauncherButtonTexture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            TextureLoader.LoadImageFromFile (appLauncherButtonTexture, "icon_button.png");

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        }

        private void Awake()
        {
            
            LoadConfigXml();

            Logger.Log("[NewGUI] awake, Mode: " + AddonName);
            GUIEnabled = true; //for testing
            guiGroupEditorEnabled = false;

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT || scene == GameScenes.EDITOR)
            {
                _instance = this;

                _servoGroupLines = new List<GameObject> ();
                _servoLines = new List<GameObject> ();
            }
            else
            {
                _instance = null;
                //actually we don't need to go further if it's not flight or editor
                return;
            }
            //GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);

            //GameEvents.onGUIApplicationLauncherReady.Add (AddAppLauncherButton);

            Logger.Log("[GUI] Added Toolbar GameEvents Handlers", Logger.Level.Debug);

            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);

            Logger.Log("[GUI] awake finished successfully", Logger.Level.Debug);
        }

        private void OnShowUI()
        {
            guiHidden = false;
        }

        private void OnHideUI()
        {
            guiHidden = true;
        }


        public void Start()
        {
            
        }

        public void RebuildUI()
        {
            //should be called by ServoController when required (Vessel changed and such).
        }

        public void Update()
        {
            if(!guiSetupDone)
            {
                //here we need to wait until prefabs become available and then Instatiate the window
                if(UIPrefabLoader.controlWindowPrefabReady && _controlWindow == null)
                {
                    _controlWindow = GameObject.Instantiate (UIPrefabLoader.controlWindowPrefab);
                    _controlWindow.transform.SetParent (MainCanvasUtil.MainCanvas.transform, false);
                    _controlWindow.AddComponent<PanelDragger> ();

                }

                guiSetupDone = UIPrefabLoader.controlWindowPrefabReady
                                && UIPrefabLoader.controlWindowGroupLinePrefabReady
                                && UIPrefabLoader.controlWindowServoLinePrefabReady;
                Logger.Log ("[NEWUI] Are prefabs ready:  " + guiSetupDone);
                if(guiSetupDone)
                {
                    GameObject servoGroupsArea = _controlWindow.GetChild ("FlightServoControlWindowContent").GetChild ("ServoGroupsVLG");

                    for (int i = 0; i < ServoController.Instance.ServoGroups.Count; i++)
                    {
                        Logger.Log ("[NEW UI] Trying to draw group via prefab");
                        ServoController.ControlGroup g = ServoController.Instance.ServoGroups[i];

                        if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != g.Vessel)
                            continue;

                        var newServoGroupLine = GameObject.Instantiate (UIPrefabLoader.controlWindowGroupLinePrefab);
                        newServoGroupLine.transform.SetParent (servoGroupsArea.transform, false);
                        var hlg = newServoGroupLine.GetChild ("ServoGroupControlsHLG");
                        hlg.GetChild ("ServoGroupNameText").GetComponent<Text> ().text = g.Name;
                        hlg.GetChild ("ServoGroupExpandedStatusToggle").GetComponent<Toggle> ().isOn = g.Expanded;

                        _servoGroupLines.Add (newServoGroupLine);

                    }

                }
            }
            else
            {
                //at this poitn we should have window instantiated and filled with groups and servos
                //all we need to do is update the fields



            }

        }

        private void AddAppLauncherButton()
        {
            Logger.Log(string.Format("[GUI] AddAppLauncherButton Called, button=null: {0}", (appLauncherButton == null)), Logger.Level.Debug);

            if (appLauncherButton != null) return;

            if (!ApplicationLauncher.Ready)
                return;

            try
            {
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(delegate { GUIEnabled = true; },
                    delegate { GUIEnabled = false; }, null, null, null, null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB |
                    ApplicationLauncher.AppScenes.SPH, appLauncherButtonTexture);

                ApplicationLauncher.Instance.AddOnHideCallback(OnHideCallback);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("[GUI AddAppLauncherButton Exception, {0}", ex.Message), Logger.Level.Fatal);
            }

            Logger.Log(string.Format("[GUI] AddAppLauncherButton finished, button=null: {0}", (appLauncherButton == null)), Logger.Level.Debug);
        }

        private void OnHideCallback()
        {
            GUIEnabled = false;
        }

        void OnGameSceneLoadRequestedForAppLauncher(GameScenes SceneToLoad)
        {
            DestroyAppLauncherButton();
        }

        private void DestroyAppLauncherButton()
        {
            try
            {
                if (appLauncherButton != null && ApplicationLauncher.Instance != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                    appLauncherButton = null;
                }

                if (ApplicationLauncher.Instance != null)
                    ApplicationLauncher.Instance.RemoveOnHideCallback(OnHideCallback);
            }
            catch (Exception e)
            {
                Logger.Log("[GUI] Failed unregistering AppLauncher handlers," + e.Message);
            }
        }

        private void OnDestroy()
        {
            Logger.Log("[GUI] destroy");

            //GameEvents.onGUIApplicationLauncherReady.Remove (AddAppLauncherButton);
            //GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
            DestroyAppLauncherButton();

            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);

            EditorLock(false);
            SaveConfigXml();
            Logger.Log("[GUI] OnDestroy finished successfully", Logger.Level.Debug);
        }


        private void RefreshKeysFromGUI()
        {
            foreach (var g in ServoController.Instance.ServoGroups)
            {
                g.RefreshKeys();
            }
        }


        /// <summary>
        ///     Applies or removes the lock
        /// </summary>
        /// <param name="apply">Which way are we going</param>
        internal void EditorLock(Boolean apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (HighLogic.LoadedSceneIsEditor && apply)
            {
                //only add a new lock if there isnt already one there
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") != ControlTypes.EDITOR_LOCK)
                {
                    Logger.Log(String.Format("[GUI] AddingLock-{0}", "IRGUILockOfEditor"), Logger.Level.Debug);

                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LOCK, "IRGUILockOfEditor");
                }
            }
            //Otherwise make sure the lock is removed
            else
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") == ControlTypes.EDITOR_LOCK)
                {
                    Logger.Log(String.Format("[GUI] Removing-{0}", "IRGUILockOfEditor"), Logger.Level.Debug);
                    InputLockManager.RemoveControlLock("IRGUILockOfEditor");
                }
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
            config.load();
            useElectricCharge = config.GetValue<bool>("useEC");
            allowServoFlip = config.GetValue<bool>("allowFlipHack");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
            config.SetValue("useEC", useElectricCharge);
            config.SetValue("allowFlipHack", allowServoFlip);
            config.save();
        }
    }
}