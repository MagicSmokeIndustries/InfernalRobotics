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
	public class IMotorGroupInterceptor : IMotorGroup
	{
		private IMotorGroup h;
		protected Vessel v;

		public IMotorGroupInterceptor(IMotorGroup group, Vessel vessel)
		{
			h = group;
			v = vessel;
		}

		private bool IsControllable()
		{
			return HighLogic.LoadedSceneIsEditor || (v.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED);
		}


// FEHLER, weiss nicht genau was das ist... klären
		public bool MovingNegative
		{
			get { return h.MovingNegative; }
			set { if(IsControllable()) h.MovingNegative = value; }
		}

		public bool MovingPositive
		{
			get { return h.MovingPositive; }
			set { if(IsControllable()) h.MovingPositive = value; }
		}

		////////////////////////////////////////
		// Settings

		public float GroupSpeedFactor
		{
			get { return h.GroupSpeedFactor; }
			set { if(IsControllable()) h.GroupSpeedFactor = value; }
		}

		////////////////////////////////////////
		// Input

		// Commands the servo to move in the direction that decreases its Position
		public void MoveLeft()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) h.MoveLeft(); else h.Stop(); }
			else
				h.EditorMoveLeft();
		}

		// Comands the servo to move towards its DefaultPosition
		public void MoveCenter()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) h.MoveCenter(); else h.Stop(); }
			else
				h.EditorMoveCenter();
		}

		// Commands the servo to move in the direction that increases its Position
		public void MoveRight()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) h.MoveRight(); else h.Stop(); }
			else
				h.EditorMoveRight();
		}

// FEHLER, hab ich hier, nicht aber im IMotor... wieso das? -> dort ist es im IPresetable... -> angleichen
		public void MovePrevPreset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) h.MovePrevPreset(); else h.Stop(); }
		}

		public void MoveNextPreset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) h.MoveNextPreset(); else h.Stop(); }
		}

		// Commands the servos to stop
		public void Stop()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) h.Stop(); }
		}

		// Keybinding for servo group's MoveForward key
		public string ForwardKey
		{
			get { return h.ForwardKey; }
			set { h.ForwardKey = value; }
		}

		// Keybinding for servo group's MoveBackward key
		public string ReverseKey
		{
			get { return h.ReverseKey; }
			set { h.ReverseKey = value; }
		}

		////////////////////////////////////////
		// Editor

		public void EditorMoveLeft()
		{ h.EditorMoveLeft(); }

		public void EditorMoveCenter()
		{ h.EditorMoveCenter(); }

		public void EditorMoveRight()
		{ h.EditorMoveRight(); }
	}
}
