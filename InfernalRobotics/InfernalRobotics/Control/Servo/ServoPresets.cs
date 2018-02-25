using InfernalRobotics_v3.Module;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics_v3.Control.Servo
{
	internal class ServoPresets : IPresetable
	{
		private readonly IServo Servo;

		public ServoPresets(IServo Servo)
		{
			this.Servo = Servo;
		}

		public void Add(float? position = null)
		{
			Servo.PresetPositions.Add(position == null ? Servo.Position : position.Value);
		}

		public void RemoveAt(int presetIndex)
		{
			Servo.PresetPositions.RemoveAt(presetIndex);
		}

		public int Count
		{
			get { return Servo.PresetPositions.Count; }
		}

		public float this[int index]
		{
			get { return Servo.PresetPositions[index]; }
			set { Servo.PresetPositions[index] = Servo.IsLimitted ? Mathf.Clamp(value, Servo.MinPosition, Servo.MaxPosition) : value; }
		}

		public void Sort(IComparer<float> sorter = null)
		{
			if(sorter != null)
				Servo.PresetPositions.Sort(sorter);
			else
				Servo.PresetPositions.Sort();
		}

		public void CopyToSymmetry()
		{
			Servo.CopyPresetsToSymmetry();
		}

		public void MovePrev()
		{
			int f, c;
			GetNearestPresets(out f, out c);
			MoveTo(f);
		}

		public void MoveNext()
		{
			int f, c;
			GetNearestPresets(out f, out c);
			MoveTo(c);
		}

		public void MoveTo(int presetIndex)
		{
			if(Servo.PresetPositions == null || Servo.PresetPositions.Count == 0
			|| presetIndex < 0 || presetIndex >= Servo.PresetPositions.Count)
				return;

			float nextPosition = Servo.PresetPositions[presetIndex];

			if(HighLogic.LoadedSceneIsEditor)
				Servo.EditorSetTo(nextPosition);
			else
				Servo.MoveTo(nextPosition, Servo.DefaultSpeed);

			Logger.Log("[Action] MoveToPreset, index=" + presetIndex + " currentPos = " + Servo.Position + ", nextPosition=" + nextPosition, Logger.Level.Debug);
		}

		public void GetNearestPresets(out int floor, out int ceiling)
		{
			floor = -1;
			ceiling = -1;

			if(Servo.PresetPositions == null || Servo.PresetPositions.Count == 0)
				return;

			ceiling = Servo.PresetPositions.FindIndex(p => p > Servo.Position + 0.005f);

			if(ceiling == -1)
				ceiling = Servo.PresetPositions.Count - 1;

			floor = Servo.PresetPositions.FindLastIndex(p => p < Servo.Position - 0.005f);

			if(floor == -1)
				floor = 0;

	/*		if(rawServo.invertAxis)
			{
				//if axis is inverted swap two nearest presets
				var tmp = ceiling;
				ceiling = floor;
				floor = tmp;
			}*/	// FEHLER; aktuell keine Inversion drin
		}
	}
}