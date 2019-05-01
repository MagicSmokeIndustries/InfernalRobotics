using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfernalRobotics_v3.Interfaces
{
	public interface IServoGroup : IMotorGroup
	{
		IServoGroup group { get; }

		bool Expanded { get; set; }

		string Name { get; set; }

		IList<IServo> Servos { get; }

		Vessel Vessel { get; }

		////////////////////////////////////////
		// Characteristics

		// Amount of EC consumed by the servos
		float TotalElectricChargeRequirement { get; }

		////////////////////////////////////////
		// Editor

		void EditorMoveLeft();
		void EditorMoveCenter();
		void EditorMoveRight();
	}
}
