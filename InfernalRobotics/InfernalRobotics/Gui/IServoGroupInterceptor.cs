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
	public class IServoGroupInterceptor : IMotorGroupInterceptor, IServoGroup
	{
		private IServoGroup g;

		public static IServoGroupInterceptor BuildInterceptor(IServoGroup group)
		{
			if(CommNet.CommNetScenario.CommNetEnabled
			&& HighLogic.CurrentGame.Parameters.CustomParams<CommNet.CommNetParams>().requireSignalForControl)
				return new IServoGroupInterceptor(group);

	//		return servo;
			return null; // FEHLER, das ist jetzt eben das, was später wieder zurückgedreht werden muss
		}
	
		public IServoGroupInterceptor(IServoGroup group)
			: base(group, group.Vessel)
		{
			g = group;
		}

		public IServoGroup group // FEHLER, eher temp
		{
			get { return g; }
		}

		private bool IsControllable()
		{
			return (g.Vessel.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED);
		}


	// FEHLER, hier evtl. eher eine Funktion "expand" anbieten oder sowas
		public bool Expanded
		{
			get { return g.Expanded; }
			set { g.Expanded = value; }
		}

		public string Name
		{
			get { return g.Name; }
			set { g.Name = value; }
		}

		public IList<IServo> Servos
		{
			get { return g.Servos; } // FEHLER FEHLER, hier genau was tun -> von wegen Interceptor einbauen gleich direkt hier... evtl. :-) ausser wir wollen das nicht... na mal sehen
		}

		public Vessel Vessel
		{
			get { return g.Vessel; }
		}

		////////////////////////////////////////
		// Characteristics

		// Amount of EC consumed by the servos
		public float TotalElectricChargeRequirement
		{
			get { return g.TotalElectricChargeRequirement; }
		}

		////////////////////////////////////////
		// Editor

		public void EditorMoveLeft()
		{ g.EditorMoveLeft(); }

		public void EditorMoveCenter()
		{ g.EditorMoveCenter(); }

		public void EditorMoveRight()
		{ g.EditorMoveRight(); }
	}
}
