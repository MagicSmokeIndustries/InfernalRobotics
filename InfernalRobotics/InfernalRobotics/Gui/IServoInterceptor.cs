using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;

namespace InfernalRobotics_v3.Gui
{
	public class IServoInterceptor : IMotorInterceptor, IServo
	{
		private IServo s;

		private IPresetableInterceptor p;

		public static IServo BuildInterceptor(IServo servo)
		{
			if(CommNet.CommNetScenario.CommNetEnabled
			&& HighLogic.CurrentGame.Parameters.CustomParams<CommNet.CommNetParams>().requireSignalForControl)
				return new IServoInterceptor(servo);

			// option: build two different interceptor classes and return the "flight" or the "editor" interceptor instead of doing the if-desicion in many functions
	
			return servo;
		}
	
		public IServoInterceptor(IServo servo)
			: base(servo, servo.HostPart.vessel)
		{
			s = servo;

			p = new IPresetableInterceptor(s.Presets, v);
		}

		public IServo servo
		{
			get { return s; }
		}

		private bool IsControllable()
		{
			return HighLogic.LoadedSceneIsEditor || (v.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED);
		}

		////////////////////////////////////////
		// Properties

		public string Name
		{
			get { return s.Name; }
			set { s.Name = value; }
		}

		public uint UID
		{
			get { return s.UID; }
		}

		public Part HostPart
		{
			get { return s.HostPart; }
		}

		public bool Highlight
		{
			set { s.Highlight = value; }
		}

		public IPresetable Presets
		{
			get { return p; }
		}

		public string GroupName
		{
			get { return s.GroupName; }
			set { s.GroupName = value; }
		}

		////////////////////////////////////////
		// Status

		public float Position
		{
			get { return s.Position; }
		}

		public float CommandedPosition
		{
			get { return s.CommandedPosition; }
		}

		public float CommandedSpeed
		{
			get { return s.CommandedSpeed; }
		}

		public bool IsMoving
		{
			get { return s.IsMoving; }
		}

		public bool IsLocked
		{
			get { return s.IsLocked; }
			set { if(IsControllable()) s.IsLocked = value; }
		}

		////////////////////////////////////////
		// Settings

		public bool IsInverted
		{
			get { return s.IsInverted; }
			set { if(IsControllable()) s.IsInverted = value; }
		}

		public List<float> PresetPositions
		{
			get { return s.PresetPositions; }
			set { if(IsControllable()) s.PresetPositions = value; }
		}

		public float DefaultPosition
		{
			get { return s.DefaultPosition; }
			set { if(IsControllable()) s.DefaultPosition = value; }
		}

		public bool IsLimitted
		{
			get { return s.IsLimitted; }
		}

		public void ToggleLimits()
		{
			if(IsControllable()) s.ToggleLimits();
		}

		public float MinPositionLimit
		{
			get { return s.MinPositionLimit; }
			set { if(IsControllable()) s.MinPositionLimit = value; }
		}

		public float MaxPositionLimit
		{
			get { return s.MaxPositionLimit; }
			set { if(IsControllable()) s.MaxPositionLimit = value; }
		}

		public float ForceLimit
		{
			get { return s.ForceLimit; }
			set { if(IsControllable()) s.ForceLimit = value; }
		}

		public float DefaultSpeed
		{
			get { return s.DefaultSpeed; }
			set { if(IsControllable()) s.DefaultSpeed = value; }
		}

		public float SpeedLimit
		{
			get { return s.SpeedLimit; }
			set { if(IsControllable()) s.SpeedLimit = value; }
		}

		public float GroupSpeedFactor
		{
			get { return s.GroupSpeedFactor; }
			set { if(IsControllable()) s.GroupSpeedFactor = value; }
		}

		public float AccelerationLimit
		{
			get { return s.AccelerationLimit; }
			set { if(IsControllable()) s.AccelerationLimit = value; }
		}

		public float SpringPower
		{
			get { return s.SpringPower; }
			set { if(IsControllable()) s.SpringPower = value; }
		}

		public float DampingPower
		{
			get { return s.DampingPower; }
			set { if(IsControllable()) s.DampingPower = value; }
		}

		////////////////////////////////////////
		// Characteristics

		public bool IsRotational
		{
			get { return s.IsRotational; }
		}

		public float MinPosition
		{
			get { return s.MinPosition; }
		}

		public float MaxPosition
		{
			get { return s.MaxPosition; }
		}

		public bool IsFreeMoving
		{
			get { return s.IsFreeMoving; }
		}

		public bool CanHaveLimits
		{
			get { return s.CanHaveLimits; }
		}

		public float MaxForce
		{
			get { return s.MaxForce; }
		}

		public float MaxSpeed
		{
			get { return s.MaxSpeed; }
		}

		public float MaxAcceleration
		{
			get { return s.MaxAcceleration; }
		}

		public float ElectricChargeRequired
		{
			get { return s.ElectricChargeRequired; }
		}

		////////////////////////////////////////
		// Editor

		public void EditorReset()
		{ s.EditorReset(); }

		public void EditorMoveLeft()
		{ s.EditorMoveLeft(); }

		public void EditorMoveCenter()
		{ s.EditorMoveCenter(); }

		public void EditorMoveRight()
		{ s.EditorMoveRight(); }

		public void EditorMove(float position)
		{ s.EditorMove(position); }

		public void EditorSetTo(float position)
		{ s.EditorSetTo(position); }

		public void CopyPresetsToSymmetry()
		{ s.CopyPresetsToSymmetry(); }

		public void CopyLimitsToSymmetry()
		{ s.CopyLimitsToSymmetry(); }

		public void DoTransformStuff(Transform trf)
		{ s.DoTransformStuff(trf); }
	}
}
