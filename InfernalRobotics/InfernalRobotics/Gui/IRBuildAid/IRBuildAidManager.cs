using InfernalRobotics_v3.Command;
using InfernalRobotics_v3.Control;
using InfernalRobotics_v3.Control.Servo;
using InfernalRobotics_v3.Utility;
using InfernalRobotics_v3.Module;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics_v3.Gui.IRBuildAid
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class IRBuildAidManager : MonoBehaviour
	{
		private static IRBuildAidManager instance;

		// for min/max (green)
		public static Color endPoint1Color = new Color(0.04f, 0.77f, 0.15f, 0.5f);
		public static Color endPoint2Color = new Color(0.04f, 0.77f, 0.15f, 0.5f);
		public static Color mainLineColor1 = new Color(0.46f, .85f, 0.42f, 0.7f);
		public static Color mainLineColor2 = new Color(0.28f, 0.82f, 0.22f, 0.5f);

		// for limits (yellow)
		public static Color endPoint1LimitColor = new Color(1f, 1f, 0, 0.5f);
		public static Color endPoint2LimitColor = new Color(1f, 1f, 0, 0.5f);
		public static Color mainLineLimitColor1 = new Color(0.88f, 0.7f, 0.188f, 0.7f);
		public static Color mainLineLimitColor2 = new Color(0.7f, 0.5f, 0, 0.5f);

		public static Color presetPositionsColor = new Color(1f, 1f, 1f, 0.5f);

		public static Color currentPositionColor = new Color(0f, 1f, 0f, 0.5f);
		public static Color currentPositionLockedColor = new Color(1f, 0f, 0f, 0.5f);

		public static IRBuildAidManager Instance 
		{
			get { return instance; }
		}

		public static Dictionary<IServo, LinePrimitive> lines;

		private static bool hiddenStatus = false;

		public IRBuildAidManager()
		{
			lines = new Dictionary<IServo, LinePrimitive>();
			instance = this;
		}

		public static bool isHidden
		{
			get { return hiddenStatus; }
			set
			{
				if(lines == null || lines.Count == 0)
					return;

				foreach(var pair in lines)
					pair.Value.enabled = value;
			}
		}

		public static void Reset()
		{
			if(lines == null || lines.Count == 0)
				return;

			foreach(var pair in lines)
				Destroy(pair.Value.gameObject);
			lines.Clear();
		}

		public void ShowServoRange(IServo s)
		{
			if(lines.ContainsKey(s))
			{
				UpdateServoRange(s);
				lines[s].enabled = true;
				return;
			}

			var obj = new GameObject("Servo IRBuildAid object");
			obj.layer = 1;

			if(s.IsRotational)
			{
				CircularInterval civ = obj.AddComponent<CircularInterval>();

				civ.isInverted = !s.IsInverted;

				civ.UpdateWidth(civ.width = 0.05f);
				civ.UpdateColor(mainLineLimitColor1);

				civ.enabled = true;

				lines.Add(s, civ);
			}
			else
			{
				BasicInterval biv = obj.AddComponent<BasicInterval>();

				biv.isInverted = !s.IsInverted;

				biv.UpdateWidth(biv.width = 0.05f);
				biv.UpdateColor(mainLineLimitColor1);

				biv.enabled = true;

				lines.Add(s, biv);
			}

			UpdateServoRange(s);
		}

		public void HideServoRange(IServo s)
		{
			if(lines == null || lines.Count == 0)
				return;

			LinePrimitive aid;
			if(lines.TryGetValue(s, out aid))
			{
				lines.Remove(s);
				Destroy(aid.gameObject);
			}
		}

		public void UpdateServoRange(IServo s)
		{
			if(!lines.ContainsKey(s))
				return;
			
			BasicInterval currentRange = (BasicInterval)lines[s];

			if(s.IsLimitted)
			{
				currentRange.length = (s.MaxPositionLimit - s.MinPositionLimit);
				currentRange.offset = s.MinPositionLimit;
			}
			else
			{
				currentRange.length = (s.MaxPosition - s.MinPosition);
				currentRange.offset = s.MinPosition;
			}

			if(s.IsInverted != currentRange.isInverted)
			{
				s.DoTransformStuff(currentRange.transform);

				if(!s.IsRotational)
				{
					if(s.IsInverted)
						currentRange.transform.position += currentRange.transform.forward * s.MaxPosition;
					
					currentRange.transform.position += currentRange.transform.right * (s.IsInverted ? -0.5f : 0.5f);
				}

				currentRange.isInverted = s.IsInverted;
			}

			currentRange.currentPosition = s.Position;
			currentRange.defaultPosition = s.DefaultPosition;

			if(s.PresetPositions != null)
				currentRange.SetPresetPositions(s.PresetPositions);

			if(s.IsLimitted)
			{
				if(s.Motor.IsAxisInverted)
					currentRange.SetMainLineColors(mainLineLimitColor2, mainLineLimitColor1);
				else
					currentRange.SetMainLineColors(mainLineLimitColor1, mainLineLimitColor2);

				currentRange.endPoint1Color = endPoint1LimitColor;
				currentRange.endPoint2Color = endPoint2LimitColor;
			}
			else
			{
				if(s.Motor.IsAxisInverted)
					currentRange.SetMainLineColors(mainLineColor2, mainLineColor1);
				else
					currentRange.SetMainLineColors(mainLineColor1, mainLineColor2);

				currentRange.endPoint1Color = endPoint1Color;
				currentRange.endPoint2Color = endPoint2Color;
			}

			currentRange.currentPositionColor = s.IsLocked ? currentPositionLockedColor : currentPositionColor;

			currentRange.presetPositionsColor = presetPositionsColor;
			if(s.PresetPositions != null)
				currentRange.SetPresetPositions(s.PresetPositions);
		}

		public void Update()
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			if(Controller.Instance != null)
			{
				if(Input.GetMouseButtonDown(2) && Input.GetKey(KeyCode.LeftShift)) 
				{
					RaycastHit hit;
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

					if(Physics.Raycast(ray, out hit)) 
					{
						GameObject hitObject = hit.transform.gameObject;
						if(hitObject == null)
							return;

						Part part = hitObject.GetComponentInParent<Part>();
						if(part == null)
							return;

						var servos = part.ToServos();

						if(servos.Count > 0)
							ShowServoRange(servos[0]);	
					}
				}

				foreach(var pair in lines) 
					UpdateServoRange(pair.Key);
			}
		}

		public void OnDestroy()
		{
			foreach(var pair in lines)
				Destroy(pair.Value.gameObject);
		}
	}
}

