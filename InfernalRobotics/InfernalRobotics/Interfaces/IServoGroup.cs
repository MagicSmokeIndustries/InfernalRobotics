using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfernalRobotics_v3.Interfaces
{
	public interface IServoGroup
	{
		IServoGroup group { get; }

		////////////////////////////////////////
		// Properties

		// ServoGroup's name
		string Name { get; set; }

		IList<IServo> Servos { get; }

		Vessel Vessel { get; }

		////////////////////////////////////////
		// Status

		// Gets the last commanded direction
		int MovingDirection { get; }

		////////////////////////////////////////
		// Settings

		bool Expanded { get; set; }

		bool AdvancedMode { get; set; }

		float GroupSpeedFactor { get; set; }

		// Keybinding for servo group's MoveBackward key
		string ReverseKey { get; set; }

		// Keybinding for servo group's MoveForward key
		string ForwardKey { get; set; }

		////////////////////////////////////////
		// Characteristics

		// Amount of EC consumed by the servos
		float TotalElectricChargeRequirement { get; }

		////////////////////////////////////////
		// Input

		// Commands the servos to move in the direction that decreases its position
		void MoveLeft();

		// Comands the servos to move towards its DefaultPosition
		void MoveCenter();

		// Commands the servos to move in the direction that increases its position
		void MoveRight();

		// Orders the servos to move to the previous preset position
		void MovePrevPreset();

		// Orders the servos to move to the next preset position
		void MoveNextPreset();

		// Commands the servos to stop
		void Stop();

		////////////////////////////////////////
		// Editor

		void EditorMoveLeft();
		void EditorMoveCenter();
		void EditorMoveRight();

		void EditorMovePrevPreset();
		void EditorMoveNextPreset();

		////////////////////////////////////////
		// BuildAid

		bool BuildAid { get; set; }
	}
}
