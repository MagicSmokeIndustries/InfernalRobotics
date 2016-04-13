using InfernalRobotics.Command;
using InfernalRobotics.Control;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI;
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

        private static WindowManager _instance;
        private static GameObject _controlWindow;
        private static GameObject _uiSettingsWindow;
        private static GameObject _editorWindow;
        private static GameObject _presetsWindow;
        private static IServo presetWindowServo;

        private static CanvasGroupFader _controlWindowFader;
        private static CanvasGroupFader _editorWindowFader;
        private static CanvasGroupFader _uiSettingsWindowFader;
        private static CanvasGroupFader _presetsWindowFader;

        internal static Dictionary<ServoController.ControlGroup,GameObject> _servoGroupUIControls;
        internal static Dictionary<IServo, GameObject> _servoUIControls;

        public static float _UIAlphaValue = 0.8f;
        public static float _UIScaleValue = 1.0f;
        private const float UI_FADE_TIME = 0.1f;
        private const float UI_MIN_ALPHA = 0.2f;
        private const float UI_MIN_SCALE = 0.5f;
        private const float UI_MAX_SCALE = 2.0f;

        internal static bool guiRebuildPending = false;
        private ApplicationLauncherButton appLauncherButton;
        private static Texture2D appLauncherButtonTexture;

        private bool guiFlightEditorWindowOpen;
        private bool guiFlightPresetModeOn;
        private bool guiPresetsWindowOpen;
        private bool guiHidden;

        private static Vector3 _controlWindowPosition;
        private static Vector3 _editorWindowPosition;
        private static Vector3 _uiSettingsWindowPosition;

        private static Vector2 _editorWindowSize;

        public static bool UseElectricCharge = true;

        public bool GUIEnabled = false;

        private static bool isKeyboardLocked = false;

        public static WindowManager Instance
        {
            get { return _instance; }
        }

        private void Awake()
        {
            
            LoadConfigXml();

            Logger.Log("[NewGUI] awake, Mode: " + AddonName);
            guiFlightEditorWindowOpen = false;

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT || scene == GameScenes.EDITOR)
            {
                _instance = this;

                _servoGroupUIControls = new Dictionary<ServoController.ControlGroup, GameObject>();
                _servoUIControls = new Dictionary<IServo, GameObject>();
            }
            else
            {
                _instance = null;
                //actually we don't need to go further if it's not flight or editor
                return;
            }

            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);

            GameEvents.onGUIApplicationLauncherReady.Add (AddAppLauncherButton);

            Logger.Log("[GUI] Added Toolbar GameEvents Handlers", Logger.Level.Debug);

            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);

            Logger.Log("[GUI] awake finished successfully", Logger.Level.Debug);
        }

        private void OnShowUI()
        {
            guiHidden = false;
            /*
            if (_controlWindowFader)
            {
                _controlWindowFader.FadeTo(_UIAlphaValue, 0.1f);
            }
            if (_editorWindowFader)
            {
                _editorWindowFader.FadeTo(_UIAlphaValue, 0.1f);
            }
            if (_uiSettingsWindowFader)
            {
                _uiSettingsWindowFader.FadeTo(_UIAlphaValue, 0.1f);
            }
            if (_presetsWindowFader)
            {
                _presetsWindowFader.FadeTo(_UIAlphaValue, 0.1f);
            }*/
        }

        private void OnHideUI()
        {
            guiHidden = true;
            /*
            if(_controlWindowFader)
            {
                _controlWindowFader.FadeTo(0f, 0.1f);
            }
            if(_editorWindowFader)
            {
                _editorWindowFader.FadeTo(0f, 0.1f);
            }
            if(_uiSettingsWindowFader)
            {
                _uiSettingsWindowFader.FadeTo(0f, 0.1f);
            }
            if(_presetsWindowFader)
            {
                _presetsWindowFader.FadeTo(0f, 0.1f);
            }*/
        }

        private void InitUISettingsWindow()
        {
            if (_uiSettingsWindow != null)
                return;

            _uiSettingsWindow = GameObject.Instantiate(UIAssetsLoader.uiSettingsWindowPrefab);
            _uiSettingsWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
            _uiSettingsWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
            _uiSettingsWindowFader = _uiSettingsWindow.AddComponent<CanvasGroupFader>();

            _uiSettingsWindow.GetComponent<CanvasGroup>().alpha = 0f;

            var closeButton = _uiSettingsWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
            if (closeButton != null)
            {
                closeButton.GetComponent<Button>().onClick.AddListener(ToggleUISettingsWindow);
            }

            var transparencySlider = _uiSettingsWindow.GetChild("WindowContent").GetChild("UITransparencySliderHLG").GetChild("TransparencySlider");

            if(transparencySlider)
            {
                var sliderControl = transparencySlider.GetComponent<Slider>();
                sliderControl.minValue = UI_MIN_ALPHA;
                sliderControl.maxValue = 1.0f;
                sliderControl.value = _UIAlphaValue;
                sliderControl.onValueChanged.AddListener(SetGlobalAlpha);
            }
            
            var alphaText = _uiSettingsWindow.GetChild("WindowContent").GetChild("UITransparencySliderHLG").GetChild("TransparencyLabel").GetComponent<Text>();
            alphaText.text = "Transparency: " + string.Format("{0:#0.00}", _UIAlphaValue);

            var scaleSlider = _uiSettingsWindow.GetChild("WindowContent").GetChild("UIScaleSliderHLG").GetChild("ScaleSlider");

            if (scaleSlider)
            {
                var sliderControl = scaleSlider.GetComponent<Slider>();
                sliderControl.minValue = UI_MIN_SCALE;
                sliderControl.maxValue = UI_MAX_SCALE;
                sliderControl.value = _UIScaleValue;
                sliderControl.onValueChanged.AddListener(SetGlobalScale);
            }

            var scaleText = _uiSettingsWindow.GetChild("WindowContent").GetChild("UIScaleSliderHLG").GetChild("ScaleLabel").GetComponent<Text>();
            scaleText.text = "UI Scale: " + string.Format("{0:#0.00}", _UIScaleValue);

            _uiSettingsWindow.SetActive(false);
        }

        private void SetGlobalAlpha(float newAlpha)
        {
            _UIAlphaValue = Mathf.Clamp(newAlpha, UI_MIN_ALPHA, 1.0f);

            if (_controlWindow)
            {
                _controlWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;
            }
            if (_uiSettingsWindow)
            {
                _uiSettingsWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;

                var alphaText = _uiSettingsWindow.GetChild("WindowContent").GetChild("UITransparencySliderHLG").GetChild("TransparencyLabel").GetComponent<Text>();
                alphaText.text = "Transparency: " + string.Format("{0:#0.##}", _UIAlphaValue);
            }
            if (_editorWindow)
            {
                _editorWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;
            }
        }

        private void SetGlobalScale(float newScale)
        {
            _UIScaleValue = Mathf.Clamp(newScale, UI_MIN_SCALE, UI_MAX_SCALE);
        }

        public void ToggleUISettingsWindow()
        {
            if (_uiSettingsWindow == null || _uiSettingsWindowFader == null)
                return;

            //lets simplify things
            if (_uiSettingsWindowFader.IsFading)
                return;

            if (_uiSettingsWindow.activeInHierarchy)
            {
                //fade the window out and deactivate
                _uiSettingsWindowFader.FadeTo(0, UI_FADE_TIME, () => { _uiSettingsWindow.SetActive(false); });
            }
            else
            {
                //activate and fade the window in,
                _uiSettingsWindow.SetActive(true);
                _uiSettingsWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
            }
        }

        private void InitFlightControlWindow(bool startSolid = true)
        {
            _controlWindow = GameObject.Instantiate(UIAssetsLoader.controlWindowPrefab);
            _controlWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
            _controlWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
            _controlWindowFader = _controlWindow.AddComponent<CanvasGroupFader>();

            //start invisible to be toggled later
            if(!startSolid)
                _controlWindow.GetComponent<CanvasGroup>().alpha = 0f;

            if (_controlWindowPosition == Vector3.zero)
            {
                //get the default position from the prefab
                _controlWindowPosition = _controlWindow.transform.position;
            }
            else
            {
                _controlWindow.transform.position = _controlWindowPosition;
            }

            var uiSettingsButton = _controlWindow.GetChild("WindowTitle").GetChild("LeftWindowButton");
            if (uiSettingsButton != null)
            {
                uiSettingsButton.GetComponent<Button>().onClick.AddListener(ToggleUISettingsWindow);
                var t = uiSettingsButton.AddComponent<BasicTooltip>();
                t.tooltipText = "Show/hide UI settings";
            }

            var closeButton = _controlWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
            if (closeButton != null)
            {
                closeButton.GetComponent<Button>().onClick.AddListener(HideIRWindow);
                var t = closeButton.AddComponent<BasicTooltip>();
                t.tooltipText = "Close window";
            }

            var flightWindowFooterButtons = _controlWindow.GetChild ("WindowFooter").GetChild ("FlightWindowFooterButtonsHLG");
            var openEditorButton = flightWindowFooterButtons.GetChild ("EditGroupsButton").GetComponent<Button> ();
            openEditorButton.onClick.AddListener (ToggleFlightEditor);

            var openEditorButtonTooltip = openEditorButton.gameObject.AddComponent<BasicTooltip>();
            openEditorButtonTooltip.tooltipText = "Switch to Editor Mode";

            var presetModeToggle = flightWindowFooterButtons.GetChild ("PresetModeButton").GetComponent<Toggle> ();
            presetModeToggle.isOn = guiFlightPresetModeOn;
            presetModeToggle.onValueChanged.AddListener (ToggleFlightPresetMode);

            var presetModeTooltip = presetModeToggle.gameObject.AddComponent<BasicTooltip>();
            presetModeTooltip.tooltipText = "Toggle Preset Mode";

            var stopAllButton = flightWindowFooterButtons.GetChild ("StopAllButton").GetComponent<Button> ();
            stopAllButton.onClick.AddListener (() => {
                foreach(var pair in _servoGroupUIControls)
                {
                    pair.Key.Stop();
                    pair.Key.MovingNegative = false;
                    pair.Key.MovingPositive = false;
                }
                guiRebuildPending = true;
                //TODO: we need to reset oll Movement Toggles
            });

            var stopAllTooltip = stopAllButton.gameObject.AddComponent<BasicTooltip>();
            stopAllTooltip.tooltipText = "Panic! Stop all servos!";
        }

        private void InitFlightGroupControls(GameObject newServoGroupLine, ServoController.ControlGroup g)
        {
            var hlg = newServoGroupLine.GetChild("ServoGroupControlsHLG");
            var servosVLG = newServoGroupLine.GetChild("ServoGroupServosVLG");

            hlg.GetChild("ServoGroupNameText").GetComponent<Text>().text = g.Name;
            
            var groupToggle = hlg.GetChild("ServoGroupExpandedStatusToggle").GetComponent<Toggle>();
            var groupLockToggleIcon = hlg.GetChild("ServoGroupExpandedStatusToggle").GetChild("Icon").GetComponent<RawImage>();
            groupToggle.onValueChanged.AddListener(v =>
            {
                groupLockToggleIcon.texture = UIAssetsLoader.iconAssets.Find(i => i.name == (g.Expanded ? "expand" : "collapse"));
                
                g.Expanded = v;
                servosVLG.SetActive(g.Expanded);
            });

            if(g.Expanded)
                groupLockToggleIcon.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "collapse");
            servosVLG.SetActive(g.Expanded);

            var groupExpandTooltip = groupToggle.gameObject.AddComponent<BasicTooltip>();
            groupExpandTooltip.tooltipText = "Show/hide group's servos";
            
            var groupSpeed = hlg.GetChild("ServoGroupSpeedMultiplier").GetComponent<InputField>();
            groupSpeed.text = g.Speed;
            groupSpeed.onEndEdit.AddListener(v => { g.Speed = v; });

            var groupSpeedTooltip = groupSpeed.gameObject.AddComponent<BasicTooltip>();
            groupSpeedTooltip.tooltipText = "Speed Multiplier";

            var groupMoveLeftToggle = hlg.GetChild("ServoGroupMoveLeftToggleButton").GetComponent<Toggle>();
            groupMoveLeftToggle.isOn = g.MovingNegative;
            groupMoveLeftToggle.onValueChanged.AddListener(v =>
            {
                if (g.MovingNegative)
                {
                    g.Stop();
                    g.MovingNegative = false;
                    g.MovingPositive = false;
                }
                else
                {
                    g.MoveLeft();
                    g.MovingNegative = true;
                    g.MovingPositive = false;
                }
            });
            var groupMoveLeftToggleTooltip = groupMoveLeftToggle.gameObject.AddComponent<BasicTooltip>();
            groupMoveLeftToggleTooltip.tooltipText = "Toggle negative movement";

            var groupMoveLeftButton = hlg.GetChild("ServoGroupMoveLeftButton");
            var groupMoveLeftHoldButton = groupMoveLeftButton.AddComponent<HoldButton>();
            groupMoveLeftHoldButton.callbackOnDown = g.MoveLeft;
            groupMoveLeftHoldButton.callbackOnUp = g.Stop;

            var groupMoveLeftTooltip = groupMoveLeftButton.AddComponent<BasicTooltip>();
            groupMoveLeftTooltip.tooltipText = "Hold to move negative";
            
            var groupMoveCenterButton = hlg.GetChild("ServoGroupMoveCenterButton");
            var groupMoveCenterHoldButton = groupMoveCenterButton.AddComponent<HoldButton>();
            groupMoveCenterHoldButton.callbackOnDown = g.MoveCenter;
            groupMoveCenterHoldButton.callbackOnUp = g.Stop;

            var groupMoveCenterTooltip = groupMoveCenterButton.AddComponent<BasicTooltip>();
            groupMoveCenterTooltip.tooltipText = "Hold to move \nto default position";

            var groupMoveRightButton = hlg.GetChild("ServoGroupMoveRightButton");
            var groupMoveRightHoldButton = groupMoveRightButton.AddComponent<HoldButton>();
            groupMoveRightHoldButton.callbackOnDown = g.MoveRight;
            groupMoveRightHoldButton.callbackOnUp = g.Stop;

            var groupMoveRightTooltip = groupMoveRightButton.AddComponent<BasicTooltip>();
            groupMoveRightTooltip.tooltipText = "Hold to move positive";

            var groupMoveRightToggle = hlg.GetChild("ServoGroupMoveRightToggleButton").GetComponent<Toggle>();
            groupMoveRightToggle.isOn = g.MovingPositive;
            groupMoveRightToggle.onValueChanged.AddListener(v =>
            {
                if (g.MovingPositive)
                {
                    g.Stop();
                    g.MovingNegative = false;
                    g.MovingPositive = false;
                }
                else
                {
                    g.MoveRight();
                    g.MovingNegative = false;
                    g.MovingPositive = true;
                }
            });

            var groupMoveRightToggleTooltip = groupMoveRightToggle.gameObject.AddComponent<BasicTooltip>();
            groupMoveRightToggleTooltip.tooltipText = "Toggle positive movement";

            var groupMovePrevPresetButton = hlg.GetChild ("ServoGroupMovePrevPresetButton").GetComponent<Button>();
            groupMovePrevPresetButton.onClick.AddListener (g.MovePrevPreset);

            var groupMovePrevPresetTooltip = groupMovePrevPresetButton.gameObject.AddComponent<BasicTooltip>();
            groupMovePrevPresetTooltip.tooltipText = "Move to previous preset";

            var groupRevertButton = hlg.GetChild ("ServoGroupRevertButton").GetComponent<Button> ();
            groupRevertButton.onClick.AddListener (g.MoveCenter);

            var groupRevertTooltip = groupRevertButton.gameObject.AddComponent<BasicTooltip>();
            groupRevertTooltip.tooltipText = "Move to default position";

            var groupMoveNextPresetButton = hlg.GetChild ("ServoGroupMoveNextPresetButton").GetComponent<Button>();
            groupMoveNextPresetButton.onClick.AddListener (g.MoveNextPreset);

            var groupMoveNextPresetTooltip = groupMoveNextPresetButton.gameObject.AddComponent<BasicTooltip>();
            groupMoveNextPresetTooltip.tooltipText = "Move to next preset";

            //now list servos
            for (int j = 0; j < g.Servos.Count; j++)
            {
                var s = g.Servos[j];

                if (s.Mechanism.IsFreeMoving)
                    continue;

                Logger.Log("[NEW UI] Trying to draw servo via prefab, servo name" + s.Name);

                var newServoLine = GameObject.Instantiate(UIAssetsLoader.controlWindowServoLinePrefab);
                newServoLine.transform.SetParent(servosVLG.transform, false);

                InitFlightServoControls(newServoLine, s);

                _servoUIControls.Add(s, newServoLine);
            }
        }

        private void InitFlightServoControls(GameObject newServoLine, IServo s)
        {
            var servoStatusLight = newServoLine.GetChild("ServoStatusRawImage").GetComponent<RawImage>();
            if (s.Mechanism.IsLocked)
            {
                var redDot = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Locked");
                if (redDot != null)
                    servoStatusLight.texture = redDot;
            }

            var servoName = newServoLine.GetChild("ServoNameText").GetComponent<Text>();
            servoName.text = s.Name;

            var servoPosition = newServoLine.GetChild("ServoPositionText").GetComponent<Text>();
            servoPosition.text = string.Format("{0:#0.00}", s.Mechanism.Position);

            var servoLockToggle = newServoLine.GetChild("ServoLockToggleButton").GetComponent<Toggle>();
            var servoLockToggleIcon = newServoLine.GetChild("ServoLockToggleButton").GetChild("Icon").GetComponent<RawImage>();
            servoLockToggle.isOn = s.Mechanism.IsLocked;
            if (servoLockToggle.isOn)
                servoLockToggleIcon.color = Color.clear;
            servoLockToggle.onValueChanged.AddListener(v =>
            {
                s.Mechanism.IsLocked = v;
                servoLockToggle.isOn = v;
                servoLockToggleIcon.color = (v ? Color.clear : Color.white);
            });

            var servoLockToggleTooltip = servoLockToggle.gameObject.AddComponent<BasicTooltip>();
            servoLockToggleTooltip.tooltipText = "Lock/unlock the servo";

            var servoMoveLeftButton = newServoLine.GetChild("ServoMoveLeftButton");
            var servoMoveLeftHoldButton = servoMoveLeftButton.AddComponent<HoldButton>();
            servoMoveLeftHoldButton.callbackOnDown = s.Motor.MoveLeft;
            servoMoveLeftHoldButton.callbackOnUp = s.Motor.Stop;

            var servoMoveLeftTooltip = servoMoveLeftButton.AddComponent<BasicTooltip>();
            servoMoveLeftTooltip.tooltipText = "Hold to move negative";

            var servoMoveCenterButton = newServoLine.GetChild("ServoMoveCenterButton");
            var servoMoveCenterHoldButton = servoMoveCenterButton.AddComponent<HoldButton>();
            servoMoveCenterHoldButton.callbackOnDown = s.Motor.MoveCenter;
            servoMoveCenterHoldButton.callbackOnUp = s.Motor.Stop;

            var servoMoveCenterTooltip = servoMoveCenterButton.AddComponent<BasicTooltip>();
            servoMoveCenterTooltip.tooltipText = "Hold to move\n to default position";

            var servoMoveRightButton = newServoLine.GetChild("ServoMoveRightButton");
            var servoMoveRightHoldButton = servoMoveRightButton.AddComponent<HoldButton>();
            servoMoveRightHoldButton.callbackOnDown = s.Motor.MoveRight;
            servoMoveRightHoldButton.callbackOnUp = s.Motor.Stop;

            var servoMoveRightTooltip = servoMoveRightButton.AddComponent<BasicTooltip>();
            servoMoveRightTooltip.tooltipText = "Hold to move negative";

            var servoInvertAxisToggle = newServoLine.GetChild("ServoInvertAxisToggleButton").GetComponent<Toggle>();
            var servoInverAxisToggleIcon = newServoLine.GetChild("ServoInvertAxisToggleButton").GetChild("Icon").GetComponent<RawImage>();
            servoInvertAxisToggle.isOn = s.Motor.IsAxisInverted;
            if (servoInvertAxisToggle.isOn)
                servoInverAxisToggleIcon.color = Color.clear;
            servoInvertAxisToggle.onValueChanged.AddListener(v =>
            {
                s.Motor.IsAxisInverted = v;
                servoInvertAxisToggle.isOn = v;
                servoInverAxisToggleIcon.color = (v ? Color.clear : Color.white);
            });
            var servoInvertAxisToggleTooltip = servoInvertAxisToggle.gameObject.AddComponent<BasicTooltip>();
            servoInvertAxisToggleTooltip.tooltipText = "Invert/uninvert servo axis";

            var servoPrevPresetButton = newServoLine.GetChild ("ServoMovePrevPresetButton").GetComponent<Button> ();
            servoPrevPresetButton.onClick.AddListener (s.Preset.MovePrev);

            var servoPrevPresetTooltip = servoPrevPresetButton.gameObject.AddComponent<BasicTooltip>();
            servoPrevPresetTooltip.tooltipText = "Move to previous preset";

            var servoOpenPresetsToggle = newServoLine.GetChild ("ServoOpenPresetsToggle").GetComponent<Toggle> ();
            servoOpenPresetsToggle.onValueChanged.AddListener (v => TogglePresetEditWindow (s, v, servoOpenPresetsToggle.gameObject));

            var servoOpenPresetsToggleTooltip = servoOpenPresetsToggle.gameObject.AddComponent<BasicTooltip>();
            servoOpenPresetsToggleTooltip.tooltipText = "Open/close presets";

            var servoNextPresetButton = newServoLine.GetChild ("ServoMoveNextPresetButton").GetComponent<Button> ();
            servoNextPresetButton.onClick.AddListener (s.Preset.MoveNext);

            var servoNextPresetTooltip = servoNextPresetButton.gameObject.AddComponent<BasicTooltip>();
            servoNextPresetTooltip.tooltipText = "Move to next preset";
        }

        private void SetGroupPresetControlsVisibility(GameObject groupUIControls, bool value)
        {
            groupUIControls.GetChild("ServoGroupMoveLeftButton").SetActive(!value);
            groupUIControls.GetChild("ServoGroupMoveCenterButton").SetActive(!value);
            groupUIControls.GetChild("ServoGroupMoveRightButton").SetActive(!value);

            groupUIControls.GetChild("ServoGroupMovePrevPresetButton").SetActive(value);
            groupUIControls.GetChild("ServoGroupRevertButton").SetActive(value);
            groupUIControls.GetChild("ServoGroupMoveNextPresetButton").SetActive(value);
        }

        private void SetServoPresetControlsVisibility(GameObject servoUIControls, bool value)
        {
            servoUIControls.GetChild("ServoMoveLeftButton").SetActive(!value);
            servoUIControls.GetChild("ServoMoveCenterButton").SetActive(!value);
            servoUIControls.GetChild("ServoMoveRightButton").SetActive(!value);

            servoUIControls.GetChild("ServoMovePrevPresetButton").SetActive(value);
            servoUIControls.GetChild("ServoOpenPresetsToggle").SetActive(value);
            servoUIControls.GetChild("ServoMoveNextPresetButton").SetActive(value);
        }

        private void ToggleFlightPresetMode(bool value)
        {
            if (_controlWindow == null || _servoUIControls == null || _servoGroupUIControls == null)
                return;

            guiFlightPresetModeOn = value;

            //we need to turn off preset buttons and turn on normal buttons
            foreach (var groupPair in _servoGroupUIControls)
            {
                SetGroupPresetControlsVisibility(groupPair.Value, value);
            }
            foreach (var servoPair in _servoUIControls)
            {
                SetServoPresetControlsVisibility(servoPair.Value, value);
            }
        }

        public void PresetInputOnEndEdit(string tmp, int i)
        {
            if (presetWindowServo == null)
                return;

            float tmpValue = 0f;
            if (float.TryParse(tmp, out tmpValue))
            {
                tmpValue = Mathf.Clamp(tmpValue, presetWindowServo.Mechanism.MinPositionLimit, presetWindowServo.Mechanism.MaxPositionLimit);
                if (presetWindowServo.Preset[i] == presetWindowServo.Mechanism.DefaultPosition)
                {
                    presetWindowServo.Mechanism.DefaultPosition = tmpValue;
                }
                presetWindowServo.Preset[i] = tmpValue;
                presetWindowServo.Preset.Sort();
            }
        }

        private void CopyPresetsToSiblings(IServo s)
        {

        }

        public void TogglePresetEditWindow (IServo servo, bool value, GameObject buttonRef)
        {
            guiPresetsWindowOpen = value;

            if (value)
            {
                if (_presetsWindow)
                {
                    //also need to find the other Toggle button that opened it
                    foreach (var pair in _servoUIControls)
                    {
                        var toggle = pair.Value.GetChild("ServoOpenPresetsToggle");
                        if (toggle == null || toggle == buttonRef)
                            continue;
                        toggle.GetComponent<Toggle>().isOn = false;
                    }
                    _presetsWindow.DestroyGameObjectImmediate();
                    _presetsWindow = null;
                }

                _presetsWindow = GameObject.Instantiate(UIAssetsLoader.presetWindowPrefab);
                _presetsWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
                _presetsWindow.GetComponent<CanvasGroup>().alpha = 0f;
                _presetsWindowFader = _presetsWindow.AddComponent<CanvasGroupFader>();

                //need a better way to tie them to each other
                _presetsWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
                _presetsWindow.transform.position = buttonRef.transform.position + new Vector3(30, 0, 0);

                presetWindowServo = servo;

                var closeButton = _presetsWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
                if (closeButton != null)
                {
                    closeButton.GetComponent<Button>().onClick.AddListener(() => {
                        TogglePresetEditWindow(servo, false, buttonRef);
                        buttonRef.GetComponent<Toggle>().isOn = false;
                    });
                    var t = closeButton.AddComponent<BasicTooltip>();
                    t.tooltipText = "Close preset window";
                }

                var footerControls = _presetsWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");
                var newPresetPositionInputField = footerControls.GetChild("NewPresetPositionInputField").GetComponent<InputField>();
                newPresetPositionInputField.text = string.Format("{0:#0.##}", servo.Mechanism.Position);

                var addPresetButton = footerControls.GetChild("AddPresetButton").GetComponent<Button>();
                addPresetButton.onClick.AddListener(() =>
                {
                    string tmp = newPresetPositionInputField.text;
                    float tmpValue = 0f;
                    if (float.TryParse(tmp, out tmpValue))
                    {
                        tmpValue = Mathf.Clamp(tmpValue, presetWindowServo.Mechanism.MinPositionLimit, presetWindowServo.Mechanism.MaxPositionLimit);
                        presetWindowServo.Preset.Add(tmpValue);
                    }
                });

                var addPresetButtonTooltip = addPresetButton.gameObject.AddComponent<BasicTooltip>();
                addPresetButtonTooltip.tooltipText = "Add preset";

                var copyPresetsButton = footerControls.GetChild("ApplySymmetryButton").GetComponent<Button>();
                copyPresetsButton.onClick.AddListener(() => CopyPresetsToSiblings(servo));

                var copyPresetsButtonTooltip = copyPresetsButton.gameObject.AddComponent<BasicTooltip>();
                copyPresetsButtonTooltip.tooltipText = "Copy to siblings";

                var presetsArea = _presetsWindow.GetChild("WindowContent");
                
                //now populate it with servo's presets
                for (int i=0; i<servo.Preset.Count; i++)
                {
                    var newPresetLine = GameObject.Instantiate(UIAssetsLoader.presetLinePrefab);
                    newPresetLine.transform.SetParent(presetsArea.transform, false);

                    var presetPositionInputField = newPresetLine.GetChild("PresetPositionInputField").GetComponent<InputField>();
                    presetPositionInputField.text = string.Format("{0:#0.##}", servo.Preset[i]);
                    var presetIndex = i;
                    presetPositionInputField.onEndEdit.AddListener(tmp => { PresetInputOnEndEdit(tmp, presetIndex); });

                    var servoDefaultPositionToggle = newPresetLine.GetChild("PresetDefaultPositionToggle").GetComponent<Toggle>();
                    servoDefaultPositionToggle.group = presetsArea.GetComponent<ToggleGroup>();
                    servoDefaultPositionToggle.isOn = (servo.Mechanism.DefaultPosition == servo.Preset[i]);
                    servoDefaultPositionToggle.onValueChanged.AddListener(v =>
                    {
                        if(v)
                        {
                            presetWindowServo.Mechanism.DefaultPosition = presetWindowServo.Preset[presetIndex];
                        }
                    });

                    var servoDefaultPositionToggleTooltip = servoDefaultPositionToggle.gameObject.AddComponent<BasicTooltip>();
                    servoDefaultPositionToggleTooltip.tooltipText = "Set as default position";

                    var presetMoveHereButton = newPresetLine.GetChild("PresetMoveHereButton").GetComponent<Button>();
                    presetMoveHereButton.onClick.AddListener(() => servo.Preset.MoveTo(i));

                    var presetMoveHereButtonTooltip = presetMoveHereButton.gameObject.AddComponent<BasicTooltip>();
                    presetMoveHereButtonTooltip.tooltipText = "Move to this position";

                    var presetDeleteButton = newPresetLine.GetChild("PresetDeleteButton").GetComponent<Button>();
                    presetDeleteButton.onClick.AddListener(() =>
                    {
                        if (presetWindowServo.Preset[presetIndex] == presetWindowServo.Mechanism.DefaultPosition)
                            presetWindowServo.Mechanism.DefaultPosition = 0;
                        presetWindowServo.Preset.RemoveAt(presetIndex);
                        Destroy(newPresetLine);
                    });

                    var presetDeleteButtonTooltip = presetDeleteButton.gameObject.AddComponent<BasicTooltip>();
                    presetDeleteButtonTooltip.tooltipText = "Delete preset";
                }

                _presetsWindowFader.FadeTo(_UIAlphaValue, 0.1f);
            }
            else
            {
                //just animate close the window.
                if(_presetsWindowFader)
                    _presetsWindowFader.FadeTo(0f, 0.1f, () => {
                        _presetsWindow.DestroyGameObjectImmediate();
                        _presetsWindow = null;
                        _presetsWindowFader = null;
                        presetWindowServo = null;
                    });
                
            }

        }
        private void ToggleFlightEditor()
        {
            if(!guiFlightEditorWindowOpen)
            {
                guiFlightEditorWindowOpen = true;
                //collapse all groups
                foreach(var g in ServoController.Instance.ServoGroups)
                {
                    g.Expanded = false;
                }
                RebuildUI();
            }
            else
            {
                guiFlightEditorWindowOpen = false;
                //collapse all groups
                foreach(var g in ServoController.Instance.ServoGroups)
                {
                    g.Expanded = false;
                }
                RebuildUI();
            }

        }

        private void InitEditorWindow(bool startSolid = true)
        {
            _editorWindow = GameObject.Instantiate(UIAssetsLoader.editorWindowPrefab);
            _editorWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
            _editorWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
            _editorWindowFader = _editorWindow.AddComponent<CanvasGroupFader>();

            //start invisible to be toggled later
            if (!startSolid)
                _editorWindow.GetComponent<CanvasGroup>().alpha = 0f;

            if (_editorWindowPosition == Vector3.zero)
            {
                //get the default position from the prefab
                _editorWindowPosition = _editorWindow.transform.position;
            }
            else
            {
                _editorWindow.transform.position = _editorWindowPosition;
            }

            var uiSettingsButton = _editorWindow.GetChild("WindowTitle").GetChild("LeftWindowButton");
            if (uiSettingsButton != null)
            {
                uiSettingsButton.GetComponent<Button>().onClick.AddListener(ToggleUISettingsWindow);
                var t = uiSettingsButton.AddComponent<BasicTooltip>();
                t.tooltipText = "Show/hide UI settings";
            }

            var closeButton = _editorWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
            if (closeButton != null)
            {
                if(guiFlightEditorWindowOpen)
                {
                    closeButton.GetComponent<Button>().onClick.AddListener(ToggleFlightEditor);
                    var t = closeButton.AddComponent<BasicTooltip>();
                    t.tooltipText = "Return to Flight Mode";
                }
                else
                {
                    closeButton.GetComponent<Button>().onClick.AddListener(HideIRWindow);
                    var t = closeButton.AddComponent<BasicTooltip>();
                    t.tooltipText = "Close window";
                }
                    
            }

            var editorFooterButtons = _editorWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");
            var newGroupNameInputField = editorFooterButtons.GetChild("NewGroupNameInputField").GetComponent<InputField>();
            var addGroupButton = editorFooterButtons.GetChild("AddGroupButton").GetComponent<Button>();
            addGroupButton.onClick.AddListener(() =>
            {
                var g = new ServoController.ControlGroup { Name = newGroupNameInputField.text };
                ServoController.Instance.ServoGroups.Add(g);

                GameObject servoGroupsArea = _editorWindow.GetChild("WindowContent").GetChild("Scroll View").GetChild("Viewport").GetChild("Content").GetChild("ServoGroupsVLG");

                var newServoGroupLine = GameObject.Instantiate(UIAssetsLoader.editorWindowGroupLinePrefab);
                newServoGroupLine.transform.SetParent(servoGroupsArea.transform, false);

                InitEditorGroupControls(newServoGroupLine, g);

                _servoGroupUIControls.Add(g, newServoGroupLine);
            });

            var addGroupTooltip = addGroupButton.gameObject.AddComponent<BasicTooltip>();
            addGroupTooltip.tooltipText = "Add new group to the\n end of the list.";

            var buildAidToggle = editorFooterButtons.GetChild("BuildAidToggle").GetComponent<Toggle>();
            buildAidToggle.onValueChanged.AddListener(v =>
            {
                foreach (var pair in _servoUIControls)
                {
                    var servoBuildAidToggle = pair.Value.GetChild("ServoBuildAidToggle");
                    servoBuildAidToggle.SetActive(v);
                }

                if (IRBuildAid.IRBuildAidManager.Instance != null)
                    IRBuildAid.IRBuildAidManager.isHidden = v;
            });

            var buildAidToggleTooltip = buildAidToggle.gameObject.AddComponent<BasicTooltip>();
            buildAidToggleTooltip.tooltipText = "Toggle IRBuildAid";

            var resizeHandler = editorFooterButtons.GetChild("ResizeHandle").AddComponent<PanelResizer>();
            resizeHandler.rectTransform = _editorWindow.transform as RectTransform;
            resizeHandler.minSize = new Vector2(350, 280);
            resizeHandler.maxSize = new Vector2(2000, 1600);


        }

        private void InitEditorGroupControls(GameObject newServoGroupLine, ServoController.ControlGroup g)
        {
            var hlg = newServoGroupLine.GetChild("ServoGroupControlsHLG");
            var servosVLG = newServoGroupLine.GetChild("ServoGroupServosVLG");
            servosVLG.AddComponent<ServoDropHandler>();

            var groupDragHandler = hlg.GetChild("GroupDragHandle").AddComponent<GroupDragHandler>();
            groupDragHandler.mainCanvas = UIMasterController.Instance.appCanvas;
            groupDragHandler.background = UIAssetsLoader.spriteAssets.Find(a => a.name == "IRWindowGroupFrame_Drag");
            
            var groupDragHandlerTooltip = groupDragHandler.gameObject.AddComponent<BasicTooltip>();
            groupDragHandlerTooltip.tooltipText = "Drag to reorder the group";

            var groupName = hlg.GetChild("GroupNameInputField").GetComponent<InputField>();
            groupName.text = g.Name;
            groupName.onEndEdit.AddListener(s => { g.Name = s; });

            var groupAdvancedModeToggle = hlg.GetChild("GroupAdvancedModeToggle").GetComponent<Toggle>();
            groupAdvancedModeToggle.isOn = g.Expanded;
            groupAdvancedModeToggle.onValueChanged.AddListener(v =>
            {
                g.Expanded = v;
                for(int i=0; i < g.Servos.Count; i++)
                {
                    ShowServoAdvancedMode(g.Servos[i], v);
                }
            });

            var groupAdvancedModeToggleTooltip = groupAdvancedModeToggle.gameObject.AddComponent<BasicTooltip>();
            groupAdvancedModeToggleTooltip.tooltipText = "Show/hide advanced servo parameters";

            var groupMoveLeftKey = hlg.GetChild("GroupMoveLeftKey").GetComponent<InputField>();
            groupMoveLeftKey.text = g.ReverseKey;
            groupMoveLeftKey.onEndEdit.AddListener(s => { g.ReverseKey = s; });

            var groupMoveRightKey = hlg.GetChild("GroupMoveRightKey").GetComponent<InputField>();
            groupMoveRightKey.text = g.ForwardKey;
            groupMoveRightKey.onEndEdit.AddListener(s => { g.ForwardKey = s; });

            hlg.GetChild("GroupECRequiredText").GetComponent<Text>().text = string.Format("{0:#0.##}",g.TotalElectricChargeRequirement);

            var groupMoveLeftButton = hlg.GetChild("GroupMoveLeftButton");
            var groupMoveLeftHoldButton = groupMoveLeftButton.AddComponent<HoldButton>();
            groupMoveLeftHoldButton.callbackOnDown = g.MoveLeft;
            groupMoveLeftHoldButton.callbackOnUp = g.Stop;
            //this is needed in Editor only
            groupMoveLeftHoldButton.updateHandler = g.MoveLeft;
            
            var groupMoveLeftTooltip = groupMoveLeftButton.AddComponent<BasicTooltip>();
            groupMoveLeftTooltip.tooltipText = "Hold to move negative";

            var groupMoveCenterButton = hlg.GetChild("GroupMoveCenterButton");
            var groupMoveCenterHoldButton = groupMoveCenterButton.AddComponent<HoldButton>();
            groupMoveCenterHoldButton.callbackOnDown = g.MoveCenter;
            groupMoveCenterHoldButton.callbackOnUp = g.Stop;
            //this is needed in Editor only
            groupMoveCenterHoldButton.updateHandler = g.MoveCenter;

            var groupMoveCenterButtonTooltip = groupMoveCenterButton.AddComponent<BasicTooltip>();
            groupMoveCenterButtonTooltip.tooltipText = "Move to default position";

            var groupMoveRightButton = hlg.GetChild("GroupMoveRightButton");
            var groupMoveRightHoldButton = groupMoveRightButton.AddComponent<HoldButton>();
            groupMoveRightHoldButton.callbackOnDown = g.MoveRight;
            groupMoveRightHoldButton.callbackOnUp = g.Stop;
            //this is needed in Editor only
            groupMoveRightHoldButton.updateHandler = g.MoveRight;

            var groupMoveRightTooltip = groupMoveRightButton.AddComponent<BasicTooltip>();
            groupMoveRightTooltip.tooltipText = "Hold to move positive";

            var groupDeleteButton = hlg.GetChild("GroupDeleteButton").GetComponent<Button>();
            groupDeleteButton.onClick.AddListener(() =>
            {
                if(ServoController.Instance.ServoGroups.Count > 1)
                {
                    while (g.Servos.Any())
                    {
                        var s = g.Servos.First();
                        ServoController.MoveServo(g, ServoController.Instance.ServoGroups[0], s);
                    }

                    ServoController.Instance.ServoGroups.Remove(g);

                    RebuildUI();
                }
            });
            if (ServoController.Instance.ServoGroups.Count < 2)
                groupDeleteButton.interactable = false;

            var groupDeleteButtonTooltip = groupDeleteButton.gameObject.AddComponent<BasicTooltip>();
            groupDeleteButtonTooltip.tooltipText = "Delete Group";

            //now list servos
            for (int j = 0; j < g.Servos.Count; j++)
            {
                var s = g.Servos[j];

                Logger.Log("[NEW UI] Trying to draw servo via prefab, servo name" + s.Name);

                var newServoLine = GameObject.Instantiate(UIAssetsLoader.editorWindowServoLinePrefab);
                newServoLine.transform.SetParent(servosVLG.transform, false);

                InitEditorServoControls(newServoLine, s);

                _servoUIControls.Add(s, newServoLine);
            }
        }

        private void InitEditorServoControls(GameObject newServoLine, IServo s)
        {
            var servoBuildAidToggle = newServoLine.GetChild("ServoBuildAidToggle").GetComponent<Toggle>();
            servoBuildAidToggle.onValueChanged.AddListener(v =>
            {
                if (IRBuildAid.IRBuildAidManager.Instance == null)
                    return;

                if (v)
                {
                    IRBuildAid.IRBuildAidManager.Instance.DrawServoRange(s);
                }
                else
                {
                    IRBuildAid.IRBuildAidManager.Instance.ToggleServoRange(s);
                }
            });

            var servoBuildAidTooltip = servoBuildAidToggle.gameObject.AddComponent<BasicTooltip>();
            servoBuildAidTooltip.tooltipText = "Toggle IR BuildAid Helper";

            var servoDragHandler = newServoLine.GetChild("ServoDragHandle").AddComponent<ServoDragHandler>();
            servoDragHandler.mainCanvas = UIMasterController.Instance.appCanvas;
            servoDragHandler.background = UIAssetsLoader.spriteAssets.Find(a => a.name == "IRWindowServoFrame_Drag");

            var servoDragHandlerTooltip = servoDragHandler.gameObject.AddComponent<BasicTooltip>();
            servoDragHandlerTooltip.tooltipText = "Drag to reorder the servo\n or put it into a different group";

            var servoName = newServoLine.GetChild("ServoNameInputField").GetComponent<InputField>();
            servoName.text = s.Name;
            servoName.onEndEdit.AddListener(n => { s.Name = n; });

            var servoHighlighter = servoName.gameObject.AddComponent<ServoHighlighter>();
            servoHighlighter.servo = s;

            var servoTooltip = servoName.gameObject.AddComponent<BasicTooltip>();
            servoTooltip.tooltipText = "You can rename servos\n Names do not have to be unique";

            var servoPrevPresetButton = newServoLine.GetChild("ServoPrevPresetButton").GetComponent<Button>();
            servoPrevPresetButton.onClick.AddListener(s.Preset.MovePrev);

            var servoPrevPresetTooltip = servoPrevPresetButton.gameObject.AddComponent<BasicTooltip>();
            servoPrevPresetTooltip.tooltipText = "Move to previous preset";

            var servoPosition = newServoLine.GetChild("ServoPositionInputField").GetComponent<InputField>();
            servoPosition.text = string.Format("{0:#0.##}", s.Mechanism.Position);
            servoPosition.onEndEdit.AddListener(tmp =>
            {
                float tmpValue = 0f;
                if (float.TryParse(tmp, out tmpValue))
                {
                    tmpValue = Mathf.Clamp(tmpValue, s.Mechanism.MinPositionLimit, s.Mechanism.MaxPositionLimit);

                    if (Math.Abs(s.Mechanism.Position - tmpValue) > 0.005)
                        s.Motor.MoveTo(tmpValue);
                }
            });

            var servoNextPresetButton = newServoLine.GetChild("ServoNextPresetButton").GetComponent<Button>();
            servoNextPresetButton.onClick.AddListener(s.Preset.MoveNext);

            var servoNextPresetTooltip = servoNextPresetButton.gameObject.AddComponent<BasicTooltip>();
            servoNextPresetTooltip.tooltipText = "Move to next preset";

            var servoOpenPresetsToggle = newServoLine.GetChild("ServoOpenPresetsToggle").GetComponent<Toggle>();
            servoOpenPresetsToggle.isOn = guiPresetsWindowOpen;
            servoOpenPresetsToggle.onValueChanged.AddListener(v => { TogglePresetEditWindow(s, v, servoOpenPresetsToggle.gameObject); });

            var servoOpenPresetsToggleTooltip = servoOpenPresetsToggle.gameObject.AddComponent<BasicTooltip>();
            servoOpenPresetsToggleTooltip.tooltipText = "Open/close presets";

            var servoMoveLeftButton = newServoLine.GetChild("ServoMoveLeftButton");
            var servoMoveLeftHoldButton = servoMoveLeftButton.AddComponent<HoldButton>();
            servoMoveLeftHoldButton.callbackOnDown = s.Motor.MoveLeft;
            servoMoveLeftHoldButton.callbackOnUp = s.Motor.Stop;
            //this is needed in Editor only
            servoMoveLeftHoldButton.updateHandler = s.Motor.MoveLeft;

            var servoMoveLeftTooltip = servoMoveLeftButton.AddComponent<BasicTooltip>();
            servoMoveLeftTooltip.tooltipText = "Hold to move negative";

            var servoMoveCenterButton = newServoLine.GetChild("ServoMoveCenterButton");
            var servoMoveCenterHoldButton = servoMoveCenterButton.AddComponent<HoldButton>();
            servoMoveCenterHoldButton.callbackOnDown = s.Motor.MoveCenter;
            servoMoveCenterHoldButton.callbackOnUp = s.Motor.Stop;
            //this is needed in Editor only
            servoMoveCenterHoldButton.updateHandler = s.Motor.MoveCenter;

            var servoMoveCenterButtonTooltip = servoMoveCenterButton.AddComponent<BasicTooltip>();
            servoMoveCenterButtonTooltip.tooltipText = "Move to default position";

            var servoMoveRightButton = newServoLine.GetChild("ServoMoveRightButton");
            var servoMoveRightHoldButton = servoMoveRightButton.AddComponent<HoldButton>();
            servoMoveRightHoldButton.callbackOnDown = s.Motor.MoveRight;
            servoMoveRightHoldButton.callbackOnUp = s.Motor.Stop;
            //this is needed in Editor only
            servoMoveRightHoldButton.updateHandler = s.Motor.MoveRight;

            var servoMoveRightTooltip = servoMoveRightButton.AddComponent<BasicTooltip>();
            servoMoveRightTooltip.tooltipText = "Hold to move positive";

            var servoMovePrevGroupButton = newServoLine.GetChild("ServoMovePrevGroupButton").GetComponent<Button>();
            servoMovePrevGroupButton.onClick.AddListener(() => { MoveServoToPrevGroup(newServoLine, s); });

            var servoMovePrevGroupButtonTooltip = servoMovePrevGroupButton.gameObject.AddComponent<BasicTooltip>();
            servoMovePrevGroupButtonTooltip.tooltipText = "Move to previous group";

            var servoMoveNextGroupButton = newServoLine.GetChild("ServoMoveNextGroupButton").GetComponent<Button>();
            servoMoveNextGroupButton.onClick.AddListener(() => { MoveServoToNextGroup(newServoLine, s); });

            var servoMoveNextGroupButtonTooltip = servoMoveNextGroupButton.gameObject.AddComponent<BasicTooltip>();
            servoMoveNextGroupButtonTooltip.tooltipText = "Move to next group";

            var servoRangeMinInputField = newServoLine.GetChild("ServoRangeMinInputField").GetComponent<InputField>();
            servoRangeMinInputField.text = string.Format("{0:#0.##}", s.Mechanism.MinPositionLimit);
            servoRangeMinInputField.onEndEdit.AddListener(tmp =>
            {
                float v;
                if (float.TryParse(tmp, out v))
                    s.Mechanism.MinPositionLimit = Mathf.Clamp(v, s.Mechanism.MinPosition, s.Mechanism.MaxPosition);
            });

            var servoRangeMaxInputField = newServoLine.GetChild("ServoRangeMaxInputField").GetComponent<InputField>();
            servoRangeMaxInputField.text = string.Format("{0:#0.##}", s.Mechanism.MaxPositionLimit);
            servoRangeMaxInputField.onEndEdit.AddListener(tmp =>
            {
                float v;
                if (float.TryParse(tmp, out v))
                    s.Mechanism.MaxPositionLimit = Mathf.Clamp(v, s.Mechanism.MinPosition, s.Mechanism.MaxPosition);
            });

            var servoSpeedInputField = newServoLine.GetChild("ServoSpeedInputField").GetComponent<InputField>();
            servoSpeedInputField.text = string.Format("{0:#0.##}", s.Motor.SpeedLimit);
            servoSpeedInputField.onEndEdit.AddListener(tmp =>
            {
                float v;
                if (float.TryParse(tmp, out v))
                    s.Motor.SpeedLimit = v;
            });

            var servoAccInputField = newServoLine.GetChild("ServoAccInputField").GetComponent<InputField>();
            servoAccInputField.text = string.Format("{0:#0.##}", s.Motor.AccelerationLimit);
            servoAccInputField.onEndEdit.AddListener(tmp =>
            {
                float v;
                if (float.TryParse(tmp, out v))
                    s.Motor.AccelerationLimit = v;
            });

            var servoInvertAxisToggle = newServoLine.GetChild("ServoInvertAxisToggle").GetComponent<Toggle>();
            var servoInverAxisToggleIcon = newServoLine.GetChild("ServoInvertAxisToggle").GetChild("Icon").GetComponent<RawImage>();
            servoInvertAxisToggle.onValueChanged.AddListener(v =>
            {
                servoInvertAxisToggle.isOn = v;
                s.Motor.IsAxisInverted = v;
                servoInverAxisToggleIcon.color = (v ? Color.clear : Color.white);
            });
            //init icon state properly
            servoInvertAxisToggle.onValueChanged.Invoke(s.Motor.IsAxisInverted);

            var servoInvertAxisToggleTooltip = servoInvertAxisToggle.gameObject.AddComponent<BasicTooltip>();
            servoInvertAxisToggleTooltip.tooltipText = "Invert/uninvert servo axis";

            var servoLockToggle = newServoLine.GetChild("ServoLockToggle").GetComponent<Toggle>();
            var servoLockToggleIcon = newServoLine.GetChild("ServoLockToggle").GetChild("Icon").GetComponent<RawImage>();
            servoLockToggle.onValueChanged.AddListener(v =>
            {
                servoLockToggle.isOn = v;
                s.Mechanism.IsLocked = v;
                servoLockToggleIcon.color = (v ? Color.clear : Color.white);
            });
            //init icon state properly
            servoLockToggle.onValueChanged.Invoke(s.Mechanism.IsLocked);

            var servoLockToggleTooltip = servoLockToggle.gameObject.AddComponent<BasicTooltip>();
            servoLockToggleTooltip.tooltipText = "Lock/unlock the servo";

            var advancedModeToggle = newServoLine.GetChild("ServoShowOtherFieldsToggle").GetComponent<Toggle>();

            advancedModeToggle.onValueChanged.AddListener(v =>
            {
                //advancedModeToggle.isOn = v;

                if (v)
                {
                    //need to disable normal buttons and enable advanced ones

                    //disable normal
                    servoPrevPresetButton.gameObject.SetActive(false);
                    servoPosition.gameObject.SetActive(false);
                    servoNextPresetButton.gameObject.SetActive(false);
                    servoOpenPresetsToggle.gameObject.SetActive(false);
                    servoMoveLeftButton.gameObject.SetActive(false);
                    servoMoveCenterButton.gameObject.SetActive(false);
                    servoMoveRightButton.gameObject.SetActive(false);
                    servoMovePrevGroupButton.gameObject.SetActive(false);
                    servoMoveNextGroupButton.gameObject.SetActive(false);

                    //enable advanced
                    newServoLine.GetChild("ServoRangeLabel").SetActive(true);
                    servoRangeMinInputField.gameObject.SetActive(true);
                    servoRangeMaxInputField.gameObject.SetActive(true);
                    newServoLine.GetChild("ServoSpeedLabel").SetActive(true);
                    servoSpeedInputField.gameObject.SetActive(true);
                    newServoLine.GetChild("ServoAccLabel").SetActive(true);
                    servoAccInputField.gameObject.SetActive(true);
                    servoInvertAxisToggle.gameObject.SetActive(true);
                    servoLockToggle.gameObject.SetActive(true);
                }
                else
                {
                    //need to disable the advanced buttons and enable the normal ones

                    //disable advanced
                    newServoLine.GetChild("ServoRangeLabel").SetActive(false);
                    servoRangeMinInputField.gameObject.SetActive(false);
                    servoRangeMaxInputField.gameObject.SetActive(false);
                    newServoLine.GetChild("ServoSpeedLabel").SetActive(false);
                    servoSpeedInputField.gameObject.SetActive(false);
                    newServoLine.GetChild("ServoAccLabel").SetActive(false);
                    servoAccInputField.gameObject.SetActive(false);
                    servoInvertAxisToggle.gameObject.SetActive(false);
                    servoLockToggle.gameObject.SetActive(false);

                    //enable normal
                    servoPrevPresetButton.gameObject.SetActive(true);
                    servoPosition.gameObject.SetActive(true);
                    servoNextPresetButton.gameObject.SetActive(true);
                    servoOpenPresetsToggle.gameObject.SetActive(true);
                    servoMoveLeftButton.gameObject.SetActive(true);
                    servoMoveCenterButton.gameObject.SetActive(true);
                    servoMoveRightButton.gameObject.SetActive(true);
                    servoMovePrevGroupButton.gameObject.SetActive(true);
                    servoMoveNextGroupButton.gameObject.SetActive(true);
                }
            });

            var advancedModeToggleTooltip = advancedModeToggle.gameObject.AddComponent<BasicTooltip>();
            advancedModeToggleTooltip.tooltipText = "Show advanced fields";

        }

        public void MoveServoToPrevGroup(GameObject servoLine, IServo s)
        {
            var currentGroupIndex = ServoController.Instance.ServoGroups.FindIndex(g => g.Servos.Contains(s));
            if(currentGroupIndex  < 1)
            {
                //error
                return;
            }

            var prevGroup = ServoController.Instance.ServoGroups[currentGroupIndex - 1];

            var prevGroupUIControls = _servoGroupUIControls[prevGroup];
            var servoUIControls = _servoUIControls[s];
            if (prevGroupUIControls == null || servoUIControls == null)
            {
                //error
                return;
            }

            //later consider puting animation here, for now just change parents
            ServoController.MoveServo(ServoController.Instance.ServoGroups[currentGroupIndex], ServoController.Instance.ServoGroups[currentGroupIndex - 1], s);
            servoUIControls.transform.SetParent(prevGroupUIControls.GetChild("ServoGroupServosVLG").transform, false);
        }

        public void MoveServoToNextGroup(GameObject servoLine, IServo s)
        {
            var currentGroupIndex = ServoController.Instance.ServoGroups.FindIndex(g => g.Servos.Contains(s));
            if (currentGroupIndex < 0)
            {
                //error
                return;
            }

            var nextGroup = ServoController.Instance.ServoGroups[currentGroupIndex + 1];

            var nextGroupUIControls = _servoGroupUIControls[nextGroup];
            var servoUIControls = _servoUIControls[s];
            if (nextGroupUIControls == null || servoUIControls == null)
            {
                //error
                return;
            }

            //later consider puting animation here, for now just change parents
            ServoController.MoveServo(ServoController.Instance.ServoGroups[currentGroupIndex], ServoController.Instance.ServoGroups[currentGroupIndex + 1], s);
            servoUIControls.transform.SetParent(nextGroupUIControls.GetChild("ServoGroupServosVLG").transform, false);
        }
        public void ShowServoAdvancedMode(IServo servo, bool value)
        {
            var servoControls = _servoUIControls[servo];
            if (servoControls != null)
            {
                servoControls.GetChild("ServoShowOtherFieldsToggle").GetComponent<Toggle>().onValueChanged.Invoke(value);
            }
        }
        public void RebuildUI()
        {
            if (_controlWindow)
            {
                _controlWindowPosition = _controlWindow.transform.position;
                _controlWindow.DestroyGameObjectImmediate();
                _controlWindow = null;
            }
            
            if (_editorWindow)
            {
                _editorWindowPosition = _editorWindow.transform.position;
                _editorWindowSize = _editorWindow.GetComponent<RectTransform>().sizeDelta;
                _editorWindow.DestroyGameObjectImmediate();
                _editorWindow = null;
            }
                
            if (_uiSettingsWindow)
                _uiSettingsWindowPosition = _uiSettingsWindow.transform.position;
            //should be called by ServoController when required (Vessel changed and such).

            _servoGroupUIControls?.Clear();
            _servoUIControls?.Clear();

            if (!ServoController.APIReady)
                return;


            if (UIAssetsLoader.uiSettingsWindowPrefabReady && _uiSettingsWindow == null)
            {
                InitUISettingsWindow();
            }

            if (HighLogic.LoadedSceneIsFlight && !guiFlightEditorWindowOpen)
            {
                //here we need to wait until prefabs become available and then Instatiate the window
                if (UIAssetsLoader.controlWindowPrefabReady && _controlWindow == null)
                {
                    InitFlightControlWindow(GUIEnabled);
                }
                
                GameObject servoGroupsArea = _controlWindow.GetChild("WindowContent").GetChild("ServoGroupsVLG");

                for (int i = 0; i < ServoController.Instance.ServoGroups.Count; i++)
                {
                    Logger.Log("[NEW UI] Trying to draw group via prefab");
                    ServoController.ControlGroup g = ServoController.Instance.ServoGroups[i];

                    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != g.Vessel)
                        continue;

                    var newServoGroupLine = GameObject.Instantiate(UIAssetsLoader.controlWindowGroupLinePrefab);
                    newServoGroupLine.transform.SetParent(servoGroupsArea.transform, false);

                    InitFlightGroupControls(newServoGroupLine, g);
                        
                    _servoGroupUIControls.Add(g, newServoGroupLine);
                }
            }
            else
            {
                //we are in Editor

                if (UIAssetsLoader.editorWindowPrefabReady && _editorWindow == null)
                {
                    InitEditorWindow(GUIEnabled);
                }
                
                GameObject servoGroupsArea = _editorWindow.GetChild("WindowContent").GetChild("Scroll View").GetChild("Viewport").GetChild("Content").GetChild("ServoGroupsVLG");
                servoGroupsArea.AddComponent<GroupDropHandler>();

                for (int i = 0; i < ServoController.Instance.ServoGroups.Count; i++)
                {
                    Logger.Log("[NEW UI] Trying to draw group via prefab");
                    ServoController.ControlGroup g = ServoController.Instance.ServoGroups[i];

                    var newServoGroupLine = GameObject.Instantiate(UIAssetsLoader.editorWindowGroupLinePrefab);
                    newServoGroupLine.transform.SetParent(servoGroupsArea.transform, false);

                    InitEditorGroupControls(newServoGroupLine, g);

                    _servoGroupUIControls.Add(g, newServoGroupLine);
                }
            }
        }

        public void UpdateServoReadoutsFlight(IServo s, GameObject servoUIControls)
        {
            var servoStatusLight = servoUIControls.GetChild("ServoStatusRawImage").GetComponent<RawImage>();
            if (s.Mechanism.IsLocked)
            {
                servoStatusLight.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Locked");
            }
            else if(s.Mechanism.IsMoving)
            {
                servoStatusLight.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Active");
            }
            else
            {
                servoStatusLight.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Idle");
            }

            var servoName = servoUIControls.GetChild("ServoNameText").GetComponent<Text>();
            servoName.text = s.Name;
             
            var servoPosition = servoUIControls.GetChild("ServoPositionText").GetComponent<Text>();
            servoPosition.text = string.Format("{0:#0.##}", s.Mechanism.Position);

            var servoLockToggle = servoUIControls.GetChild("ServoLockToggleButton").GetComponent<Toggle>();
            if(servoLockToggle.isOn != s.Mechanism.IsLocked)
                servoLockToggle.onValueChanged.Invoke (s.Mechanism.IsLocked);
            
            var servoInvertAxisToggle = servoUIControls.GetChild("ServoInvertAxisToggleButton").GetComponent<Toggle>();
            if (servoInvertAxisToggle.isOn != s.Motor.IsAxisInverted)
                servoInvertAxisToggle.onValueChanged.Invoke (s.Motor.IsAxisInverted);
            
        }

        public void UpdateServoReadoutsEditor(IServo s, GameObject servoUIControls)
        {
            var servoPosition = servoUIControls.GetChild("ServoPositionInputField").GetComponent<InputField>();
            if(!servoPosition.isFocused)
                servoPosition.text = string.Format("{0:#0.##}", s.Mechanism.Position);

            var servoLockToggle = servoUIControls.GetChild("ServoLockToggle").GetComponent<Toggle>();
            if (s.Mechanism.IsLocked != servoLockToggle.isOn)
                servoLockToggle.onValueChanged.Invoke(s.Mechanism.IsLocked);
            
            var servoInvertAxisToggle = servoUIControls.GetChild("ServoInvertAxisToggle").GetComponent<Toggle>();
            if (s.Motor.IsAxisInverted != servoInvertAxisToggle.isOn)
                servoLockToggle.onValueChanged.Invoke(s.Mechanism.IsLocked);

        }

        public void ShowIRWindow()
        {
            RebuildUI();

            if(HighLogic.LoadedSceneIsEditor || guiFlightEditorWindowOpen)
            {
                _editorWindowFader.FadeTo(_UIAlphaValue, 0.1f, () => { GUIEnabled = true; appLauncherButton.SetTrue(false); });
            }
            else
            {
                _controlWindowFader.FadeTo(_UIAlphaValue, 0.1f, () => { GUIEnabled = true; appLauncherButton.SetTrue(false); });
            }

        }

        public void HideIRWindow()
        {
            if(HighLogic.LoadedSceneIsEditor || guiFlightEditorWindowOpen)
            {
                if (_editorWindowFader)
                    _editorWindowFader.FadeTo(0f, 0.1f, () => { GUIEnabled = false; appLauncherButton.SetFalse(false); });
                else
                    GUIEnabled = false;
            }
            else
            {
                if (_controlWindowFader)
                    _controlWindowFader.FadeTo(0f, 0.1f, () => { GUIEnabled = false; appLauncherButton.SetFalse(false); });
                else
                    GUIEnabled = false;
            }

            if(_uiSettingsWindow)
            {
                _uiSettingsWindowFader.FadeTo (0f, 0.1f);
                _uiSettingsWindow.SetActive(false);
            }

            if(_presetsWindow && guiPresetsWindowOpen)
            {
                _presetsWindow.DestroyGameObject ();
                _presetsWindow = null;
                _presetsWindowFader = null;
                guiPresetsWindowOpen = false;
            }
        }

        public void Update()
        {
            if (!ServoController.APIReady || !UIAssetsLoader.controlWindowPrefabReady)
            {
                GUIEnabled = false;
                appLauncherButton.SetFalse();
                return;
            }

            if(guiRebuildPending && GUIEnabled)
            {
                RebuildUI();
                guiRebuildPending = !guiRebuildPending;

            }
            
            if (!GUIEnabled)
                return;

            if(EventSystem.current.currentSelectedGameObject != null && 
                (EventSystem.current.currentSelectedGameObject.name == "GroupNameInputField"
                || EventSystem.current.currentSelectedGameObject.name == "GroupMoveLeftKey"
                || EventSystem.current.currentSelectedGameObject.name == "GroupMoveRightKey"
                || EventSystem.current.currentSelectedGameObject.name == "ServoNameInputField"
                || EventSystem.current.currentSelectedGameObject.name == "ServoPositionInputField"
                || EventSystem.current.currentSelectedGameObject.name == "NewGroupNameInputField"
                || EventSystem.current.currentSelectedGameObject.name == "ServoGroupSpeedMultiplier"))
            {
                if(!isKeyboardLocked)
                    KeyboardLock(true); 
            }
            else
            {
                if(isKeyboardLocked)
                    KeyboardLock(false);
            }
            
            //at this point we should have windows instantiated and filled with groups and servos
            //all we need to do is update the fields
            if (HighLogic.LoadedSceneIsFlight && !guiFlightEditorWindowOpen)
            {
                //here we need to update servo statuses, servo positions and status of Locked and Inverted
                if (GUIEnabled && _controlWindow == null)
                {
                    RebuildUI();
                }

                foreach (var pair in _servoUIControls)
                {
                    if (!pair.Value.activeInHierarchy)
                        continue;
                    UpdateServoReadoutsFlight(pair.Key, pair.Value);
                }
            }
            else 
            {
                if(GUIEnabled && _editorWindow == null)
                {
                    RebuildUI();
                }
                //editor mode

                //here we need to update servo statuses, servo positions and status of Locked and Inverted
                foreach (var pair in _servoUIControls)
                {
                    if (!pair.Value.activeInHierarchy)
                        continue;
                    UpdateServoReadoutsEditor(pair.Key, pair.Value);
                }
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
                appLauncherButtonTexture = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_button");
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                    ShowIRWindow, //onTrue
                    HideIRWindow, //onFalse
                    null, //onHover
                    null, //onHoverOut
                    null, //onEnable
                    null, //inDisable
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
            HideIRWindow();
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

        private void OnSave()
        {
            SaveConfigXml();
        }

        private void OnDestroy()
        {
            Logger.Log("[GUI] destroy");

            KeyboardLock(false);
            SaveConfigXml();

            if(_controlWindow)
            {
                _controlWindow.DestroyGameObject ();
                _controlWindow = null;
                _controlWindowFader = null;
            }

            if(_editorWindow)
            {
                _editorWindow.DestroyGameObject ();
                _editorWindow = null;
                _editorWindowFader = null;
            }

            if(_uiSettingsWindow)
            {
                _uiSettingsWindow.DestroyGameObject ();
                _uiSettingsWindow = null;
                _uiSettingsWindowFader = null;
            }

            if(_presetsWindow)
            {
                _presetsWindow.DestroyGameObject ();
                _presetsWindow = null;
                _presetsWindowFader = null;
            }

            GameEvents.onGUIApplicationLauncherReady.Remove (AddAppLauncherButton);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
            DestroyAppLauncherButton();

            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);

            Logger.Log("[GUI] OnDestroy finished successfully", Logger.Level.Debug);
        }


        /// <summary>
        ///     Applies or removes the lock
        /// </summary>
        /// <param name="apply">Which way are we going</param>
        internal void KeyboardLock(Boolean apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (apply)
            {
                //only add a new lock if there isnt already one there
                if (InputLockManager.GetControlLock("IRKeyboardLock") != ControlTypes.KEYBOARDINPUT)
                {
                    Logger.Log(String.Format("[GUI] AddingLock-{0}", "IRKeyboardLock"), Logger.Level.Debug);

                    InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, "IRKeyboardLock");
                }
            }
            //Otherwise make sure the lock is removed
            else 
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("IRKeyboardLock") == ControlTypes.KEYBOARDINPUT)
                {
                    Logger.Log(String.Format("[GUI] Removing-{0}", "IRKeyboardLock"), Logger.Level.Debug);
                    InputLockManager.RemoveControlLock("IRKeyboardLock");
                }
            }

            isKeyboardLocked = apply;
        }
        
        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
            config.load();

            _controlWindowPosition = config.GetValue<Vector3>("controlWindowPosition");
            _editorWindowPosition = config.GetValue<Vector3>("editorWindowPosition");
            _editorWindowSize = config.GetValue<Vector2>("editorWindowSize");
            _uiSettingsWindowPosition = config.GetValue<Vector3>("uiSettingsWindowPosition");
            _UIAlphaValue = config.GetValue<float>("UIAlphaValue", 0.8f);
            _UIScaleValue = config.GetValue<float>("UIScaleValue", 1.0f);
            UseElectricCharge = config.GetValue<bool>("useEC", true);
        }

        public void SaveConfigXml()
        {
            if(_controlWindow)
                _controlWindowPosition = _controlWindow.transform.position;

            if(_editorWindow)
            {
                _editorWindowPosition = _editorWindow.transform.position;
                _editorWindowSize = _editorWindow.GetComponent<RectTransform> ().sizeDelta;
            }
            if(_uiSettingsWindow)
            {
                _uiSettingsWindowPosition = _uiSettingsWindow.transform.position;
            }

            PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
            config.SetValue("controlWindowPosition", _controlWindowPosition);
            config.SetValue("editorWindowPosition", _editorWindowPosition);
            config.SetValue("editorWindowSize", _editorWindowSize);
            config.SetValue("uiSettingsWindowPosition", _uiSettingsWindowPosition);
            config.SetValue("UIAlphaValue", _UIAlphaValue.ToString());
            config.SetValue("UIScaleValue", _UIScaleValue.ToString());
            config.SetValue("useEC", UseElectricCharge);

            config.save();
        }
    }
}