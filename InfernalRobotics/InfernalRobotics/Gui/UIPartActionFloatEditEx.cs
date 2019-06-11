using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using KSP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InfernalRobotics_v3.Gui
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
	public class UI_FloatEditEx : UI_Control
	{
		private const string UIControlName = "FloatEditEx";

		public float minValue = float.NegativeInfinity;
		public float maxValue = float.PositiveInfinity;

		public float incrementLarge;
		public float incrementSmall;
		public float incrementSlide;

		public bool useSI;

		public string unit = string.Empty;

		public int sigFigs;

		public override void Load(ConfigNode node, object host)
		{
			base.Load(node, host);
		}

		public override void Save(ConfigNode node, object host)
		{
			base.Save(node, host);
		}
	}

	[UI_FloatEditEx]
	public class UIPartActionFloatEditEx : UIPartActionFieldItem
	{
		public TextMeshProUGUI fieldName;
		public TextMeshProUGUI fieldValue;

		public UIButtonToggle incLarge;
		public UIButtonToggle incSmall;
		public UIButtonToggle decLarge;
		public UIButtonToggle decSmall;
		public Slider slider;

		private bool blockSliderUpdate;

		protected UI_FloatEditEx floatControl
		{ get { return (UI_FloatEditEx)control; } }

		public static Type VersionTaggedType(Type baseClass)
		{
			var ass = baseClass.Assembly;
			Type tagged = ass.GetTypes().Where(t => t.BaseType == baseClass).Where(t => t.FullName.StartsWith(baseClass.FullName)).FirstOrDefault();
			if(tagged != null)
				return tagged;
			return baseClass;
		}

		internal static T GetTaggedComponent<T>(GameObject go) where T : Component
		{
			return (T)go.GetComponent(VersionTaggedType(typeof(T)));
		}

		public static void InstantiateRecursive2(GameObject go, GameObject goc, ref Dictionary<GameObject, GameObject> list)
		{
			for(int i = 0; i < go.transform.childCount; i++)
			{
				list.Add(go.transform.GetChild(i).gameObject, goc.transform.GetChild(i).gameObject);
				InstantiateRecursive2(go.transform.GetChild(i).gameObject, goc.transform.GetChild(i).gameObject, ref list);
			}
		}

		public static void InstantiateRecursive(GameObject go, Transform trfp, ref Dictionary<GameObject, GameObject> list)
		{
			for(int i = 0; i < go.transform.childCount; i++)
			{
				GameObject goc = Instantiate(go.transform.GetChild(i).gameObject);
				goc.transform.parent = trfp;
				goc.transform.localPosition = go.transform.GetChild(i).localPosition;
				if((goc.transform is RectTransform) && (go.transform.GetChild(i) is RectTransform))
				{
					RectTransform rtc = goc.transform as RectTransform;
					RectTransform rt = go.transform.GetChild(i) as RectTransform;

					rtc.offsetMax = rt.offsetMax;
					rtc.offsetMin = rt.offsetMin;
				}
				list.Add(go.transform.GetChild(i).gameObject, goc);
				InstantiateRecursive2(go.transform.GetChild(i).gameObject, goc, ref list);
			}
		}

        public static UIPartActionFloatEditEx CreateTemplate()
        {
			// create the control
			GameObject editGo = new GameObject("UIPartActionFloatEditEx", VersionTaggedType(typeof(UIPartActionFloatEditEx)));
			UIPartActionFloatEditEx edit = GetTaggedComponent<UIPartActionFloatEditEx>(editGo);
			editGo.SetActive(false);

			// find template
			UIPartActionFloatEdit paFlt = (UIPartActionFloatEdit)UIPartActionController.Instance.fieldPrefabs.Find(cls => cls.GetType() == typeof(UIPartActionFloatEdit));


			editGo.AddComponent<RectTransform>();

			RectTransform rtc = editGo.transform as RectTransform;
			RectTransform rt = paFlt.transform as RectTransform;

			rtc.offsetMin = rt.offsetMin;
			rtc.offsetMax = rt.offsetMax;
			rtc.anchorMin = rt.anchorMin;
			rtc.anchorMax = rt.anchorMax;
			rtc.pivot = rt.pivot;


			LayoutElement lec = editGo.AddComponent<LayoutElement>();
			LayoutElement le = paFlt.GetComponent<LayoutElement>();

			lec.flexibleHeight = le.flexibleHeight;
			lec.flexibleWidth = le.flexibleWidth;
			lec.minHeight = le.minHeight;
			lec.minWidth = le.minWidth;
			lec.preferredHeight = le.preferredHeight;
			lec.preferredWidth = le.preferredWidth;
			lec.layoutPriority = le.layoutPriority;

			// copy control parts
			Dictionary<GameObject, GameObject> list = new Dictionary<GameObject,GameObject>();

			InstantiateRecursive(paFlt.gameObject, editGo.transform, ref list);

			GameObject fieldNameGo;
			list.TryGetValue(paFlt.fieldName.gameObject, out fieldNameGo);
			edit.fieldName = fieldNameGo.GetComponent<TextMeshProUGUI>();

			GameObject fieldValueGo;
			list.TryGetValue(paFlt.fieldValue.gameObject, out fieldValueGo);
			edit.fieldValue = fieldValueGo.GetComponent<TextMeshProUGUI>();

			GameObject incLargeGo;
			list.TryGetValue(paFlt.incLarge.gameObject, out incLargeGo);
			edit.incLarge = incLargeGo.GetComponent<UIButtonToggle>();

			GameObject incSmallGo;
			list.TryGetValue(paFlt.incSmall.gameObject, out incSmallGo);
			edit.incSmall = incSmallGo.GetComponent<UIButtonToggle>();

			GameObject decLargeGo;
			list.TryGetValue(paFlt.decLarge.gameObject, out decLargeGo);
			edit.decLarge = decLargeGo.GetComponent<UIButtonToggle>();

			GameObject decSmallGo;
			list.TryGetValue(paFlt.decSmall.gameObject, out decSmallGo);
			edit.decSmall = decSmallGo.GetComponent<UIButtonToggle>();

			GameObject sliderGo;
			list.TryGetValue(paFlt.slider.gameObject, out sliderGo);
			edit.slider = sliderGo.GetComponent<Slider>();

            return edit;
        }

		public override void Setup(UIPartActionWindow window, Part part, PartModule partModule, UI_Scene scene, UI_Control control, BaseField field)
		{
			base.Setup(window, part, partModule, scene, control, field);
			float value = GetFieldValue();
			value = Clamp(value);
			value = UpdateSlider(value);
			SetFieldValue(value);
			UpdateDisplay(value, null);
			fieldName.text = field.guiName;
			incLarge.onToggle.AddListener(OnTap_incLarge);
			incSmall.onToggle.AddListener(OnTap_incSmall);
			decLarge.onToggle.AddListener(OnTap_decLarge);
			decSmall.onToggle.AddListener(OnTap_decSmall);
			slider.onValueChanged.AddListener(OnValueChanged);
		}

		private float GetFieldValue()
		{
			return field.GetValue<float>(field.host);
		}

		private float UpdateSlider(float value)
		{
			if(floatControl.incrementSlide != 0f)
				value = Mathf.Round(value / floatControl.incrementSlide) * floatControl.incrementSlide;
			return value;
		}

		private float IntervalBase(float value, float increment)
		{
			float num = Mathf.Floor((value + floatControl.incrementSlide / 2f) / increment) * increment;
			if(num > floatControl.maxValue - increment)
				num = floatControl.maxValue - increment;
			return num;
		}

		private void SliderInterval(float value, out float min, out float max)
		{
			if(floatControl.incrementLarge == 0f)
			{
				min = floatControl.minValue;
				max = floatControl.maxValue;
			}
			else if(floatControl.incrementSmall == 0f)
			{
				min = IntervalBase(value, floatControl.incrementLarge);
				max = min + floatControl.incrementLarge;
			}
			else
			{
				min = IntervalBase(value, floatControl.incrementSmall);
				max = min + floatControl.incrementSmall;
			}
			min = Mathf.Max(min, floatControl.minValue);
			max = Mathf.Min(max, floatControl.maxValue);
		}

		private void UpdateControlStates()
		{
			RectTransform component = slider.gameObject.GetComponent<RectTransform>();
			Vector2 sizeDelta = default(Vector2);
			sizeDelta.y = 0f;
			bool active;
			bool active2;
			if(floatControl.incrementLarge == 0f)
			{
				active = false;
				active2 = false;
				sizeDelta.x = 0f;
			}
			else if (floatControl.incrementSmall == 0f)
			{
				active = true;
				active2 = false;
				sizeDelta.x = -44f;
			}
			else
			{
				active = true;
				active2 = true;
				sizeDelta.x = -76f;
			}
			incLarge.gameObject.SetActive(active);
			decLarge.gameObject.SetActive(active);
			incSmall.gameObject.SetActive(active2);
			decSmall.gameObject.SetActive(active2);
			Vector2 sizeDelta2 = component.sizeDelta;
			if(sizeDelta2.x != sizeDelta.x)
				component.sizeDelta = sizeDelta;
		}

		private void UpdateDisplay(float value, UIButtonToggle button)
		{
			string unit = floatControl.unit;
			int sigFigs = floatControl.sigFigs;
			string text;
			if(floatControl.useSI)
				text = KSPUtil.PrintSI(value, unit, sigFigs);
			else
				text = KSPUtil.LocalizeNumber(value, "F" + sigFigs) + unit;
			fieldValue.text = text;
			float min, max;
			SliderInterval(value, out min, out max);
			blockSliderUpdate = true;
			slider.minValue = min;
			slider.maxValue = max;
			slider.value = value;
			blockSliderUpdate = false;
			if((bool)button)
				button.SetState(on: false);
			UpdateControlStates();
		}

		private float Clamp(float value)
		{
			value = Mathf.Min(value, floatControl.maxValue);
			value = Mathf.Max(value, floatControl.minValue);
			return value;
		}

		private float AdjustValue(float value, bool up, float increment)
		{
			if(increment == 0f)
				return value;

			float num = value % increment;
			if(num < 0f)
				num += increment;
			value -= num;
			if(up)
			{
				value += increment;
				if(increment - num < floatControl.incrementSlide / 2f)
					value += increment;
			}
			else if(num < floatControl.incrementSlide / 2f)
				value -= increment;
			value = Clamp(value);
			return value;
		}

		public void OnTap_incLarge()
		{
			if((control != null) && control.requireFullControl)
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_FULLONLY))
					return;
			}
			else
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_ANYCONTROL))
					return;
			}
			float value = GetFieldValue();
			value = AdjustValue(value, true, floatControl.incrementLarge);
			UpdateDisplay(value, null);
			SetFieldValue(value);
		}

		public void OnTap_incSmall()
		{
			if((control != null) && control.requireFullControl)
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_FULLONLY))
					return;
			}
			else
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_ANYCONTROL))
					return;
			}
			float value = GetFieldValue();
			value = AdjustValue(value, true, floatControl.incrementSmall);
			UpdateDisplay(value, null);
			SetFieldValue(value);
		}

		public void OnTap_decLarge()
		{
			if((control != null) && control.requireFullControl)
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_FULLONLY))
					return;
			}
			else
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_ANYCONTROL))
					return;
			}
			float value = GetFieldValue();
			value = AdjustValue(value, false, floatControl.incrementLarge);
			UpdateDisplay(value, null);
			SetFieldValue(value);
		}

		public void OnTap_decSmall()
		{
			if((control != null) && control.requireFullControl)
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_FULLONLY))
					return;
			}
			else
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_ANYCONTROL))
					return;
			}
			float value = GetFieldValue();
			value = AdjustValue(value, false, floatControl.incrementSmall);
			UpdateDisplay(value, null);
			SetFieldValue(value);
		}

		public void OnValueChanged(float obj)
		{
			if(blockSliderUpdate)
				return;

			if((control != null) && control.requireFullControl)
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_FULLONLY))
					return;
			}
			else
			{
				if(!InputLockManager.IsUnlocked(ControlTypes.TWEAKABLES_ANYCONTROL))
					return;
			}
			float value = slider.value;
			value = UpdateSlider(value);
			UpdateDisplay(value, null);
			SetFieldValue(value);
		}

		public override void UpdateItem()
		{
			float value = GetFieldValue();
			fieldName.text = field.guiName;
			UpdateDisplay(value, null);
		}
	}

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class UIPartActionFloatEditExRegistration : MonoBehaviour
	{
		private static bool loaded = false;
		private bool isRunning = false;

		public void Start()
		{
			if(loaded)
			{
				Destroy(gameObject);
				return;
			}
			loaded = true;

			DontDestroyOnLoad(gameObject);
		}

		public void OnLevelWasLoaded(int level)
		{
			if(isRunning)
				StopCoroutine("Register");
			if(!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
				return;
			isRunning = true;
			StartCoroutine("Register");
		}

		internal IEnumerator Register()
		{
			UIPartActionController controller;
			while((controller = UIPartActionController.Instance) == null)
				yield return null;

			FieldInfo typesField = (from fld in controller.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
									where fld.FieldType == typeof(List<Type>)
									select fld).First();
			List<Type> fieldPrefabTypes;
			while((fieldPrefabTypes = (List<Type>)typesField.GetValue(controller)) == null
				|| fieldPrefabTypes.Count == 0 
				|| !UIPartActionController.Instance.fieldPrefabs.Find(cls => cls.GetType() == typeof(UIPartActionFloatEdit)))
				yield return false;

			// register prefabs
			controller.fieldPrefabs.Add(UIPartActionFloatEditEx.CreateTemplate());
			fieldPrefabTypes.Add(typeof(UI_FloatEditEx));

			isRunning = false;
		}
	}
}
