using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using InfernalRobotics_v3.Command;
using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Module;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI;

namespace InfernalRobotics_v3.Gui
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

		private bool GUIHidden = false;

		// windows
		private static GameObject _controlWindow;
		private static Vector3 _controlWindowPosition;
		private static CanvasGroupFader _controlWindowFader;

		private bool guiFlightPresetModeOn;

		private static GameObject _editorWindow;
		private static Vector3 _editorWindowPosition;
		private static Vector2 _editorWindowSize;
		private static CanvasGroupFader _editorWindowFader;

		private static ServoGroup _editorPartSelectorGroup;

		enum guiMode { Control, Editor };
		private guiMode _mode;

		// popups
		private static GameObject _settingsWindow;
		private static Vector3 _settingsWindowPosition;
		private static CanvasGroupFader _settingsWindowFader;

		private bool guiSettingsWindowOpen;

		private static GameObject _presetsWindow;
		private static Vector3 _presetsWindowPosition;
		private static CanvasGroupFader _presetsWindowFader;
		private static IServoGroup presetWindowServoGroup;
		private static IServo presetWindowServo;

		private bool guiPresetsWindowOpen;

		// servos
		internal static Dictionary<IServoGroup, GameObject> _servoGroupUIControls;

		internal class servoUIControl { public IServo s; public GameObject ui; };
		internal class servoUIControlComparer : IEqualityComparer<servoUIControl>
		{
			public bool Equals(servoUIControl l, servoUIControl r)
			{
				return (l.s == r.s) && (l.ui == r.ui);
			}

			public int GetHashCode(servoUIControl e)
			{
				return e.ui.GetHashCode();
			}
		}

		internal static HashSet<servoUIControl> _servoUIControls;

		// settings
		public static float _UIAlphaValue = 0.8f;
		public static float _UIScaleValue = 1.0f;
		private const float UI_FADE_TIME = 0.1f;
		private const float UI_MIN_ALPHA = 0.2f;
		private const float UI_MIN_SCALE = 0.5f;
		private const float UI_MAX_SCALE = 2.0f;

		private static bool bInvalid = false;

		internal void Invalidate()
		{
			bInvalid = true;
			if(appLauncherButton != null)
			{
				GUIEnabled = appLauncherButton.toggleButton.CurrentState == UIRadioButton.State.True;
				appLauncherButton.VisibleInScenes =
					Controller.APIReady ?(ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH) : ApplicationLauncher.AppScenes.NEVER;
			}
			else
				GUIEnabled = false;
		}

		private ApplicationLauncherButton appLauncherButton;

		internal static Color ir_yellow = new Color(255, 194, 0, 255);

		public static bool UseElectricCharge = true;

		internal bool GUIEnabled = false;

		private static bool isKeyboardLocked = false;

		// editor
		internal static bool _bIsAdvancedModeOn = false;
		internal static bool _bIsBuildAidOn = false;


		public static WindowManager Instance
		{
			get { return _instance; }
		}

		private void Awake()
		{
			LoadConfigXml();

			Logger.Log("[NewGUI] awake, Mode: " + AddonName);

			if(HighLogic.LoadedSceneIsFlight)
				_mode = guiMode.Control;
			else if(HighLogic.LoadedSceneIsEditor)
				_mode = guiMode.Editor;
			else
			{
				_instance = null;
				// actually we don't need to go further if it's not flight or editor
				return;
			}

			_instance = this;

			_servoGroupUIControls = new Dictionary<IServoGroup, GameObject>();
			_servoUIControls = new HashSet<servoUIControl>(new servoUIControlComparer());

			GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);
			GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncherButton);

			Logger.Log("[GUI] Added Toolbar GameEvents Handlers", Logger.Level.Debug);

			GameEvents.onShowUI.Add(OnShowUI);
			GameEvents.onHideUI.Add(OnHideUI);

			Logger.Log("[GUI] awake finished successfully", Logger.Level.Debug);
		}

		private void OnShowUI()
		{
			if(GUIHidden)
			{
				GUIHidden = false;
				ShowIRWindow();
			}
		}

		private void OnHideUI()
		{
			if(GUIHidden = GUIEnabled)
				HideIRWindow();
		}

		private void SetGlobalAlpha(float newAlpha)
		{
			_UIAlphaValue = Mathf.Clamp(newAlpha, UI_MIN_ALPHA, 1.0f);

			if(_controlWindow)
				_controlWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;

			if(_settingsWindow)
			{
				_settingsWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;

				var alphaText = _settingsWindow.GetChild("WindowContent").GetChild("UITransparencySliderHLG").GetChild("TransparencyValue").GetComponent<Text>();
				alphaText.text = string.Format("{0:#0.##}", _UIAlphaValue);
			}

			if(_editorWindow)
				_editorWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;
		}

		private void SetGlobalScale(float newScale)
		{
			newScale = Mathf.Clamp(newScale, UI_MIN_SCALE, UI_MAX_SCALE);

			if(_controlWindow)
				_controlWindow.transform.localScale = Vector3.one * newScale;

			if(_settingsWindow)
			{
				_settingsWindow.transform.localScale = Vector3.one * newScale;

				var scaleText = _settingsWindow.GetChild("WindowContent").GetChild("UIScaleSliderHLG").GetChild("ScaleValue").GetComponent<Text>();
				scaleText.text =  string.Format("{0:#0.##}", newScale);
			}

			if(_editorWindow)
				_editorWindow.transform.localScale = Vector3.one * newScale;

			if(_presetsWindow)
				_presetsWindow.transform.localScale = Vector3.one * newScale;

			_UIScaleValue = newScale;
		}

		////////////////////////////////////////
		// Control

		private void InitFlightControlWindow(bool startSolid = true)
		{
			_controlWindow = GameObject.Instantiate(UIAssetsLoader.controlWindowPrefab);
			_controlWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
			_controlWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
			_controlWindowFader = _controlWindow.AddComponent<CanvasGroupFader>();

			// start invisible to be toggled later
			if(!startSolid)
				_controlWindow.GetComponent<CanvasGroup>().alpha = 0f;

			if(_controlWindowPosition == Vector3.zero)
				_controlWindowPosition = _controlWindow.transform.position; // get the default position from the prefab
			else
				_controlWindow.transform.position = ClampWindowPosition(_controlWindowPosition);

			var settingsButton = _controlWindow.GetChild("WindowTitle").GetChild("LeftWindowButton");
			if(settingsButton != null)
			{
				settingsButton.GetComponent<Button>().onClick.AddListener(ToggleSettingsWindow);
				var t = settingsButton.AddComponent<BasicTooltip>();
				t.tooltipText = "Show/hide UI settings";
			}

			var closeButton = _controlWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
			if(closeButton != null)
			{
				closeButton.GetComponent<Button>().onClick.AddListener(OnHideCallback);
				var t = closeButton.AddComponent<BasicTooltip>();
				t.tooltipText = "Close window";
			}

			var flightWindowFooterButtons = _controlWindow.GetChild("WindowFooter").GetChild("FlightWindowFooterButtonsHLG");
			var openEditorButton = flightWindowFooterButtons.GetChild("EditGroupsButton").GetComponent<Button>();
			openEditorButton.onClick.AddListener(ToggleFlightEditor);

			var openEditorButtonTooltip = openEditorButton.gameObject.AddComponent<BasicTooltip>();
			openEditorButtonTooltip.tooltipText = "Switch to Editor Mode";

			var presetModeToggle = flightWindowFooterButtons.GetChild("PresetModeButton").GetComponent<Toggle>();
			presetModeToggle.isOn = guiFlightPresetModeOn;
			presetModeToggle.onValueChanged.AddListener(ToggleFlightPresetMode);

			var presetModeTooltip = presetModeToggle.gameObject.AddComponent<BasicTooltip>();
			presetModeTooltip.tooltipText = "Toggle Preset Mode";

			var stopAllButton = flightWindowFooterButtons.GetChild("StopAllButton").GetComponent<Button>();
			stopAllButton.onClick.AddListener(() =>
				{
					foreach(var pair in _servoGroupUIControls)
						pair.Key.Stop();
				});

			var stopAllTooltip = stopAllButton.gameObject.AddComponent<BasicTooltip>();
			stopAllTooltip.tooltipText = "Panic! Stop all servos!";

			// toggle preset mode if needed
			ToggleFlightPresetMode(guiFlightPresetModeOn);
		}

		private void InitFlightGroupControls(GameObject newServoGroupLine, IServoGroup g)
		{
			var hlg = newServoGroupLine.GetChild("ServoGroupControlsHLG");
			var servosVLG = newServoGroupLine.GetChild("ServoGroupServosVLG");

			hlg.GetChild("ServoGroupNameText").GetComponent<Text>().text = g.Name;
			
			var groupToggle = hlg.GetChild("ServoGroupExpandedStatusToggle").GetComponent<Toggle>();
			var groupLockToggleIcon = hlg.GetChild("ServoGroupExpandedStatusToggle").GetChild("Icon").GetComponent<RawImage>();
			groupToggle.onValueChanged.AddListener(v =>
				{
					groupLockToggleIcon.texture = UIAssetsLoader.iconAssets.Find(i => i.name ==(g.Expanded ? "expand" : "collapse"));
				
					g.Expanded = v;
					servosVLG.SetActive(g.Expanded);
				});

			if(g.Expanded)
			{
				groupToggle.isOn = true;
				groupLockToggleIcon.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "collapse");
			}
			servosVLG.SetActive(g.Expanded);

			var groupExpandTooltip = groupToggle.gameObject.AddComponent<BasicTooltip>();
			groupExpandTooltip.tooltipText = "Show/hide group's servos";
			
			var groupSpeed = hlg.GetChild("ServoGroupSpeedMultiplier").GetComponent<InputField>();
			groupSpeed.text = string.Format("{0:#0.##}", g.GroupSpeedFactor);
			groupSpeed.onEndEdit.AddListener(v => { float parsedV; float.TryParse(v, out parsedV); g.GroupSpeedFactor = parsedV; });

			var groupSpeedTooltip = groupSpeed.gameObject.AddComponent<BasicTooltip>();
			groupSpeedTooltip.tooltipText = "Speed Multiplier";

			var groupMoveLeftToggle = hlg.GetChild("ServoGroupMoveLeftToggleButton").GetComponent<Toggle>();
			groupMoveLeftToggle.onValueChanged.AddListener(v =>
				{
					g.Stop();
					if(v) {
						hlg.GetChild("ServoGroupMoveRightToggleButton").GetComponent<Toggle>().isOn = false;
						g.MoveLeft();
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
			groupMoveCenterTooltip.tooltipText = "Hold to move\nto default position";

			var groupMoveRightButton = hlg.GetChild("ServoGroupMoveRightButton");
			var groupMoveRightHoldButton = groupMoveRightButton.AddComponent<HoldButton>();
			groupMoveRightHoldButton.callbackOnDown = g.MoveRight;
			groupMoveRightHoldButton.callbackOnUp = g.Stop;

			var groupMoveRightTooltip = groupMoveRightButton.AddComponent<BasicTooltip>();
			groupMoveRightTooltip.tooltipText = "Hold to move positive";

			var groupMoveRightToggle = hlg.GetChild("ServoGroupMoveRightToggleButton").GetComponent<Toggle>();
			groupMoveRightToggle.onValueChanged.AddListener(v =>
				{
					g.Stop();
					if(v)
					{
						hlg.GetChild("ServoGroupMoveLeftToggleButton").GetComponent<Toggle>().isOn = false;
						g.MoveRight();
					}
				});

			var groupMoveRightToggleTooltip = groupMoveRightToggle.gameObject.AddComponent<BasicTooltip>();
			groupMoveRightToggleTooltip.tooltipText = "Toggle positive movement";

			var groupMovePrevPresetButton = hlg.GetChild("ServoGroupMovePrevPresetButton").GetComponent<Button>();
			groupMovePrevPresetButton.onClick.AddListener(g.MovePrevPreset);

			var groupMovePrevPresetTooltip = groupMovePrevPresetButton.gameObject.AddComponent<BasicTooltip>();
			groupMovePrevPresetTooltip.tooltipText = "Move to previous preset";

			var groupRevertButton = hlg.GetChild("ServoGroupRevertButton").GetComponent<Button>();
			groupRevertButton.onClick.AddListener(g.MoveCenter);

			var groupRevertTooltip = groupRevertButton.gameObject.AddComponent<BasicTooltip>();
			groupRevertTooltip.tooltipText = "Move to default position";

			var groupMoveNextPresetButton = hlg.GetChild("ServoGroupMoveNextPresetButton").GetComponent<Button>();
			groupMoveNextPresetButton.onClick.AddListener(g.MoveNextPreset);

			var groupMoveNextPresetTooltip = groupMoveNextPresetButton.gameObject.AddComponent<BasicTooltip>();
			groupMoveNextPresetTooltip.tooltipText = "Move to next preset";

			// IK

			var ikLimiterButton = hlg.GetChild("IKLimiterButton");
			var ikLimiterToggle = ikLimiterButton.GetComponent<Toggle>();
			if(Controller._IKModule != null)
				ikLimiterToggle.isOn = Controller._IKModule.GetLimiter(g.group);
			ikLimiterToggle.onValueChanged.AddListener(v =>
				{ Controller._IKModule.SetLimiter(g.group, v); });

			var ikDirectModeButton = hlg.GetChild("IKDirectModeButton");
			var ikDirectModeToggle = ikDirectModeButton.GetComponent<Toggle>();
			if(Controller._IKModule != null)
				ikLimiterToggle.isOn = Controller._IKModule.GetDirectMode(g.group);
			ikDirectModeToggle.onValueChanged.AddListener(v =>
				{ Controller._IKModule.SetDirectMode(g.group, v); });

			var ikEndEffectorButton = hlg.GetChild("IKEndEffectorButton").GetComponent<Button>();
			ikEndEffectorButton.onClick.AddListener(() => { Controller._IKModule.SelectEndEffector(g.group); });

			var ikAction1Button = hlg.GetChild("IKAction1Button").GetComponent<Button>();
			ikAction1Button.onClick.AddListener(() => { Controller._IKModule.Action1(g.group); });

			var ikAction2Button = hlg.GetChild("IKAction2Button").GetComponent<Button>();
			ikAction2Button.onClick.AddListener(() => { Controller._IKModule.Action2(g.group); });

			var ikModeToggleButton = hlg.GetChild("IKModeToggleButton");
			var ikModeToggleToggle = ikModeToggleButton.GetComponent<Toggle>();
			if(Controller._IKModule != null)
				ikModeToggleToggle.isOn = Controller._IKServoGroup == g.group;
			ikModeToggleToggle.onValueChanged.AddListener(v =>
				{
					if(v)
					{
						ToggleIKMode(g.group, true);
						Controller._IKServoGroup = g.group;
						UpdateIKButtons();
						Controller._IKModule.SelectActiveGroup(g.group);
					}
					else
					{
						ToggleIKMode(g.group, false);
						Controller._IKServoGroup = null;
						UpdateIKButtons();
						Controller._IKModule.SelectActiveGroup(null);
					}
				});

			// now list servos
			for(int j = 0; j < g.Servos.Count; j++)
			{
				var s = g.Servos[j];

				if(s.IsFreeMoving || !s.IsServo)
					continue;

				var newServoLine = GameObject.Instantiate(UIAssetsLoader.controlWindowServoLinePrefab);
				newServoLine.transform.SetParent(servosVLG.transform, false);

				InitFlightServoControls(newServoLine, g, Controller.Instance.GetInterceptor(s));

				_servoUIControls.Add(new servoUIControl { s = Controller.Instance.GetInterceptor(s), ui = newServoLine });
			}
		}

		public void UpdateIKButtons()
		{
			if(Controller._IKModule == null)
			{
				foreach(var v in _servoGroupUIControls)
				{
					ToggleIKMode(v.Key, false);
					v.Value.GetChild("ServoGroupControlsHLG").GetChild("IKModeToggleButton").SetActive(false);
				}
			}
			else
			{
				foreach(var v in _servoGroupUIControls)
				{
					var button = v.Value.GetChild("ServoGroupControlsHLG").GetChild("IKModeToggleButton");
					button.SetActive(true);

					button.GetComponent<Toggle>().interactable = ((Controller._IKServoGroup == null) || (v.Key == Controller._IKServoGroup));
				}
			}
		}

		public void ToggleIKMode(IServoGroup g, bool active)
		{
			var hlg = _servoGroupUIControls[g.group].GetChild("ServoGroupControlsHLG");

			hlg.GetChild("ServoGroupMoveLeftToggleButton").SetActive(!active);
			hlg.GetChild("ServoGroupMoveLeftButton").SetActive(!active);
			hlg.GetChild("ServoGroupMoveCenterButton").SetActive(!active);
			hlg.GetChild("ServoGroupMoveRightButton").SetActive(!active);
			hlg.GetChild("ServoGroupMoveRightToggleButton").SetActive(!active);

		//	hlg.GetChild("ServoGroupMovePrevPresetButton").SetActive(!active);
		//	hlg.GetChild("ServoGroupRevertButton").SetActive(!active);
		//	hlg.GetChild("ServoGroupMoveNextPresetButton").SetActive(!active);

			hlg.GetChild("IKLimiterButton").SetActive(active);
			hlg.GetChild("IKDirectModeButton").SetActive(active);
			hlg.GetChild("IKEndEffectorButton").SetActive(active);
			hlg.GetChild("IKAction1Button").SetActive(active);
			hlg.GetChild("IKAction2Button").SetActive(active);
		}

		private void InitFlightServoControls(GameObject newServoLine, IServoGroup g, IServo s)
		{
			var servoStatusLight = newServoLine.GetChild("ServoStatusRawImage").GetComponent<RawImage>();
			if(s.IsLocked)
			{
				var redDot = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Locked");
				if(redDot != null)
					servoStatusLight.texture = redDot;
			}

			var servoName = newServoLine.GetChild("ServoNameText").GetComponent<Text>();
			servoName.text = s.Name;
			var highlighter = servoName.gameObject.AddComponent<ServoHighlighter>();
			highlighter.servo = s.servo;

			var servoPosition = newServoLine.GetChild("ServoPositionText").GetComponent<Text>();
			servoPosition.text = string.Format("{0:#0.00}", s.CommandedPosition);

			var servoLockToggle = newServoLine.GetChild("ServoLockToggleButton").GetComponent<Toggle>();
			servoLockToggle.isOn = s.IsLocked;
			servoLockToggle.onValueChanged.AddListener(v =>
				{
					s.IsLocked = v;
					servoLockToggle.isOn = v;
				});

			var servoLockToggleTooltip = servoLockToggle.gameObject.AddComponent<BasicTooltip>();
			servoLockToggleTooltip.tooltipText = "Lock/unlock the servo";

			var servoMoveLeftButton = newServoLine.GetChild("ServoMoveLeftButton");
			var servoMoveLeftHoldButton = servoMoveLeftButton.AddComponent<HoldButton>();
			servoMoveLeftHoldButton.callbackOnDown = () => s.MoveLeft(s.DefaultSpeed * g.GroupSpeedFactor);
			servoMoveLeftHoldButton.callbackOnUp = s.Stop;

			var servoMoveLeftTooltip = servoMoveLeftButton.AddComponent<BasicTooltip>();
			servoMoveLeftTooltip.tooltipText = "Hold to move negative";

			var servoMoveCenterButton = newServoLine.GetChild("ServoMoveCenterButton");
			var servoMoveCenterHoldButton = servoMoveCenterButton.AddComponent<HoldButton>();
			servoMoveCenterHoldButton.callbackOnDown = () => s.MoveCenter(s.DefaultSpeed * g.GroupSpeedFactor);
			servoMoveCenterHoldButton.callbackOnUp = s.Stop;

			var servoMoveCenterTooltip = servoMoveCenterButton.AddComponent<BasicTooltip>();
			servoMoveCenterTooltip.tooltipText = "Hold to move\n to default position";

			var servoMoveRightButton = newServoLine.GetChild("ServoMoveRightButton");
			var servoMoveRightHoldButton = servoMoveRightButton.AddComponent<HoldButton>();
			servoMoveRightHoldButton.callbackOnDown = () => s.MoveRight(s.DefaultSpeed * g.GroupSpeedFactor);
			servoMoveRightHoldButton.callbackOnUp = s.Stop;

			var servoMoveRightTooltip = servoMoveRightButton.AddComponent<BasicTooltip>();
			servoMoveRightTooltip.tooltipText = "Hold to move positive";

			var servoInvertAxisToggle = newServoLine.GetChild("ServoInvertAxisToggleButton").GetComponent<Toggle>();
			servoInvertAxisToggle.isOn = s.IsInverted;
			servoInvertAxisToggle.onValueChanged.AddListener(v =>
				{
					s.IsInverted = v;
					servoInvertAxisToggle.isOn = v;
				});

			var servoInvertAxisToggleTooltip = servoInvertAxisToggle.gameObject.AddComponent<BasicTooltip>();
			servoInvertAxisToggleTooltip.tooltipText = "Invert/uninvert servo axis";

			var servoPrevPresetButton = newServoLine.GetChild("ServoMovePrevPresetButton").GetComponent<Button>();
			servoPrevPresetButton.onClick.AddListener(() => s.Presets.MovePrev(s.DefaultSpeed * g.GroupSpeedFactor));

			var servoPrevPresetTooltip = servoPrevPresetButton.gameObject.AddComponent<BasicTooltip>();
			servoPrevPresetTooltip.tooltipText = "Move to previous preset";

			var servoOpenPresetsToggle = newServoLine.GetChild("ServoOpenPresetsToggle").GetComponent<Toggle>();
			servoOpenPresetsToggle.onValueChanged.AddListener(v => TogglePresetEditWindow(g, s, v, servoOpenPresetsToggle.gameObject));

			var servoOpenPresetsToggleTooltip = servoOpenPresetsToggle.gameObject.AddComponent<BasicTooltip>();
			servoOpenPresetsToggleTooltip.tooltipText = "Open/close presets";

			var servoNextPresetButton = newServoLine.GetChild("ServoMoveNextPresetButton").GetComponent<Button>();
			servoNextPresetButton.onClick.AddListener(() => s.Presets.MoveNext(s.DefaultSpeed * g.GroupSpeedFactor));

			var servoNextPresetTooltip = servoNextPresetButton.gameObject.AddComponent<BasicTooltip>();
			servoNextPresetTooltip.tooltipText = "Move to next preset";
		}

		////////////////////////////////////////
		// Editor

		private void ToggleFlightEditor()
		{
			_mode =(_mode == guiMode.Control) ? guiMode.Editor : guiMode.Control;

			RebuildUI();
		}

		private void InitEditorWindow(bool startSolid = true)
		{
			_editorWindow = GameObject.Instantiate(UIAssetsLoader.editorWindowPrefab);
			_editorWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
			_editorWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
			_editorWindowFader = _editorWindow.AddComponent<CanvasGroupFader>();

			// start invisible to be toggled later
			if(!startSolid)
				_editorWindow.GetComponent<CanvasGroup>().alpha = 0f;

			if(_editorWindowPosition == Vector3.zero)
				_editorWindowPosition = _editorWindow.transform.position; // get the default position from the prefab
			else
				_editorWindow.transform.position = ClampWindowPosition(_editorWindowPosition);

			if(_editorWindowSize == Vector2.zero)
				_editorWindowSize = _editorWindow.GetComponent<RectTransform>().sizeDelta;
			else
				_editorWindow.GetComponent<RectTransform>().sizeDelta = _editorWindowSize;

			var settingsButton = _editorWindow.GetChild("WindowTitle").GetChild("LeftWindowButton");
			if(settingsButton != null)
			{
				settingsButton.GetComponent<Button>().onClick.AddListener(ToggleSettingsWindow);
				var t = settingsButton.AddComponent<BasicTooltip>();
				t.tooltipText = "Show/hide UI settings";
			}

			var closeButton = _editorWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
			if(closeButton != null)
			{
				if(HighLogic.LoadedSceneIsFlight)
				{
					closeButton.GetComponent<Button>().onClick.AddListener(ToggleFlightEditor);
					var t = closeButton.AddComponent<BasicTooltip>();
					t.tooltipText = "Return to Flight Mode";
				}
				else
				{
					closeButton.GetComponent<Button>().onClick.AddListener(OnHideCallback);
					var t = closeButton.AddComponent<BasicTooltip>();
					t.tooltipText = "Close window";
				}
			}

			var editorFooterButtons = _editorWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");
			var newGroupNameInputField = editorFooterButtons.GetChild("NewGroupNameInputField").GetComponent<InputField>();
			var addGroupButton = editorFooterButtons.GetChild("AddGroupButton").GetComponent<Button>();
			addGroupButton.onClick.AddListener(() =>
				{
					string newGroupName = newGroupNameInputField.text;
					int newGroupNameNumber = 0;

					for(int i = 0; i < Controller.Instance.ServoGroups.Count; i++)
					{
						if(Controller.Instance.ServoGroups[i].Name == newGroupName)
						{
							newGroupName = newGroupNameInputField.text + "(" + ++newGroupNameNumber + ")";
							i = 0;
						}
					}

					var g =
						HighLogic.LoadedSceneIsFlight
						? new ServoGroup(Controller.Instance.ServoGroups[0].Vessel, newGroupName)
						: new ServoGroup(newGroupName);

					Controller.Instance.ServoGroups.Add(g);

					GameObject servoGroupsArea = _editorWindow.GetChild("WindowContent").GetChild("Scroll View").GetChild("Viewport").GetChild("Content").GetChild("ServoGroupsVLG");

					var newServoGroupLine = GameObject.Instantiate(UIAssetsLoader.editorWindowGroupLinePrefab);
					newServoGroupLine.transform.SetParent(servoGroupsArea.transform, false);

					InitEditorGroupControls(newServoGroupLine, Controller.Instance.GetInterceptor(g));

					_servoGroupUIControls.Add(g, newServoGroupLine);

					Invalidate();
				});

			var addGroupTooltip = addGroupButton.gameObject.AddComponent<BasicTooltip>();
			addGroupTooltip.tooltipText = "Add new group to the\n end of the list.";

			var advancedModeToggle = editorFooterButtons.GetChild("AdvancedModeToggle").GetComponent<Toggle>();
			advancedModeToggle.isOn = _bIsAdvancedModeOn;
			advancedModeToggle.onValueChanged.AddListener(v =>
				{
					_bIsAdvancedModeOn = v;

					foreach(var pair in _servoUIControls)
						ShowServoAdvancedMode(pair.ui, v);
				});

			var advancedModeToggleTooltip = advancedModeToggle.gameObject.AddComponent<BasicTooltip>();
			advancedModeToggleTooltip.tooltipText = "Show/hide advanced servo parameters";

			var buildAidToggle = editorFooterButtons.GetChild("BuildAidToggle").GetComponent<Toggle>();
			buildAidToggle.isOn = _bIsBuildAidOn;
			buildAidToggle.onValueChanged.AddListener(v =>
				{
					_bIsBuildAidOn = v;

					foreach(var pair in _servoUIControls)
					{
						var servoBuildAidToggle = pair.ui.GetChild("ServoBuildAidToggle");
						servoBuildAidToggle.SetActive(v);
					}

					foreach(var pair in _servoGroupUIControls)
					{
						var groupBuildAidToggle = pair.Value.GetChild("ServoGroupControlsHLG").GetChild("GroupBuildAidToggle");
						groupBuildAidToggle.SetActive(v);
					}

					if(IRBuildAid.IRBuildAidManager.Instance != null)
						IRBuildAid.IRBuildAidManager.isHidden = v;
				});

			var buildAidToggleTooltip = buildAidToggle.gameObject.AddComponent<BasicTooltip>();
			buildAidToggleTooltip.tooltipText = "Toggle IRBuildAid";

			buildAidToggle.gameObject.SetActive(HighLogic.LoadedSceneIsEditor);

			var resizeHandler = editorFooterButtons.GetChild("ResizeHandle").AddComponent<PanelResizer>();
			resizeHandler.rectTransform = _editorWindow.transform as RectTransform;
			resizeHandler.minSize = new Vector2(350, 280);
			resizeHandler.maxSize = new Vector2(2000, 1600);
		}

		private void onSelectedPart(Part p)
		{
			_editorPartSelectorGroup.AddControl(p.GetComponent<ModuleIRServo_v3>(), -1);

			_editorPartSelectorGroup = null;

			Invalidate();
		}

		private void InitEditorGroupControls(GameObject newServoGroupLine, IServoGroup g)
		{
			var hlg = newServoGroupLine.GetChild("ServoGroupControlsHLG");
			var servosVLG = newServoGroupLine.GetChild("ServoGroupServosVLG");
			servosVLG.AddComponent<ServoDropHandler>();

			var groupBuildAidToggle = hlg.GetChild("GroupBuildAidToggle").GetComponent<Toggle>();
			groupBuildAidToggle.gameObject.SetActive(HighLogic.LoadedSceneIsEditor && _bIsBuildAidOn);
			groupBuildAidToggle.isOn = g.group.BuildAid;
			groupBuildAidToggle.onValueChanged.AddListener(v =>
				{
					if(IRBuildAid.IRBuildAidManager.Instance == null)
						return;

					var servoToggles = servosVLG.GetComponentsInChildren<Toggle>(true);
					for(int i = 0; i < servoToggles.Length; i++)
					{
						if(servoToggles[i].name == "ServoBuildAidToggle")
						{
							servoToggles[i].isOn = v;
							//servoToggles[i].onValueChanged.Invoke(v);
						}
					}

					g.group.BuildAid = v;
				});

			var groupDragHandler = hlg.GetChild("GroupDragHandle").AddComponent<GroupDragHandler>();
			groupDragHandler.mainCanvas = UIMasterController.Instance.appCanvas;
			groupDragHandler.background = UIAssetsLoader.spriteAssets.Find(a => a.name == "IRWindowGroupFrame_Drag");
			
			var groupDragHandlerTooltip = groupDragHandler.gameObject.AddComponent<BasicTooltip>();
			groupDragHandlerTooltip.tooltipText = "Drag to reorder the group";

			var groupName = hlg.GetChild("GroupNameInputField").GetComponent<InputField>();
			groupName.text = g.Name;
			groupName.onEndEdit.AddListener(s => { g.Name = s; });

			var groupMoveLeftKey = hlg.GetChild("GroupMoveLeftKey").GetComponent<InputField>();
			groupMoveLeftKey.text = g.ReverseKey;
			groupMoveLeftKey.onEndEdit.AddListener(s => { g.ReverseKey = s; });

			var groupMoveRightKey = hlg.GetChild("GroupMoveRightKey").GetComponent<InputField>();
			groupMoveRightKey.text = g.ForwardKey;
			groupMoveRightKey.onEndEdit.AddListener(s => { g.ForwardKey = s; });

			var groupMoveLeftButton = hlg.GetChild("GroupMoveLeftButton");
			var groupMoveLeftHoldButton = groupMoveLeftButton.AddComponent<HoldButton>();
			groupMoveLeftHoldButton.callbackOnDown = (() => { if(!HighLogic.LoadedSceneIsEditor) g.MoveLeft(); else g.EditorMoveLeft(); });
			groupMoveLeftHoldButton.callbackOnUp = (() => { if(!HighLogic.LoadedSceneIsEditor) g.Stop(); });
			if(HighLogic.LoadedSceneIsEditor)
				groupMoveLeftHoldButton.updateHandler = g.EditorMoveLeft;
			
			var groupMoveLeftTooltip = groupMoveLeftButton.AddComponent<BasicTooltip>();
			groupMoveLeftTooltip.tooltipText = "Hold to move negative";

			var groupMoveCenterButton = hlg.GetChild("GroupMoveCenterButton");
			var groupMoveCenterHoldButton = groupMoveCenterButton.AddComponent<HoldButton>();
			groupMoveCenterHoldButton.callbackOnDown = (() => { if(!HighLogic.LoadedSceneIsEditor) g.MoveCenter(); else g.EditorMoveCenter(); });
			groupMoveCenterHoldButton.callbackOnUp = (() => { if(!HighLogic.LoadedSceneIsEditor) g.Stop(); });
		//	if(HighLogic.LoadedSceneIsEditor)
		//		groupMoveCenterHoldButton.updateHandler = g.EditorMoveCenter;

			var groupMoveCenterButtonTooltip = groupMoveCenterButton.AddComponent<BasicTooltip>();
			groupMoveCenterButtonTooltip.tooltipText = "Move to default position";

			var groupMoveRightButton = hlg.GetChild("GroupMoveRightButton");
			var groupMoveRightHoldButton = groupMoveRightButton.AddComponent<HoldButton>();
			groupMoveRightHoldButton.callbackOnDown = (() => { if(!HighLogic.LoadedSceneIsEditor) g.MoveRight(); else g.EditorMoveRight(); });
			groupMoveRightHoldButton.callbackOnUp = (() => { if(!HighLogic.LoadedSceneIsEditor) g.Stop(); });
			if(HighLogic.LoadedSceneIsEditor)
				groupMoveRightHoldButton.updateHandler = g.EditorMoveRight;

			var groupMoveRightTooltip = groupMoveRightButton.AddComponent<BasicTooltip>();
			groupMoveRightTooltip.tooltipText = "Hold to move positive";

			var groupAddServoButton = hlg.GetChild("GroupAddServoButton").GetComponent<Button>();
			groupAddServoButton.onClick.AddListener(() =>
				{
					_editorPartSelectorGroup = (ServoGroup)g.group;

					GameObject go = new GameObject("PartSelectorHelper");
					Utility.PartSelector Selector = go.AddComponent<InfernalRobotics_v3.Utility.PartSelector>();

					Selector.onSelectedCallback = onSelectedPart;

					if(HighLogic.LoadedSceneIsFlight)
						Selector.AddAllPartsOfType<ModuleIRServo_v3>(FlightGlobals.ActiveVessel);
					else if(HighLogic.LoadedSceneIsEditor)
						Selector.AddAllPartsOfType<ModuleIRServo_v3>(EditorLogic.fetch.ship);

					Selector.StartSelection();
				});

			var groupDeleteButton = hlg.GetChild("GroupDeleteButton").GetComponent<Button>();
			groupDeleteButton.onClick.AddListener(() =>
				{
					if(Controller.Instance.ServoGroups.Count > 1)
					{
						while(g.Servos.Any())
							((ServoGroup)g.group).RemoveControl(g.Servos.First().servo);

						Controller.Instance.ServoGroups.Remove(g.group);
						g = null;

						Invalidate();
					}
				});

			var groupDeleteButtonTooltip = groupDeleteButton.gameObject.AddComponent<BasicTooltip>();
			groupDeleteButtonTooltip.tooltipText = "Delete Group";

			// now list servos
			for(int j = 0; j < g.Servos.Count; j++)
			{
				var s = g.Servos[j];

				if(s.IsFreeMoving || !s.IsServo)
					continue;

				var newServoLine = GameObject.Instantiate(UIAssetsLoader.editorWindowServoLinePrefab);
				newServoLine.transform.SetParent(servosVLG.transform, false);

				InitEditorServoControls(newServoLine, Controller.Instance.GetInterceptor(g), Controller.Instance.GetInterceptor(s));

				_servoUIControls.Add(new servoUIControl { s = Controller.Instance.GetInterceptor(s), ui = newServoLine });
			}
		}

		private void InitEditorServoControls(GameObject newServoLine, IServoGroup g, IServo s)
		{
			var servoBuildAidToggle = newServoLine.GetChild("ServoBuildAidToggle").GetComponent<Toggle>();
			servoBuildAidToggle.gameObject.SetActive(HighLogic.LoadedSceneIsEditor && _bIsBuildAidOn);
			servoBuildAidToggle.onValueChanged.AddListener(v =>
				{
					if(!IRBuildAid.IRBuildAidManager.Instance)
						return;

					if(v)
						IRBuildAid.IRBuildAidManager.Instance.ShowServoRange(s.servo);
					else
						IRBuildAid.IRBuildAidManager.Instance.HideServoRange(s.servo);

					Controller.Instance.ServoBuildAid(s, v);
				});

			if(HighLogic.LoadedSceneIsEditor && Controller.Instance.ServoBuildAid(s))
			{
				servoBuildAidToggle.isOn = true;
				if(IRBuildAid.IRBuildAidManager.Instance)
					IRBuildAid.IRBuildAidManager.Instance.ShowServoRange(s.servo);
			}
	
			var servoBuildAidTooltip = servoBuildAidToggle.gameObject.AddComponent<BasicTooltip>();
			servoBuildAidTooltip.tooltipText = "Toggle IR BuildAid Helper";
	
			var servoDragHandler = newServoLine.GetChild("ServoDragHandle").AddComponent<ServoDragHandler>();
			servoDragHandler.mainCanvas = UIMasterController.Instance.appCanvas;
			servoDragHandler.background = UIAssetsLoader.spriteAssets.Find(a => a.name == "IRWindowServoFrame_Drag");

			var servoDragHandlerTooltip = servoDragHandler.gameObject.AddComponent<BasicTooltip>();
			servoDragHandlerTooltip.tooltipText = "Drag to reorder the servo\n or put it into a different group";

			var servoName = newServoLine.GetChild("ServoNameInputField").GetComponent<InputField>();
			servoName.text = s.Name;
			servoName.onEndEdit.AddListener(n => { s.Name = n; Invalidate(); });

			var servoHighlighter = servoName.gameObject.AddComponent<ServoHighlighter>();
			servoHighlighter.servo = s.servo;

			var servoTooltip = servoName.gameObject.AddComponent<BasicTooltip>();
			servoTooltip.tooltipText = "You can rename servos\n Names do not have to be unique";

			var servoPrevPresetButton = newServoLine.GetChild("ServoPrevPresetButton").GetComponent<Button>();
			servoPrevPresetButton.onClick.AddListener(
				() => { if(!HighLogic.LoadedSceneIsEditor) s.Presets.MovePrev(s.DefaultSpeed * g.GroupSpeedFactor); else s.Presets.EditorMovePrev(s.DefaultSpeed * g.GroupSpeedFactor); });

			var servoPrevPresetTooltip = servoPrevPresetButton.gameObject.AddComponent<BasicTooltip>();
			servoPrevPresetTooltip.tooltipText = "Move to previous preset";

			var servoPosition = newServoLine.GetChild("ServoPositionInputField").GetComponent<InputField>();
			servoPosition.text = string.Format("{0:#0.##}", s.CommandedPosition);
			servoPosition.onEndEdit.AddListener(tmp =>
				{
					float tmpValue = 0f;
					if(float.TryParse(tmp, out tmpValue))
					{
						tmpValue = Mathf.Clamp(tmpValue, s.MinPositionLimit, s.MaxPositionLimit);

						if(Math.Abs(s.CommandedPosition - tmpValue) > 0.005f)
						{ if(!HighLogic.LoadedSceneIsEditor) s.MoveTo(tmpValue); else s.EditorSetTo(tmpValue); }
					}
				});

			var servoNextPresetButton = newServoLine.GetChild("ServoNextPresetButton").GetComponent<Button>();
			servoNextPresetButton.onClick.AddListener(
				() => { if(!HighLogic.LoadedSceneIsEditor) s.Presets.MoveNext(s.DefaultSpeed * g.GroupSpeedFactor); else s.Presets.EditorMoveNext(s.DefaultSpeed * g.GroupSpeedFactor); });

			var servoNextPresetTooltip = servoNextPresetButton.gameObject.AddComponent<BasicTooltip>();
			servoNextPresetTooltip.tooltipText = "Move to next preset";

			var servoOpenPresetsToggle = newServoLine.GetChild("ServoOpenPresetsToggle").GetComponent<Toggle>();
			servoOpenPresetsToggle.isOn = guiPresetsWindowOpen;
			servoOpenPresetsToggle.onValueChanged.AddListener(v => { TogglePresetEditWindow(g, s, v, servoOpenPresetsToggle.gameObject); });

			var servoOpenPresetsToggleTooltip = servoOpenPresetsToggle.gameObject.AddComponent<BasicTooltip>();
			servoOpenPresetsToggleTooltip.tooltipText = "Open/close presets";

			var servoMoveLeftButton = newServoLine.GetChild("ServoMoveLeftButton");
			var servoMoveLeftHoldButton = servoMoveLeftButton.AddComponent<HoldButton>();
			servoMoveLeftHoldButton.callbackOnDown = (() => { if(!HighLogic.LoadedSceneIsEditor) s.MoveLeft(s.DefaultSpeed * g.GroupSpeedFactor); else s.EditorMoveLeft(s.DefaultSpeed * g.GroupSpeedFactor); });
			servoMoveLeftHoldButton.callbackOnUp = (() => { if(!HighLogic.LoadedSceneIsEditor) s.Stop(); });
			if(HighLogic.LoadedSceneIsEditor)
				servoMoveLeftHoldButton.updateHandler = () => s.EditorMoveLeft(s.DefaultSpeed * g.GroupSpeedFactor);

			var servoMoveLeftTooltip = servoMoveLeftButton.AddComponent<BasicTooltip>();
			servoMoveLeftTooltip.tooltipText = "Hold to move negative";

			var servoMoveCenterButton = newServoLine.GetChild("ServoMoveCenterButton");
			var servoMoveCenterHoldButton = servoMoveCenterButton.AddComponent<HoldButton>();
			servoMoveCenterHoldButton.callbackOnDown = (() => { if(!HighLogic.LoadedSceneIsEditor) s.MoveCenter(s.DefaultSpeed * g.GroupSpeedFactor); else s.EditorMoveCenter(s.DefaultSpeed * g.GroupSpeedFactor); });
			servoMoveCenterHoldButton.callbackOnUp = (() => { if(!HighLogic.LoadedSceneIsEditor) s.Stop(); });
		//	if(HighLogic.LoadedSceneIsEditor)
		//		servoMoveCenterHoldButton.updateHandler = s.EditorMoveCenter;

			var servoMoveCenterButtonTooltip = servoMoveCenterButton.AddComponent<BasicTooltip>();
			servoMoveCenterButtonTooltip.tooltipText = "Move to default position";

			var servoMoveRightButton = newServoLine.GetChild("ServoMoveRightButton");
			var servoMoveRightHoldButton = servoMoveRightButton.AddComponent<HoldButton>();
			servoMoveRightHoldButton.callbackOnDown = (() => { if(!HighLogic.LoadedSceneIsEditor) s.MoveRight(s.DefaultSpeed * g.GroupSpeedFactor); else s.EditorMoveRight(s.DefaultSpeed * g.GroupSpeedFactor); });
			servoMoveRightHoldButton.callbackOnUp = (() => { if(!HighLogic.LoadedSceneIsEditor) s.Stop(); });
			if(HighLogic.LoadedSceneIsEditor)
				servoMoveRightHoldButton.updateHandler = () => s.EditorMoveRight(s.DefaultSpeed * g.GroupSpeedFactor);

			var servoMoveRightTooltip = servoMoveRightButton.AddComponent<BasicTooltip>();
			servoMoveRightTooltip.tooltipText = "Hold to move positive";

			var advancedModeToggle = newServoLine.GetChild("ServoShowOtherFieldsToggle").GetComponent<Toggle>();

			var servoRangeMinInputField = newServoLine.GetChild("ServoRangeMinInputField").GetComponent<InputField>();
			servoRangeMinInputField.text = string.Format("{0:#0.##}", s.MinPositionLimit);
			servoRangeMinInputField.onEndEdit.AddListener(tmp =>
				{
					float v;
					if(float.TryParse(tmp, out v))
						s.MinPositionLimit = v;
				});
			
			var servoRangeMaxInputField = newServoLine.GetChild("ServoRangeMaxInputField").GetComponent<InputField>();
			servoRangeMaxInputField.text = string.Format("{0:#0.##}", s.MaxPositionLimit);
			servoRangeMaxInputField.onEndEdit.AddListener(tmp =>
				{
					float v;
					if(float.TryParse(tmp, out v))
						s.MaxPositionLimit = v;
				});


			var servoEngageLimitsToggle = newServoLine.GetChild("ServoEngageLimitsToggle").GetComponent<Toggle>();
			servoEngageLimitsToggle.isOn = s.IsLimitted;
			servoEngageLimitsToggle.onValueChanged.AddListener(v =>
				{
					s.IsLimitted = v;

					newServoLine.GetChild("ServoRangeLabel").SetActive(v & advancedModeToggle.isOn);
					servoRangeMinInputField.gameObject.SetActive(v & advancedModeToggle.isOn);
					servoRangeMaxInputField.gameObject.SetActive(v & advancedModeToggle.isOn);
				});
			servoEngageLimitsToggle.gameObject.SetActive(false);

			var servoEngageLimitsToggleTooltip = servoEngageLimitsToggle.gameObject.AddComponent<BasicTooltip>();
			servoEngageLimitsToggleTooltip.tooltipText = "Engage/disengage limits";

			var servoSpeedInputField = newServoLine.GetChild("ServoSpeedInputField").GetComponent<InputField>();
			servoSpeedInputField.text = string.Format("{0:#0.##}", s.SpeedLimit);
			servoSpeedInputField.onEndEdit.AddListener(tmp =>
				{
					float v;
					if(float.TryParse(tmp, out v))
						s.SpeedLimit = v;
				});

			var servoAccInputField = newServoLine.GetChild("ServoAccInputField").GetComponent<InputField>();
			servoAccInputField.text = string.Format("{0:#0.##}", s.AccelerationLimit);
			servoAccInputField.onEndEdit.AddListener(tmp =>
				{
					float v;
					if(float.TryParse(tmp, out v))
						s.AccelerationLimit = v;
				});

			var servoInvertAxisToggle = newServoLine.GetChild("ServoInvertAxisToggle").GetComponent<Toggle>();
			servoInvertAxisToggle.onValueChanged.AddListener(v =>
				{
					servoInvertAxisToggle.isOn = v;
					s.IsInverted = v;
				});
			// init icon state properly
			servoInvertAxisToggle.onValueChanged.Invoke(s.IsInverted);

			var servoInvertAxisToggleTooltip = servoInvertAxisToggle.gameObject.AddComponent<BasicTooltip>();
			servoInvertAxisToggleTooltip.tooltipText = "Invert/uninvert servo axis";

			var servoLockToggle = newServoLine.GetChild("ServoLockToggle").GetComponent<Toggle>();
			servoLockToggle.onValueChanged.AddListener(v =>
				{
					servoLockToggle.isOn = v;
					s.IsLocked = v;
				});
			// init icon state properly
			servoLockToggle.onValueChanged.Invoke(s.IsLocked);

			var servoLockToggleTooltip = servoLockToggle.gameObject.AddComponent<BasicTooltip>();
			servoLockToggleTooltip.tooltipText = "Lock/unlock the servo";

			advancedModeToggle.onValueChanged.AddListener(v =>
				{
					//advancedModeToggle.isOn = v;

					if(v)
					{
						//need to disable normal buttons and enable advanced ones

						// disable normal
						servoPrevPresetButton.gameObject.SetActive(false);
						servoPosition.gameObject.SetActive(false);
						servoNextPresetButton.gameObject.SetActive(false);
						servoOpenPresetsToggle.gameObject.SetActive(false);
						servoMoveLeftButton.gameObject.SetActive(false);
						servoMoveCenterButton.gameObject.SetActive(false);
						servoMoveRightButton.gameObject.SetActive(false);

						bool showRangeEdit = s.IsLimitted && s.CanHaveLimits;
						// enable advanced
						servoEngageLimitsToggle.gameObject.SetActive(s.CanHaveLimits);
						newServoLine.GetChild("ServoRangeLabel").SetActive(showRangeEdit);
						servoRangeMinInputField.gameObject.SetActive(showRangeEdit);
						servoRangeMaxInputField.gameObject.SetActive(showRangeEdit);
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

						// disable advanced
						servoEngageLimitsToggle.gameObject.SetActive(false);
						newServoLine.GetChild("ServoRangeLabel").SetActive(false);
						servoRangeMinInputField.gameObject.SetActive(false);
						servoRangeMaxInputField.gameObject.SetActive(false);
						newServoLine.GetChild("ServoSpeedLabel").SetActive(false);
						servoSpeedInputField.gameObject.SetActive(false);
						newServoLine.GetChild("ServoAccLabel").SetActive(false);
						servoAccInputField.gameObject.SetActive(false);
						servoInvertAxisToggle.gameObject.SetActive(false);
						servoLockToggle.gameObject.SetActive(false);

						// enable normal
						servoPrevPresetButton.gameObject.SetActive(true);
						servoPosition.gameObject.SetActive(true);
						servoNextPresetButton.gameObject.SetActive(true);
						servoOpenPresetsToggle.gameObject.SetActive(true);
						servoMoveLeftButton.gameObject.SetActive(true);
						servoMoveCenterButton.gameObject.SetActive(true);
						servoMoveRightButton.gameObject.SetActive(true);
					}
				});
			advancedModeToggle.gameObject.SetActive(false);

			if(g.AdvancedMode)
			{
				advancedModeToggle.onValueChanged.Invoke(true);
				advancedModeToggle.isOn = true;
			}

			var advancedModeToggleTooltip = advancedModeToggle.gameObject.AddComponent<BasicTooltip>();
			advancedModeToggleTooltip.tooltipText = "Show advanced fields";

			var servoDeleteButton = newServoLine.GetChild("ServoDeleteButton").GetComponent<Button>();
			servoDeleteButton.onClick.AddListener(() =>
				{
					((ServoGroup)g.group).RemoveControl(s.servo);

					Invalidate();
				});
		}

		////////////////////////////////////////
		// Settings

		public void ToggleSettingsWindow()
		{
			if(_settingsWindow == null)
				InitSettingsWindow();

			guiSettingsWindowOpen = !guiSettingsWindowOpen;

			if(guiSettingsWindowOpen)
			{
				_settingsWindow.SetActive(true);
				_settingsWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
			}
			else
				_settingsWindowFader.FadeTo(0, UI_FADE_TIME,() => { _settingsWindow.SetActive(false); });
		}

		private void InitSettingsWindow()
		{
			_settingsWindow = GameObject.Instantiate(UIAssetsLoader.uiSettingsWindowPrefab);
			_settingsWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
			_settingsWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
			_settingsWindowFader = _settingsWindow.AddComponent<CanvasGroupFader>();

			if(_settingsWindowPosition == Vector3.zero)
				_settingsWindowPosition = _settingsWindow.transform.position; // get the default position from the prefab
			else
				_settingsWindow.transform.position = ClampWindowPosition(_settingsWindowPosition);

			_settingsWindow.GetComponent<CanvasGroup>().alpha = 0f;

			var closeButton = _settingsWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
			if(closeButton != null)
				closeButton.GetComponent<Button>().onClick.AddListener(ToggleSettingsWindow);

			var alphaText = _settingsWindow.GetChild("WindowContent").GetChild("UITransparencySliderHLG").GetChild("TransparencyValue").GetComponent<Text>();
			alphaText.text = string.Format("{0:#0.00}", _UIAlphaValue);

			var transparencySlider = _settingsWindow.GetChild("WindowContent").GetChild("UITransparencySliderHLG").GetChild("TransparencySlider");

			if(transparencySlider)
			{
				var sliderControl = transparencySlider.GetComponent<Slider>();
				sliderControl.minValue = UI_MIN_ALPHA;
				sliderControl.maxValue = 1.0f;
				sliderControl.value = _UIAlphaValue;
				sliderControl.onValueChanged.AddListener(v => { alphaText.text = string.Format("{0:#0.00}", v);});
			}

			var scaleText = _settingsWindow.GetChild("WindowContent").GetChild("UIScaleSliderHLG").GetChild("ScaleValue").GetComponent<Text>();
			scaleText.text = string.Format("{0:#0.00}", _UIScaleValue);

			var scaleSlider = _settingsWindow.GetChild("WindowContent").GetChild("UIScaleSliderHLG").GetChild("ScaleSlider");

			if(scaleSlider)
			{
				var sliderControl = scaleSlider.GetComponent<Slider>();
				sliderControl.minValue = UI_MIN_SCALE;
				sliderControl.maxValue = UI_MAX_SCALE;
				sliderControl.value = _UIScaleValue;
				sliderControl.onValueChanged.AddListener(v => { scaleText.text = string.Format("{0:#0.00}", v);});
			}

			var useECToggle = _settingsWindow.GetChild("WindowContent").GetChild("UseECHLG").GetChild("UseECToggle").GetComponent<Toggle>();
			useECToggle.isOn = UseElectricCharge;
			useECToggle.onValueChanged.AddListener(v => UseElectricCharge = v);

			var useECToggleTooltip = useECToggle.gameObject.AddComponent<BasicTooltip>();
			useECToggleTooltip.tooltipText = "Debug mode, no EC consumption.\nRequires scene change";

			var footerButtons = _settingsWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");

			var cancelButton = footerButtons.GetChild("CancelButton").GetComponent<Button>();
			cancelButton.onClick.AddListener(() =>
				{
					transparencySlider.GetComponent<Slider>().value = _UIAlphaValue;
					alphaText.text = string.Format("{0:#0.00}", _UIAlphaValue);

					scaleSlider.GetComponent<Slider>().value = _UIScaleValue;
					scaleText.text = string.Format("{0:#0.00}", _UIScaleValue);
				});

			var defaultButton = footerButtons.GetChild("DefaultButton").GetComponent<Button>();
			defaultButton.onClick.AddListener(() =>
				{
					_UIAlphaValue = 0.8f;
					_UIScaleValue = 1.0f;

					transparencySlider.GetComponent<Slider>().value = _UIAlphaValue;
					alphaText.text = string.Format("{0:#0.00}", _UIAlphaValue);

					scaleSlider.GetComponent<Slider>().value = _UIScaleValue;
					scaleText.text = string.Format("{0:#0.00}", _UIScaleValue);

					SetGlobalAlpha(_UIAlphaValue);
					SetGlobalScale(_UIScaleValue);
				});

			var applyButton = footerButtons.GetChild("ApplyButton").GetComponent<Button>();
			applyButton.onClick.AddListener(() => 
				{
					float newAlphaValue =(float) Math.Round(transparencySlider.GetComponent<Slider>().value, 2);
					float newScaleValue =(float) Math.Round(scaleSlider.GetComponent<Slider>().value, 2);

					SetGlobalAlpha(newAlphaValue);
					SetGlobalScale(newScaleValue);
				});
			_settingsWindow.SetActive(false);
		}

		public void UpdateServoReadoutsFlight(IServo s, GameObject servoUIControls)
		{
			var servoStatusLight = servoUIControls.GetChild("ServoStatusRawImage").GetComponent<RawImage>();
			if(s.IsLocked)
				servoStatusLight.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Locked");
			else if(s.IsMoving)
				servoStatusLight.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Active");
			else
				servoStatusLight.texture = UIAssetsLoader.iconAssets.Find(i => i.name == "IRWindowIndicator_Idle");

			var servoPosition = servoUIControls.GetChild("ServoPositionText").GetComponent<Text>();
			servoPosition.text = string.Format("{0:#0.##}", s.CommandedPosition);
			servoPosition.color = s.IsInverted ? Color.yellow : Color.white;

			var servoLockToggle = servoUIControls.GetChild("ServoLockToggleButton").GetComponent<Toggle>();
			if(servoLockToggle.isOn != s.IsLocked)
				servoLockToggle.onValueChanged.Invoke(s.IsLocked);
			
			var servoInvertAxisToggle = servoUIControls.GetChild("ServoInvertAxisToggleButton").GetComponent<Toggle>();
			if(servoInvertAxisToggle.isOn != s.IsInverted)
				servoInvertAxisToggle.onValueChanged.Invoke(s.IsInverted);
		}

		public void UpdateGroupReadoutsFlight(IServoGroup g, GameObject groupUIControls)
		{
			var groupSpeed = groupUIControls.GetChild("ServoGroupSpeedMultiplier").GetComponent<InputField>();
			if(!groupSpeed.isFocused)
				groupSpeed.text = string.Format("{0:#0.##}", g.GroupSpeedFactor);

			foreach(var t in groupUIControls.GetComponentsInChildren<Toggle>())
			{
				if(t.gameObject.name == "ServoGroupMoveLeftToggleButton")
					t.isOn = g.MovingDirection < 0;
				else if(t.gameObject.name == "ServoGroupMoveRightToggleButton")
					t.isOn = g.MovingDirection > 0;
			}
		}

		public void UpdateServoReadoutsEditor(IServo s, GameObject servoUIControls)
		{
			var servoPosition = servoUIControls.GetChild("ServoPositionInputField").GetComponent<InputField>();
			if(!servoPosition.isFocused)
			{
				servoPosition.text = string.Format("{0:#0.##}", s.CommandedPosition);
				servoPosition.gameObject.GetChild("Text").GetComponent<Text>().color = s.IsInverted ? ir_yellow : Color.white;
			}

			var advancedModeToggle = servoUIControls.GetChild("ServoShowOtherFieldsToggle").GetComponent<Toggle>();

			var servoRangeMinInputField = servoUIControls.GetChild("ServoRangeMinInputField").GetComponent<InputField>();
			if(!servoRangeMinInputField.isFocused)
			{
				servoRangeMinInputField.text = string.Format("{0:#0.##}", s.MinPositionLimit);
				servoRangeMinInputField.gameObject.GetChild("Text").GetComponent<Text>().color = s.IsInverted ? ir_yellow : Color.white;
			}

			var servoRangeMaxInputField = servoUIControls.GetChild("ServoRangeMaxInputField").GetComponent<InputField>();
			if(!servoRangeMaxInputField.isFocused)
			{
				servoRangeMaxInputField.text = string.Format("{0:#0.##}", s.MaxPositionLimit);
				servoRangeMaxInputField.gameObject.GetChild("Text").GetComponent<Text>().color = s.IsInverted ? ir_yellow : Color.white;
			}
				
			var servoEngageLimitsToggle = servoUIControls.GetChild("ServoEngageLimitsToggle").GetComponent<Toggle>();
			servoEngageLimitsToggle.isOn = s.IsLimitted;
			servoUIControls.GetChild("ServoRangeLabel").SetActive(s.IsLimitted && advancedModeToggle.isOn);
			servoRangeMinInputField.gameObject.SetActive(s.IsLimitted && advancedModeToggle.isOn);
			servoRangeMaxInputField.gameObject.SetActive(s.IsLimitted && advancedModeToggle.isOn);

			var servoSpeedInputField = servoUIControls.GetChild("ServoSpeedInputField").GetComponent<InputField>();
			if(!servoSpeedInputField.isFocused)
				servoSpeedInputField.text = string.Format("{0:#0.##}", s.SpeedLimit);

			var servoAccInputField = servoUIControls.GetChild("ServoAccInputField").GetComponent<InputField>();
			if(!servoAccInputField.isFocused)
				servoAccInputField.text = string.Format("{0:#0.##}", s.AccelerationLimit);

			var servoLockToggle = servoUIControls.GetChild("ServoLockToggle").GetComponent<Toggle>();
			if(s.IsLocked != servoLockToggle.isOn)
				servoLockToggle.onValueChanged.Invoke(s.IsLocked);

			var servoInvertAxisToggle = servoUIControls.GetChild("ServoInvertAxisToggle").GetComponent<Toggle>();
			if(s.IsInverted != servoInvertAxisToggle.isOn)
				servoInvertAxisToggle.onValueChanged.Invoke(s.IsInverted);
		}

		////////////////////////////////////////
		// Presets

		private void ToggleFlightPresetMode(bool value)
		{
			if(_controlWindow == null || _servoUIControls == null || _servoGroupUIControls == null)
				return;

			guiFlightPresetModeOn = value;

			// we need to turn off preset buttons and turn on normal buttons
			foreach(var groupPair in _servoGroupUIControls)
				SetGroupPresetControlsVisibility(groupPair.Value, value);
			foreach(var servoPair in _servoUIControls)
				SetServoPresetControlsVisibility(servoPair.ui, value);
		}

		public void TogglePresetEditWindow(IServoGroup group, IServo servo, bool value, GameObject buttonRef)
		{
			guiPresetsWindowOpen = value;

			if(value)
			{
				if(_presetsWindow)
				{
					// also need to find the other Toggle button that opened it
					foreach(var pair in _servoUIControls)
					{
						var toggle = pair.ui.GetChild("ServoOpenPresetsToggle");
						if(toggle != null && toggle != buttonRef)
							toggle.GetComponent<Toggle>().isOn = false;
					}
					_presetsWindowPosition = _presetsWindow.transform.position;
					_presetsWindow.DestroyGameObjectImmediate();
					_presetsWindow = null;
				}

				_presetsWindow = GameObject.Instantiate(UIAssetsLoader.presetWindowPrefab);
				_presetsWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
				_presetsWindow.GetComponent<CanvasGroup>().alpha = 0f;
				_presetsWindowFader = _presetsWindow.AddComponent<CanvasGroupFader>();

				// need a better way to tie them to each other
				_presetsWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

				if(_presetsWindowPosition == Vector3.zero && buttonRef != null)
					_presetsWindow.transform.position = buttonRef.transform.position + new Vector3(30, 0, 0);
				else
					_presetsWindow.transform.position = ClampWindowPosition(_presetsWindowPosition);

				presetWindowServoGroup = group;
				presetWindowServo = servo;

				var closeButton = _presetsWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
				if(closeButton != null)
				{
					closeButton.GetComponent<Button>().onClick.AddListener(() =>
						{
							TogglePresetEditWindow(group, servo, false, buttonRef);
							if(buttonRef!= null)
								buttonRef.GetComponent<Toggle>().isOn = false;
							else
							{
								foreach(var pair in _servoUIControls) {
									var toggle = pair.ui.GetChild("ServoOpenPresetsToggle");
									if(toggle != null)
										toggle.GetComponent<Toggle>().isOn = false;
								}
							}
						});
					var t = closeButton.AddComponent<BasicTooltip>();
					t.tooltipText = "Close preset window";
				}

				var footerControls = _presetsWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");

				var newPresetPositionInputField = footerControls.GetChild("NewPresetPositionInputField").GetComponent<InputField>();
				newPresetPositionInputField.text = string.Format("{0:#0.##}", servo.CommandedPosition);

				var addPresetButton = footerControls.GetChild("AddPresetButton").GetComponent<Button>();
				addPresetButton.onClick.AddListener(() =>
					{
						footerControls = _presetsWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");
						newPresetPositionInputField = footerControls.GetChild("NewPresetPositionInputField").GetComponent<InputField>();

						string tmp = newPresetPositionInputField.text;
						float tmpValue = 0f;
						if(float.TryParse(tmp, out tmpValue))
						{
							tmpValue = Mathf.Clamp(tmpValue, presetWindowServo.MinPositionLimit, presetWindowServo.MaxPositionLimit);
							presetWindowServo.Presets.Add(tmpValue);
							presetWindowServo.Presets.Sort();
							TogglePresetEditWindow(group, presetWindowServo, true, buttonRef);
						}
					});

				var addPresetButtonTooltip = addPresetButton.gameObject.AddComponent<BasicTooltip>();
				addPresetButtonTooltip.tooltipText = "Add preset";

				var presetsArea = _presetsWindow.GetChild("WindowContent");
				
				// now populate it with servo's presets
				for(int i = 0; i < servo.Presets.Count; i++)
				{
					var newPresetLine = GameObject.Instantiate(UIAssetsLoader.presetLinePrefab);
					newPresetLine.transform.SetParent(presetsArea.transform, false);

					var presetPositionInputField = newPresetLine.GetChild("PresetPositionInputField").GetComponent<InputField>();
					presetPositionInputField.text = string.Format("{0:#0.##}", servo.Presets[i]);
					var presetIndex = i;
					presetPositionInputField.onEndEdit.AddListener(tmp => PresetInputOnEndEdit(tmp, presetIndex, buttonRef));

					var servoDefaultPositionToggle = newPresetLine.GetChild("PresetDefaultPositionToggle").GetComponent<Toggle>();
					servoDefaultPositionToggle.group = presetsArea.GetComponent<ToggleGroup>();
					servoDefaultPositionToggle.isOn =(servo.DefaultPosition == servo.Presets[i]);
					servoDefaultPositionToggle.onValueChanged.AddListener(v =>
						{
							if(v)
								presetWindowServo.DefaultPosition = presetWindowServo.Presets[presetIndex];
						});

					var servoDefaultPositionToggleTooltip = servoDefaultPositionToggle.gameObject.AddComponent<BasicTooltip>();
					servoDefaultPositionToggleTooltip.tooltipText = "Set as default position";

					var presetMoveHereButton = newPresetLine.GetChild("PresetMoveHereButton").GetComponent<Button>();
					presetMoveHereButton.onClick.AddListener(() => servo.Presets.MoveTo(presetIndex, servo.DefaultSpeed * group.GroupSpeedFactor));

					var presetMoveHereButtonTooltip = presetMoveHereButton.gameObject.AddComponent<BasicTooltip>();
					presetMoveHereButtonTooltip.tooltipText = "Move to this position";

					var presetDeleteButton = newPresetLine.GetChild("PresetDeleteButton").GetComponent<Button>();
					presetDeleteButton.onClick.AddListener(() =>
						{
							if(presetWindowServo.Presets[presetIndex] == presetWindowServo.DefaultPosition)
								presetWindowServo.DefaultPosition = 0;
							presetWindowServo.Presets.RemoveAt(presetIndex);
							Destroy(newPresetLine);
							TogglePresetEditWindow(group, presetWindowServo, true, buttonRef);
						});

					var presetDeleteButtonTooltip = presetDeleteButton.gameObject.AddComponent<BasicTooltip>();
					presetDeleteButtonTooltip.tooltipText = "Delete preset";
				}
				SetGlobalScale(_UIScaleValue);

				_presetsWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
			}
			else
			{
				// just animate close the window.
				if(_presetsWindowFader)
					_presetsWindowFader.FadeTo(0f, UI_FADE_TIME,() =>
						{
							_presetsWindow.DestroyGameObjectImmediate();
							_presetsWindow = null;
							_presetsWindowFader = null;
							presetWindowServoGroup = null;
							presetWindowServo = null;
						});
			}
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

		public void PresetInputOnEndEdit(string tmp, int i, GameObject buttonRef = null)
		{
			if(presetWindowServo == null)
				return;

			float tmpValue = 0f;
			if(float.TryParse(tmp, out tmpValue))
			{
				tmpValue = Mathf.Clamp(tmpValue, presetWindowServo.MinPositionLimit, presetWindowServo.MaxPositionLimit);
				if(presetWindowServo.Presets[i] == presetWindowServo.DefaultPosition)
					presetWindowServo.DefaultPosition = tmpValue;
				presetWindowServo.Presets[i] = tmpValue;
				presetWindowServo.Presets.Sort();
				TogglePresetEditWindow(presetWindowServoGroup, presetWindowServo, true, buttonRef);
			}
		}

		public void ShowServoAdvancedMode(GameObject servoControls, bool value)
		{
			var servoToggle = servoControls.GetChild("ServoShowOtherFieldsToggle").GetComponent<Toggle>();
			servoToggle.onValueChanged.Invoke(value);
			servoToggle.isOn = value;
		}

		public void RebuildUI()
		{
			bInvalid = false;

			if(_controlWindow)
			{
				_controlWindowPosition = _controlWindow.transform.position;
				_controlWindow.DestroyGameObjectImmediate();
				_controlWindow = null;
			}
			
			if(_editorWindow)
			{
				_editorWindowPosition = _editorWindow.transform.position;
				_editorWindowSize = _editorWindow.GetComponent<RectTransform>().sizeDelta;
				_editorWindow.DestroyGameObjectImmediate();
				_editorWindow = null;
			}
				
			if(_settingsWindow)
				_settingsWindowPosition = _settingsWindow.transform.position;

			if(_presetsWindow)
				_presetsWindowPosition = _presetsWindow.transform.position;

			// should be called by ServoController when required(Vessel changed and such).

			_servoGroupUIControls.Clear();
			_servoUIControls.Clear();

			if(!Controller.APIReady)
				return;

			if(UIAssetsLoader.allPrefabsReady && _settingsWindow == null)
				InitSettingsWindow();

			if(HighLogic.LoadedSceneIsFlight && (_mode != guiMode.Editor))
			{
				// here we need to wait until prefabs become available and then Instatiate the window
				if(UIAssetsLoader.allPrefabsReady && _controlWindow == null)
					InitFlightControlWindow(GUIEnabled);
				
				GameObject servoGroupsArea = _controlWindow.GetChild("WindowContent").GetChild("ServoGroupsVLG");

				for(int i = 0; i < Controller.Instance.ServoGroups.Count; i++)
				{
					IServoGroup g = Controller.Instance.ServoGroups[i];

					if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != g.Vessel)
						continue;

					var newServoGroupLine = GameObject.Instantiate(UIAssetsLoader.controlWindowGroupLinePrefab);
					newServoGroupLine.transform.SetParent(servoGroupsArea.transform, false);

					InitFlightGroupControls(newServoGroupLine, Controller.Instance.GetInterceptor(g));
						
					_servoGroupUIControls.Add(g, newServoGroupLine);
				}

				if((Controller._IKModule != null) && (Controller._IKServoGroup != null))
					ToggleIKMode(Controller._IKServoGroup, true);

				UpdateIKButtons();
			}
			else // Editor
			{
				if(UIAssetsLoader.allPrefabsReady && _editorWindow == null)
					InitEditorWindow(GUIEnabled);
				
				GameObject servoGroupsArea = _editorWindow.GetChild("WindowContent").GetChild("Scroll View").GetChild("Viewport").GetChild("Content").GetChild("ServoGroupsVLG");
				servoGroupsArea.AddComponent<GroupDropHandler>();

				for(int i = 0; i < Controller.Instance.ServoGroups.Count; i++)
				{
					IServoGroup g = Controller.Instance.ServoGroups[i];

					var newServoGroupLine = GameObject.Instantiate(UIAssetsLoader.editorWindowGroupLinePrefab);
					newServoGroupLine.transform.SetParent(servoGroupsArea.transform, false);

					InitEditorGroupControls(newServoGroupLine, Controller.Instance.GetInterceptor(g));

					_servoGroupUIControls.Add(g, newServoGroupLine);
				}
			}

			// we don't need to set global alpha as all the windows will be faded it to the setting
			SetGlobalScale(_UIScaleValue);
		}

		public void ShowIRWindow()
		{
			if(!Controller.APIReady)
				return;

			if(bInvalid)
				RebuildUI();

			appLauncherButton.SetTrue(false);
			GUIEnabled = true;

			switch(_mode)
			{
			case guiMode.Control:
				_controlWindow.SetActive(true);
				_controlWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
				break;

			case guiMode.Editor:
				_editorWindow.SetActive(true);
				_editorWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
				break;
			}

			if(_settingsWindowFader && guiSettingsWindowOpen)
			{
				_settingsWindow.SetActive(true);
				_settingsWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
			}

			if(_presetsWindowFader && guiPresetsWindowOpen)
			{
				_presetsWindow.SetActive(true);
				_presetsWindowFader.FadeTo(_UIAlphaValue, UI_FADE_TIME);
			}
		}

		public void HideIRWindow()
		{
			appLauncherButton.SetFalse(false);
			GUIEnabled = false;

			switch(_mode)
			{
			case guiMode.Control:
				if(_controlWindowFader)
					_controlWindowFader.FadeTo(0f, UI_FADE_TIME,() => { _controlWindow.SetActive(false); });
				break;

			case guiMode.Editor:
				if(_editorWindowFader)
					_editorWindowFader.FadeTo(0f, UI_FADE_TIME,() => { _editorWindow.SetActive(false); });
				break;
			}

			if(_settingsWindowFader && guiSettingsWindowOpen)
				_settingsWindowFader.FadeTo(0f, UI_FADE_TIME,() => { _settingsWindow.SetActive(false); });

			if(_presetsWindowFader && guiPresetsWindowOpen)
				_presetsWindowFader.FadeTo(0f, UI_FADE_TIME,() => { _presetsWindow.SetActive(false); });
		}

		public void Update()
		{
			if(!GUIEnabled)
				return;

			if(!Controller.APIReady || !UIAssetsLoader.allPrefabsReady)
			{
				HideIRWindow();

				GUIEnabled = false;
		//		appLauncherButton.SetFalse(false);
			}

			if(bInvalid)
				RebuildUI();
			
			if(EventSystem.current.currentSelectedGameObject != null && 
			  (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null
				|| EventSystem.current.currentSelectedGameObject.GetType() == typeof(InputField))				/*
				(EventSystem.current.currentSelectedGameObject.name == "GroupNameInputField"
				 || EventSystem.current.currentSelectedGameObject.name == "GroupMoveLeftKey"
				 || EventSystem.current.currentSelectedGameObject.name == "GroupMoveRightKey"
				 || EventSystem.current.currentSelectedGameObject.name == "ServoNameInputField"
				 || EventSystem.current.currentSelectedGameObject.name == "ServoPositionInputField"
				 || EventSystem.current.currentSelectedGameObject.name == "NewGroupNameInputField"
				 || EventSystem.current.currentSelectedGameObject.name == "ServoGroupSpeedMultiplier")*/)
			{
				if(!isKeyboardLocked)
					KeyboardLock(true); 
			}
			else
			{
				if(isKeyboardLocked)
					KeyboardLock(false);
			}
			
			// at this point we should have windows instantiated and filled with groups and servos
			// all we need to do is update the fields
			if(_mode == guiMode.Control)
			{
				foreach(var pair in _servoUIControls)
				{
					if(!pair.ui.activeInHierarchy)
						continue;
					UpdateServoReadoutsFlight(pair.s, pair.ui);
				}

				foreach(var pair in _servoGroupUIControls) 
				{
					if(!pair.Value.activeInHierarchy)
						continue;
					UpdateGroupReadoutsFlight(Controller.Instance.GetInterceptor(pair.Key), pair.Value);
				}
			}
			else // _mode == guiMode.Editor(HighLogic.LoadedSceneIsFlight or HighLogic.LoadedSceneIsEditor)
			{
				foreach(var pair in _servoUIControls)
				{
					if(pair.ui.activeInHierarchy)
						UpdateServoReadoutsEditor(pair.s, pair.ui);
				}
			}
		}

		private void AddAppLauncherButton()
		{
			if((appLauncherButton != null) || !ApplicationLauncher.Ready ||(ApplicationLauncher.Instance == null))
				return;

			try
			{
				Texture2D texture = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_button");

				appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
					ShowIRWindow,
					HideIRWindow,
					null, null, null, null,
					ApplicationLauncher.AppScenes.NEVER,
					texture);

				ApplicationLauncher.Instance.AddOnHideCallback(OnHideCallback);
			}
			catch(Exception ex)
			{
				Logger.Log(string.Format("[GUI AddAppLauncherButton Exception, {0}", ex.Message), Logger.Level.Error);
			}

			Invalidate();
		}

		private void OnHideCallback()
		{
			try
			{
				appLauncherButton.SetFalse(false);
			}
			catch(Exception)
			{}

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
				if(appLauncherButton != null && ApplicationLauncher.Instance != null)
				{
					ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
					appLauncherButton = null;
				}

				if(ApplicationLauncher.Instance != null)
					ApplicationLauncher.Instance.RemoveOnHideCallback(OnHideCallback);
			}
			catch(Exception e)
			{
				Logger.Log("[GUI] Failed unregistering AppLauncher handlers," + e.Message);
			}
		}

		private void OnDestroy()
		{
			Logger.Log("[GUI] destroy");

			KeyboardLock(false);
			SaveConfigXml();

			if(_controlWindow)
			{
				_controlWindow.DestroyGameObject();
				_controlWindow = null;
				_controlWindowFader = null;
			}

			if(_editorWindow)
			{
				_editorWindow.DestroyGameObject();
				_editorWindow = null;
				_editorWindowFader = null;
			}

			if(_settingsWindow)
			{
				_settingsWindow.DestroyGameObject();
				_settingsWindow = null;
				_settingsWindowFader = null;
			}

			if(_presetsWindow)
			{
				_presetsWindow.DestroyGameObject();
				_presetsWindow = null;
				_presetsWindowFader = null;
			}

			GameEvents.onGUIApplicationLauncherReady.Remove(AddAppLauncherButton);
			GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
			DestroyAppLauncherButton();

			GameEvents.onShowUI.Remove(OnShowUI);
			GameEvents.onHideUI.Remove(OnHideUI);

			Logger.Log("[GUI] OnDestroy finished successfully", Logger.Level.Debug);
		}

		internal void KeyboardLock(Boolean apply)
		{
			
			if(apply) // only do this lock in the editor - no point elsewhere
			{
				//only add a new lock if there isnt already one there
				if(InputLockManager.GetControlLock("IRKeyboardLock") != ControlTypes.KEYBOARDINPUT)
				{
					Logger.Log(String.Format("[GUI] AddingLock-{0}", "IRKeyboardLock"), Logger.Level.SuperVerbose);

					InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, "IRKeyboardLock");
				}
			}
			
			else // otherwise make sure the lock is removed
			{
				// only try and remove it if there was one there in the first place
				if(InputLockManager.GetControlLock("IRKeyboardLock") == ControlTypes.KEYBOARDINPUT)
				{
					Logger.Log(String.Format("[GUI] Removing-{0}", "IRKeyboardLock"), Logger.Level.SuperVerbose);
					InputLockManager.RemoveControlLock("IRKeyboardLock");
				}
			}

			isKeyboardLocked = apply;
		}

		public static Vector3 ClampWindowPosition(Vector3 windowPosition)
		{
			Canvas canvas = UIMasterController.Instance.appCanvas;
			RectTransform canvasRectTransform = canvas.transform as RectTransform;

			var windowPositionOnScreen = RectTransformUtility.WorldToScreenPoint(UIMasterController.Instance.uiCamera, windowPosition);

			float clampedX = Mathf.Clamp(windowPositionOnScreen.x, 0, Screen.width);
			float clampedY = Mathf.Clamp(windowPositionOnScreen.y, 0, Screen.height);

			windowPositionOnScreen = new Vector2(clampedX, clampedY);

			Vector3 newWindowPosition;
			if(RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, 
				   windowPositionOnScreen, UIMasterController.Instance.uiCamera, out newWindowPosition))
				return newWindowPosition;
			else
				return Vector3.zero;
		}

		private void OnSave()
		{
			SaveConfigXml();
		}

		public void SaveConfigXml()
		{
			if(_controlWindow)
				_controlWindowPosition = _controlWindow.transform.position;

			if(_editorWindow)
			{
				_editorWindowPosition = _editorWindow.transform.position;
				_editorWindowSize = _editorWindow.GetComponent<RectTransform>().sizeDelta;
			}

			if(_settingsWindow)
				_settingsWindowPosition = _settingsWindow.transform.position;
			
			PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
			config.load();

			config.SetValue("controlWindowPosition", _controlWindowPosition);
			config.SetValue("editorWindowPosition", _editorWindowPosition);
			config.SetValue("editorWindowSize", _editorWindowSize);
			config.SetValue("uiSettingsWindowPosition", _settingsWindowPosition);
			config.SetValue("presetsWindowPosition", _presetsWindowPosition);
			config.SetValue("UIAlphaValue",(double) _UIAlphaValue);
			config.SetValue("UIScaleValue",(double) _UIScaleValue);
			config.SetValue("useEC", UseElectricCharge);

			config.save();
		}

		public void LoadConfigXml()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
			config.load();

			_controlWindowPosition = config.GetValue<Vector3>("controlWindowPosition");
			_editorWindowPosition = config.GetValue<Vector3>("editorWindowPosition");
			_editorWindowSize = config.GetValue<Vector2>("editorWindowSize");
			_settingsWindowPosition = config.GetValue<Vector3>("uiSettingsWindowPosition");
			_presetsWindowPosition = config.GetValue<Vector3>("presetsWindowPosition");

			_UIAlphaValue =(float) config.GetValue<double>("UIAlphaValue", 0.8);
			_UIScaleValue =(float) config.GetValue<double>("UIScaleValue", 1.0);
			UseElectricCharge = config.GetValue<bool>("useEC", true);
		}
	}
}
