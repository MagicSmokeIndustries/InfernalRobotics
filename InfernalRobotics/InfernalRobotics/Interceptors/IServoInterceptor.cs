using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;

namespace InfernalRobotics_v3.Interceptors
{
	public class IServoInterceptor : IServo
	{
		private IServo s;
		protected Vessel v;

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
		{
			s = servo;
			v = servo.HostPart.vessel;

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

		public float TargetPosition
		{
			get { return s.TargetPosition; }
		}

		public float TargetSpeed
		{
			get { return s.TargetSpeed; }
		}

		public float CommandedPosition
		{
			get { return s.CommandedPosition; }
		}

		public float CommandedSpeed
		{
			get { return s.CommandedSpeed; }
		}

		public float Position
		{
			get { return s.Position; }
		}

		public bool IsMoving
		{
			get { return s.IsMoving; }
		}

		////////////////////////////////////////
		// Settings

		public ModeType Mode
		{
			get { return s.Mode; }
			set { if(IsControllable()) s.Mode = value; }
		}

		public InputModeType InputMode
		{
			get { return s.InputMode; }
			set { if(IsControllable()) s.InputMode = value; }
		}

		public bool IsLocked
		{
			get { return s.IsLocked; }
			set { if(IsControllable()) s.IsLocked = value; }
		}

		////////////////////////////////////////
		// Settings (servo)

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

		public void AddPresetPosition(float position)
		{
			if(IsControllable()) s.AddPresetPosition(position);
		}

		public void RemovePresetPositionsAt(int presetIndex)
		{
			if(IsControllable()) s.RemovePresetPositionsAt(presetIndex);
		}

		public void SortPresetPositions(IComparer<float> sorter = null)
		{
			if(IsControllable()) s.SortPresetPositions(sorter);
		}

		public float DefaultPosition
		{
			get { return s.DefaultPosition; }
			set { if(IsControllable()) s.DefaultPosition = value; }
		}

		public float ForceLimit
		{
			get { return s.ForceLimit; }
			set { if(IsControllable()) s.ForceLimit = value; }
		}

		public float AccelerationLimit
		{
			get { return s.AccelerationLimit; }
			set { if(IsControllable()) s.AccelerationLimit = value; }
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

		public bool IsLimitted
		{
			get { return s.IsLimitted; }
			set { if(IsControllable()) s.IsLimitted = value; }
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

		public string ForwardKey
		{
			get { return s.ForwardKey; }
			set { s.ForwardKey = value; }
		}

		public string ReverseKey
		{
			get { return s.ReverseKey; }
			set { s.ReverseKey = value; }
		}

		////////////////////////////////////////
		// Settings (servo - control input)

		public float ControlDeflectionRange
		{
			get { return s.ControlDeflectionRange; }
			set { if(IsControllable()) s.ControlDeflectionRange = value; }
		}

		public float ControlNeutralPosition
		{
			get { return s.ControlNeutralPosition; }
			set { if(IsControllable()) s.ControlNeutralPosition = value; }
		}

		public float PitchControl
		{
			get { return s.PitchControl; }
			set { if(IsControllable()) s.PitchControl = value; }
		}
		
		public float RollControl
		{
			get { return s.RollControl; }
			set { if(IsControllable()) s.RollControl = value; }
		}
		
		public float YawControl
		{
			get { return s.YawControl; }
			set { if(IsControllable()) s.YawControl = value; }
		}
		
		public float ThrottleControl
		{
			get { return s.ThrottleControl; }
			set { if(IsControllable()) s.ThrottleControl = value; }
		}
		
		public float XControl
		{
			get { return s.XControl; }
			set { if(IsControllable()) s.XControl = value; }
		}
		
		public float YControl
		{
			get { return s.YControl; }
			set { if(IsControllable()) s.YControl = value; }
		}
		
		public float ZControl
		{
			get { return s.ZControl; }
			set { if(IsControllable()) s.ZControl = value; }
		}

		////////////////////////////////////////
		// Settings (servo - link input)

		public void LinkInput()
		{
			if(IsControllable()) s.LinkInput();
		}

		////////////////////////////////////////
		// Settings (servo - track input)

		public bool TrackSun
		{
			get { return s.TrackSun; }
			set { if(IsControllable()) s.TrackSun = value; }
		}

		public float TrackAngle
		{
			get { return s.TrackAngle; }
			set { if(IsControllable()) s.TrackAngle = value; }
		}

		////////////////////////////////////////
		// Settings (rotor)

		public float RotorAcceleration
		{
			get { return s.RotorAcceleration; }
			set { if(IsControllable()) s.RotorAcceleration = value; }
		}

		public float BaseSpeed
		{
			get { return s.BaseSpeed; }
			set { if(IsControllable()) s.BaseSpeed = value; }
		}

		public float PitchSpeed
		{
			get { return s.PitchSpeed; }
			set { if(IsControllable()) s.PitchSpeed = value; }
		}

		public float RollSpeed
		{
			get { return s.RollSpeed; }
			set { if(IsControllable()) s.RollSpeed = value; }
		}

		public float YawSpeed
		{
			get { return s.YawSpeed; }
			set { if(IsControllable()) s.YawSpeed = value; }
		}

		public float ThrottleSpeed
		{
			get { return s.ThrottleSpeed; }
			set { if(IsControllable()) s.ThrottleSpeed = value; }
		}

		public float XSpeed
		{
			get { return s.XSpeed; }
			set { if(IsControllable()) s.XSpeed = value; }
		}

		public float YSpeed
		{
			get { return s.YSpeed; }
			set { if(IsControllable()) s.YSpeed = value; }
		}

		public float ZSpeed
		{
			get { return s.ZSpeed; }
			set { if(IsControllable()) s.ZSpeed = value; }
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

		public bool IsServo
		{
			get { return s.IsServo; }
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

		public bool HasSpring
		{
			get { return s.HasSpring; }
		}

		////////////////////////////////////////
		// Input (servo)

		public void MoveLeft()
		{
			if(IsControllable()) s.MoveLeft();
		}

		public void MoveCenter()
		{
			if(IsControllable()) s.MoveCenter();
		}

		public void MoveRight()
		{
			if(IsControllable()) s.MoveRight();
		}

		public void Move(float deltaPosition, float targetSpeed)
		{
			if(IsControllable()) s.Move(deltaPosition, targetSpeed);
		}

		public void MoveTo(float position)
		{
			if(IsControllable()) s.MoveTo(position);
		}

		public void MoveTo(float position, float speed)
		{
			if(IsControllable()) s.MoveTo(position, speed);
		}

		public void Stop()
		{
			if(IsControllable()) s.Stop();
		}

		public void SetRelaxMode(float relaxFactor)
		{
			if(IsControllable()) s.SetRelaxMode(relaxFactor);
		}

		public void ResetRelaxMode()
		{
			if(IsControllable()) s.ResetRelaxMode();
		}

		public bool RelaxStep()
		{
			if(IsControllable()) return s.RelaxStep();
			return true;
		}

		////////////////////////////////////////
		// Input (rotor)

		public bool IsRunning
		{
			get { return s.IsRunning; }
			set { if(IsControllable()) s.IsRunning = value; }
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

		public void DoTransformStuff(Transform trf)
		{ s.DoTransformStuff(trf); }
	}
}
