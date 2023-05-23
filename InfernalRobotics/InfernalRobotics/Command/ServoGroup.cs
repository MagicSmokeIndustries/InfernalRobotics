using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Servo;
using InfernalRobotics_v3.Module;


namespace InfernalRobotics_v3.Command
{
	public class ServoGroup : IServoGroup
	{
		private Vessel vessel;
		private string name;
		private List<IServo> servos;

		private bool bDirty;

		private string forwardKey;
		private string reverseKey;
		private float groupSpeedFactor;

		private float totalElectricChargeRequirement;

		public ServoGroup(IServo servo, Vessel v, string name)
			: this(servo, name)
		{
			vessel = v;
		}

		public ServoGroup(Vessel v, string name)
			: this(name)
		{
			vessel = v;
		}

		public ServoGroup(IServo servo, string name)
			: this(name)
		{
			servos.Add(servo);
		}

		public ServoGroup(string name)
		{
			servos = new List<IServo>();

			Expanded = false;
			Name = name;
			ForwardKey = "";
			ReverseKey = "";
			GroupSpeedFactor = 1;
			bDirty = true;
		}

		public IServoGroup group
		{
			get { return this; }
		}

		public Vessel Vessel
		{
			get { return vessel; }
		}

		public string Name 
		{ 
			get { return name; } 
			set { 
				if(servos != null && servos.Count > 0)
				{
					foreach(IServo servo in servos)
						servo.GroupName = AddNameToList(RemoveNameFromList(servo.GroupName, name), value);
				}
				name = value;
			} 
		}

		public IList<IServo> Servos
		{
			get { return servos; }
		}

		public void AddControl(IServo servo, int index)
		{
			if(servos.Contains(servo))
				return;

			for(int i = 0; i < servo.HostPart.symmetryCounterparts.Count; i++)
			{
				if(servos.Contains((IServo)servo.HostPart.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>()))
					return;
			}

			servos.Insert(index < 0 ? servos.Count : index, servo);

			servo.GroupName = AddNameToList(servo.GroupName, Name);

			bDirty = true;
		}

		public void RemoveControl(IServo servo)
		{
			servos.Remove(servo);

			servo.GroupName = RemoveNameFromList(servo.GroupName, Name);

			bDirty = true;
		}

		public static string AddNameToList(string list, string name)
		{
			string[] listNames = list.Split('|');
			foreach(string listName in listNames)
			{
				if(listName == name)
					return list;
			}

			return (list + "|" + name).Trim('|');
		}

		public static string RemoveNameFromList(string list, string name)
		{
			string result = "";

			string[] listNames = list.Split('|');
			foreach(string listName in listNames)
			{
				if(listName != name)
					result += "|" + listName;
			}

			return result.Trim('|');
		}

		////////////////////////////////////////
		// Status

		private int iMovingDirection = 0;

		public int MovingDirection
		{
			get { return iMovingDirection; }
		}

		////////////////////////////////////////
		// Settings

		public bool Expanded { get; set; }

		public bool AdvancedMode { get; set; }

		public float GroupSpeedFactor
		{
			get { return groupSpeedFactor; }
			set
			{
				groupSpeedFactor = value;
			}
		}

		public string ForwardKey
		{
			get { return forwardKey; }
			set
			{
				forwardKey = value;
			}
		}

		public string ReverseKey
		{
			get { return reverseKey; }
			set
			{
				reverseKey = value;
			}
		}

		////////////////////////////////////////
		// Input

		private bool KeyPressed(string key)
		{
			return (key != "" && vessel == FlightGlobals.ActiveVessel
					&& InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKey(key));
		}

		private bool KeyUnPressed(string key)
		{
			return (key != "" && vessel == FlightGlobals.ActiveVessel
					&& InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKeyUp(key));
		}

		public void CheckInputs()
		{
			if(KeyPressed(forwardKey))
				MoveRight();
			else if(KeyPressed(reverseKey))
				MoveLeft();
			else if(KeyUnPressed(forwardKey) || KeyUnPressed(reverseKey))
				Stop();
		}

		////////////////////////////////////////
		// Characteristics

		public float TotalElectricChargeRequirement
		{
			get
			{
				if(bDirty) Freshen();
				return totalElectricChargeRequirement;
			}
		}

		public void MoveLeft()
		{
			iMovingDirection = -1;

			foreach(var servo in servos)
				servo.MoveLeft(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void MoveCenter()
		{
			iMovingDirection = 0;

			foreach(var servo in servos)
				servo.MoveCenter(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void MoveRight()
		{
			iMovingDirection = 1;

			foreach(var servo in servos)
				servo.MoveRight(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void MovePrevPreset()
		{
			foreach(var servo in servos)
				servo.Presets.MovePrev(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void MoveNextPreset()
		{
			foreach(var servo in servos)
				servo.Presets.MoveNext(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void Stop()
		{
			iMovingDirection = 0;

			foreach(var servo in servos)
				servo.Stop();
		}

		private void Freshen()
		{
			totalElectricChargeRequirement = servos.Where(s => s.IsFreeMoving == false).Sum (s => s.ElectricChargeRequired);

			bDirty = false;
		}

		////////////////////////////////////////
		// Editor

		public void EditorMoveLeft()
		{
			foreach(var servo in servos)
				servo.EditorMoveLeft(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void EditorMoveCenter()
		{
			foreach(var servo in servos)
				servo.EditorMoveCenter(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void EditorMoveRight()
		{
			foreach(var servo in servos)
				servo.EditorMoveRight(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void EditorMovePrevPreset()
		{
			foreach(var servo in servos)
				servo.Presets.EditorMovePrev(servo.DefaultSpeed * GroupSpeedFactor);
		}

		public void EditorMoveNextPreset()
		{
			foreach(var servo in servos)
				servo.Presets.EditorMovePrev(servo.DefaultSpeed * GroupSpeedFactor);
		}

		////////////////////////////////////////
		// BuildAid

		public bool BuildAid { get; set; }

		////////////////////////////////////////
		// IK

		public bool bLimiter = false; // FEHLER, experimentell
	}
}
