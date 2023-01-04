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
		private readonly List<IServo> servos;

		private /*readonly*/ Vessel vessel;

		private bool bDirty;

		private string forwardKey;
		private string reverseKey;
		private float groupSpeedFactor;

		private float totalElectricChargeRequirement;

		private class IServoState { public bool bIsBuildAidOn = false; }
		private Dictionary<IServo, IServoState> servosState;

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
			Name = servo.GroupName;
			ForwardKey = servo.ForwardKey;
			ReverseKey = servo.ReverseKey;
			groupSpeedFactor = 1;

			servos.Add(servo);
			servosState.Add(servo, new IServoState());
		}

		public ServoGroup(string name)
		{
			servos = new List<IServo>();
			servosState = new Dictionary<IServo,IServoState>();

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

		private string name;
		public string Name 
		{ 
			get { return this.name; } 
			set { 
				this.name = value;
				if(this.servos != null && this.servos.Count > 0)
					this.servos.ForEach(s => s.GroupName = this.name);
			} 
		}

		public IList<IServo> Servos
		{
			get { return servos; }
		}

		public Vessel Vessel
		{
			get { return vessel; }
		}

		public void AddControl(IServo servo, int index)
		{
			if(servosState.ContainsKey(servo))
				return;

			for(int i = 0; i < servo.HostPart.symmetryCounterparts.Count; i++)
			{
				if(servosState.ContainsKey((IServo)servo.HostPart.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>()))
					return;
			}

			servos.Insert(index < 0 ? servos.Count : index, servo);
			servo.GroupName = Name;
			servo.ForwardKey = ForwardKey;
			servo.ReverseKey = ReverseKey;
			servosState.Add(servo, new IServoState());
			bDirty = true;
		}

		public void RemoveControl(IServo servo)
		{
			servos.Remove(servo);
			servosState.Remove(servo);
			bDirty = true;
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
				PropogateGroupSpeedFactor();
			}
		}

		public string ForwardKey
		{
			get { return forwardKey; }
			set
			{
				forwardKey = value;
				PropogateForward();
			}
		}

		public string ReverseKey
		{
			get { return reverseKey; }
			set
			{
				reverseKey = value;
				PropogateReverse();
			}
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
				servo.MoveLeft();
		}

		public void MoveCenter()
		{
			iMovingDirection = 0;

			foreach(var servo in servos)
				servo.MoveCenter();
		}

		public void MoveRight()
		{
			iMovingDirection = 1;

			foreach(var servo in servos)
				servo.MoveRight();
		}

		public void MovePrevPreset()
		{
			foreach(var servo in servos)
				servo.Presets.MovePrev();
		}

		public void MoveNextPreset()
		{
			foreach(var servo in servos)
				servo.Presets.MoveNext();
		}

		public void Stop()
		{
			iMovingDirection = 0;

			foreach(var servo in servos)
				servo.Stop();
		}

		private void Freshen()
		{
			PropogateGroupSpeedFactor();

			totalElectricChargeRequirement = servos.Where(s => s.IsFreeMoving == false).Sum (s => s.ElectricChargeRequired);

			bDirty = false;
		}

		private void PropogateForward()
		{
			foreach(var servo in servos)
				servo.ForwardKey = ForwardKey;
		}

		private void PropogateReverse()
		{
			foreach(var servo in servos)
				servo.ReverseKey = ReverseKey;
		}

		private void PropogateGroupSpeedFactor()
		{
			foreach(var servo in servos)
				servo.GroupSpeedFactor = groupSpeedFactor;
		}

		public void RefreshKeys()
		{
			foreach(var servo in servos)
			{
				servo.ReverseKey = ReverseKey;
				servo.ForwardKey = ForwardKey;
			}
		}

		////////////////////////////////////////
		// Editor

		public void EditorMoveLeft()
		{
			foreach(var servo in servos)
				servo.EditorMoveLeft();
		}

		public void EditorMoveCenter()
		{
			foreach(var servo in servos)
				servo.EditorMoveCenter();
		}

		public void EditorMoveRight()
		{
			foreach(var servo in servos)
				servo.EditorMoveRight();
		}

		public void EditorMovePrevPreset()
		{
			foreach(var servo in servos)
				servo.Presets.EditorMovePrev();
		}

		public void EditorMoveNextPreset()
		{
			foreach(var servo in servos)
				servo.Presets.EditorMovePrev();
		}

		////////////////////////////////////////
		// BuildAid

		public bool BuildAid { get; set; }

		public bool ServoBuildAid(IServo s)
		{
			return servosState[s.servo].bIsBuildAidOn;
		}

		public void ServoBuildAid(IServo s, bool v)
		{
			servosState[s.servo].bIsBuildAidOn = v;
		}
	}
}
