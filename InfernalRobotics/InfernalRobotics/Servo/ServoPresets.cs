using System.Collections.Generic;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Module;


namespace InfernalRobotics_v3.Servo
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
			Servo.AddPresetPosition(position == null ? Servo.CommandedPosition : position.Value);
		}

		public void RemoveAt(int presetIndex)
		{
			Servo.RemovePresetPositionsAt(presetIndex);
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
			Servo.SortPresetPositions(sorter);
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

			Servo.MoveTo(nextPosition, Servo.DefaultSpeed);

			Logger.Log("[Action] MoveToPreset, index=" + presetIndex + " currentPos = " + Servo.CommandedPosition + ", nextPosition=" + nextPosition, Logger.Level.Debug);
		}

		public void GetNearestPresets(out int floor, out int ceiling)
		{
			floor = -1;
			ceiling = -1;

			if(Servo.PresetPositions == null || Servo.PresetPositions.Count == 0)
				return;

			ceiling = Servo.PresetPositions.FindIndex(p => p > Servo.CommandedPosition + 0.005f);

			if(ceiling == -1)
				ceiling = Servo.PresetPositions.Count - 1;

			floor = Servo.PresetPositions.FindLastIndex(p => p < Servo.CommandedPosition - 0.005f);

			if(floor == -1)
				floor = 0;
		}

		////////////////////////////////////////
		// Editor

		public void EditorMovePrev()
		{
			int f, c;
			GetNearestPresets(out f, out c);
			EditorMoveTo(f);
		}

		public void EditorMoveNext()
		{
			int f, c;
			GetNearestPresets(out f, out c);
			EditorMoveTo(c);
		}

		public void EditorMoveTo(int presetIndex)
		{
			if(Servo.PresetPositions == null || Servo.PresetPositions.Count == 0
			|| presetIndex < 0 || presetIndex >= Servo.PresetPositions.Count)
				return;

			float nextPosition = Servo.PresetPositions[presetIndex];

			Servo.EditorSetTo(nextPosition);
		}
	}
}