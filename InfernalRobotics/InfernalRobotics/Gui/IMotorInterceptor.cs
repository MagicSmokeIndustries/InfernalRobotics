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
	public class IMotorInterceptor : IMotor
	{
		private IMotor m;
		protected Vessel v;

		public IMotorInterceptor(IMotor motor, Vessel vessel)
		{
			m = motor;
			v = vessel;
		}

		private bool IsControllable()
		{
			return (v.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED);
		}

		////////////////////////////////////////
		// Status

		public float TargetPosition
		{
			get { return m.TargetPosition; }
		}

		public float TargetSpeed
		{
			get { return m.TargetSpeed; }
		}

		public float CommandedPosition
		{
			get { return m.CommandedPosition; }
		}

		public float CommandedSpeed
		{
			get { return m.CommandedSpeed; }
		}

		public float Position
		{
			get { return m.Position; }
		}

		public bool IsMoving
		{
			get { return m.IsMoving; }
		}

		public bool IsLocked
		{
			get { return m.IsLocked; }
			set { if(IsControllable()) m.IsLocked = value; }
		}

		////////////////////////////////////////
		// Settings

		public bool IsInverted
		{
			get { return m.IsInverted; }
			set { if(IsControllable()) m.IsInverted = value; }
		}

		public float TorqueLimit
		{
			get { return m.TorqueLimit; }
			set { if(IsControllable()) m.TorqueLimit = value; }
		}

		public float AccelerationLimit
		{
			get { return m.AccelerationLimit; }
			set { if(IsControllable()) m.AccelerationLimit = value; }
		}

		public float SpeedLimit
		{
			get { return m.SpeedLimit; }
			set { if(IsControllable()) m.SpeedLimit = value; }
		}

		public float DefaultSpeed
		{
			get { return m.DefaultSpeed; }
			set { if(IsControllable()) m.DefaultSpeed = value; }
		}

		////////////////////////////////////////
		// Input

		// question: should a servo stop, when the signal is lost? always? or only when some keys are pressed? -> currently it stops when the arrow keys are used to move

		public void MoveLeft()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) m.MoveLeft(); else m.Stop(); }
			else
				m.EditorMoveLeft();
		}

		public void MoveCenter()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) m.MoveCenter(); else m.Stop(); }
			else
				m.EditorMoveCenter();
		}

		public void MoveRight()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) m.MoveRight(); else m.Stop(); }
			else
				m.EditorMoveRight();
		}

		public void MoveTo(float position)
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) m.MoveTo(position); }
			else
				m.EditorSetTo(position);
		}

		public void MoveTo(float position, float speed)
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) m.MoveTo(position, speed); }
			else
				m.EditorSetTo(position);
		}

		public void Stop()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) m.Stop(); }
		}

		public string ForwardKey
		{
			get { return m.ForwardKey; }
			set { m.ForwardKey = value; }
		}

		public string ReverseKey
		{
			get { return m.ReverseKey; }
			set { m.ReverseKey = value; }
		}

		////////////////////////////////////////
		// Editor

		public void EditorMoveLeft()
		{ m.EditorMoveLeft(); }

		public void EditorMoveCenter()
		{ m.EditorMoveCenter(); }

		public void EditorMoveRight()
		{ m.EditorMoveRight(); }

		public void EditorMove(float position)
		{ m.EditorMove(position); }

		public void EditorSetTo(float position)
		{ m.EditorSetTo(position); }
	}
}
