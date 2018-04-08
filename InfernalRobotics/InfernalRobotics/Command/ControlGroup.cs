using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Servo;
using InfernalRobotics_v3.Module;


namespace InfernalRobotics_v3.Command
{
	public class ControlGroup
	{
		private bool bDirty;

		private float totalElectricChargeRequirement;

		private float groupSpeedFactor;
		private string forwardKey;
		private string reverseKey;

// FEHLER FEHLER, supertemp
public bool bIsAdvancedOn = false;
public bool bIsBuildAidOn = false;
public class IServoState { public bool bIsBuildAidOn = false; }
public Dictionary<IServo, IServoState> servosState;

		private readonly List<IServo> servos;

		private /*readonly*/ Vessel vessel;

		public ControlGroup(IServo servo, Vessel v)
			: this(servo)
		{
			vessel = v;
		}

		public ControlGroup(IServo servo)
			: this()
		{
			Name = servo.GroupName;
			ForwardKey = servo.Motor.ForwardKey;
			ReverseKey = servo.Motor.ReverseKey;
			groupSpeedFactor = 1;

			servos.Add(servo);
			servosState.Add(servo, new IServoState());
		}

		public ControlGroup()
		{
			servos = new List<IServo>();
			servosState = new Dictionary<IServo,IServoState>();

			Expanded = false;
			Name = "New Group";
			ForwardKey = string.Empty;
			ReverseKey = string.Empty;
			GroupSpeedFactor = 1;
			MovingNegative = false;
			MovingPositive = false;
			bDirty = true;
		}

		public bool Expanded { get; set; }

		private string name = "New Group";
		public string Name 
		{ 
			get { return this.name; } 
			set { 
				this.name = value;
				if(this.servos != null && this.servos.Count > 0)
					this.servos.ForEach(s => s.GroupName = this.name);
			} 
		}

		public bool MovingNegative { get; set; }

		public bool MovingPositive { get; set; }

		public IList<IServo> Servos
		{
			get { return servos; }
		}

		public Vessel Vessel
		{
			get { return vessel; }
		}

		public void MurksBugFixVessel(Vessel v)
		{
			vessel = v;
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

		public float GroupSpeedFactor
		{
			get { return groupSpeedFactor; }
			set
			{
				groupSpeedFactor = value;
				PropogateGroupSpeedFactor();
			}
		}

		public float TotalElectricChargeRequirement
		{
			get
			{
				if(bDirty) Freshen();
				return totalElectricChargeRequirement;
			}
		}

		public void AddControl(IServo control, int index)
		{
			servos.Insert(index < 0 ? servos.Count : index, control);
			control.GroupName = Name;
			control.Motor.ForwardKey = ForwardKey;
			control.Motor.ReverseKey = ReverseKey;
			servosState.Add(control, new IServoState());
			bDirty = true;
		}

		public void RemoveControl(IServo control)
		{
			servos.Remove(control);
			servosState.Remove(control);
			bDirty = true;
		}

		public void MoveRight()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.MoveRight();
			}
		}

		public void MoveLeft()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.MoveLeft();
			}
		}

		public void MoveCenter()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.MoveCenter();
			}
		}

		public void MoveNextPreset()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Presets.MoveNext();
			}
		}

		public void MovePrevPreset()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Presets.MovePrev();
			}
		}

		public void Stop()
		{
			MovingNegative = false;
			MovingPositive = false;

			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.Stop();
			}
		}

		private void Freshen()
		{
			if(Servos == null) return;

			PropogateGroupSpeedFactor();

			totalElectricChargeRequirement = Servos.Where(s => s.IsFreeMoving == false).Sum (s => s.ElectricChargeRequired);

			bDirty = false;
		}

		private void PropogateForward()
		{
			if(Servos == null) return;

			foreach(var servo in Servos)
				servo.Motor.ForwardKey = ForwardKey;
		}

		private void PropogateReverse()
		{
			if(Servos == null) return;

			foreach(var servo in Servos)
				servo.Motor.ReverseKey = ReverseKey;
		}

		private void PropogateGroupSpeedFactor()
		{
			if(Servos == null) return;
retry:
			float minGroupSpeedFactor = groupSpeedFactor;

			foreach(var servo in Servos)
			{
				servo.GroupSpeedFactor = groupSpeedFactor;
				minGroupSpeedFactor = Mathf.Min(minGroupSpeedFactor, servo.GroupSpeedFactor);
			}

			if(groupSpeedFactor - minGroupSpeedFactor >= 0.05)
			{
				groupSpeedFactor = minGroupSpeedFactor;
				goto retry;
			}

			// FEHLER, das ist ja zwar alles gut und Recht, aber wenn ein Servo was ändert an seinen Einstellungen,
			// dann müsste man den GroupSpeedFactor auch anpassen... dieser Callback fehlt noch, weil der Servo keinen
			// Schimmer von der ControlGroup hat... das später nachrüsten... wobei, später ist der Servo evtl. ja auch
			// in mehreren Gruppen, da müssen wir sowieso nochmal kräftig umbauen
		}

		public void RefreshKeys()
		{
			foreach(var servo in Servos)
			{
				servo.Motor.ReverseKey = ReverseKey;
				servo.Motor.ForwardKey = ForwardKey;
			}
		}
	}
}
