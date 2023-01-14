using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Command;

namespace InfernalRobotics_v3.Interceptors
{
	public class IServoGroupInterceptor : IServoGroup
	{
		private IServoGroup g;
		protected Vessel v;

		public static IServoGroup BuildInterceptor(IServoGroup group)
		{
			if(CommNet.CommNetScenario.CommNetEnabled
			&& HighLogic.CurrentGame.Parameters.CustomParams<CommNet.CommNetParams>().requireSignalForControl)
				return new IServoGroupInterceptor(group);

			return group;
		}
	
		public IServoGroupInterceptor(IServoGroup group)
		{
			g = group;
			v = group.Vessel;
		}

		public IServoGroup group
		{
			get { return g; }
		}

		private bool IsControllable()
		{
			return HighLogic.LoadedSceneIsEditor || (g.Vessel.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED);
		}

		////////////////////////////////////////
		// Properties

		public string Name
		{
			get { return g.Name; }
			set { g.Name = value; }
		}

		public IList<IServo> Servos
		{
			get
			{
				List<IServo> l = new List<IServo>(g.Servos.Count);

				foreach(IServo s in g.Servos)
					l.Add(Controller.Instance.GetInterceptor(s));

				return l;
			}
		}

		public Vessel Vessel
		{
			get { return g.Vessel; }
		}

		////////////////////////////////////////
		// Status

		public int MovingDirection
		{
			get { return g.MovingDirection; }
		}

		////////////////////////////////////////
		// Settings

		public bool Expanded
		{
			get { return g.Expanded; }
			set { g.Expanded = value; }
		}

		public bool AdvancedMode
		{
			get { return g.AdvancedMode; }
			set { g.AdvancedMode = value; }
		}

		public float GroupSpeedFactor
		{
			get { return g.GroupSpeedFactor; }
			set { if(IsControllable()) g.GroupSpeedFactor = value; }
		}

		// Keybinding for servo group's MoveForward key
		public string ForwardKey
		{
			get { return g.ForwardKey; }
			set { g.ForwardKey = value; }
		}

		// Keybinding for servo group's MoveBackward key
		public string ReverseKey
		{
			get { return g.ReverseKey; }
			set { g.ReverseKey = value; }
		}

		////////////////////////////////////////
		// Characteristics

		// Amount of EC consumed by the servos
		public float TotalElectricChargeRequirement
		{
			get { return g.TotalElectricChargeRequirement; }
		}

		////////////////////////////////////////
		// Input

		// Commands the servos to move in the direction that decreases its Position
		public void MoveLeft()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) g.MoveLeft(); else g.Stop(); }
			else
				g.EditorMoveLeft();
		}

		// Comands the servos to move towards its DefaultPosition
		public void MoveCenter()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) g.MoveCenter(); else g.Stop(); }
			else
				g.EditorMoveCenter();
		}

		// Commands the servos to move in the direction that increases its Position
		public void MoveRight()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) g.MoveRight(); else g.Stop(); }
			else
				g.EditorMoveRight();
		}

		// Orders the servos to move to the previous preset position
		public void MovePrevPreset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) g.MovePrevPreset(); else g.Stop(); }
			else
				g.EditorMovePrevPreset();
		}

		// Orders the servos to move to the next preset position
		public void MoveNextPreset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) g.MoveNextPreset(); else g.Stop(); }
			else
				g.EditorMoveNextPreset();
		}

		// Commands the servos to stop
		public void Stop()
		{
			if(!HighLogic.LoadedSceneIsEditor)
			{ if(IsControllable()) g.Stop(); }
		}

		////////////////////////////////////////
		// Editor

		public void EditorMoveLeft()
		{
			if(HighLogic.LoadedSceneIsEditor)
				g.EditorMoveLeft();
		}

		public void EditorMoveCenter()
		{
			if(HighLogic.LoadedSceneIsEditor)
				g.EditorMoveCenter();
		}

		public void EditorMoveRight()
		{
			if(HighLogic.LoadedSceneIsEditor)
				g.EditorMoveRight();
		}

		public void EditorMovePrevPreset()
		{
			if(HighLogic.LoadedSceneIsEditor)
				g.EditorMovePrevPreset();
		}

		public void EditorMoveNextPreset()
		{
			if(HighLogic.LoadedSceneIsEditor)
				g.EditorMoveNextPreset();
		}

		////////////////////////////////////////
		// BuildAid

		public bool BuildAid
		{
			get { return g.BuildAid; }
			set { g.BuildAid = value; }
		}

		public bool ServoBuildAid(IServo s)
		{
			return g.ServoBuildAid(s);
		}

		public void ServoBuildAid(IServo s, bool v)
		{
			g.ServoBuildAid(s, v);
		}
	}
}
