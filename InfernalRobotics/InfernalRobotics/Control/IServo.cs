using System.Collections.Generic;
using InfernalRobotics_v3.Module;
using UnityEngine;

namespace InfernalRobotics_v3.Control
{
	public interface IServo
	{
		////////////////////////////////////////
		// Properties

		// Servo's name
		string Name { get; set; }

		// Servo's unique identifier
		uint UID { get; }

		// Part object that hosts the Servo
		Part HostPart { get; }

		// Settable only, Highlight the servo part on the vessel
		bool Highlight { set; }


// FEHLER FEHLER mechanik oder halt characteristics

		// Implementation of servo's motor
		IMotor Motor { get; }

		// Implementation of presets
		IPresetable Presets { get; }


		// Servo's Group related implementation
		string GroupName { get; set; }
// FEHLER FEHLER evtl. die gruppe holen hier?

		////////////////////////////////////////
		// Status

		// Gets the current position.
		float Position { get; }

		float Speed { get; }

		// Returns true if servo is currently moving
		bool IsMoving { get; }

		// Returns/set locked state of the servo. Locked servos do not move until unlocked.
		bool IsLocked { get; set; }

		////////////////////////////////////////
		// Settings

		bool IsInverted { get; set; }

		List<float> PresetPositions { get; set; }

		// Returns default or 'zero' position of the part
		float DefaultPosition { get; set; }

		bool IsLimitted { get; }
		void ToggleLimits();

		// Returns/sets current tweaked MinPosition value
		float MinPositionLimit { get; set; }

		// Returns/sets current tweaked MaxPosition value
		float MaxPositionLimit { get; set; }


		float TorqueLimit { get; set; }

		float DefaultSpeed { get; set; }
		float SpeedLimit { get; set; }
		float GroupSpeedFactor { get; set; }

		float AccelerationLimit { get; set; }

		// Gets or sets the spring power.
		float SpringPower { get; set; }

		// Gets or sets the damping power for spring. Usen in conjuction with SpringPower to create suspension effect.
		float DampingPower { get; set; }

		////////////////////////////////////////
		// Characteristics

		bool IsRotational { get; }

		// Returns rotateMin or translateMin
		float MinPosition { get; }

		// Returns rotateMax or translateMax
		float MaxPosition { get; }

		// Returns true is the servo is an uncontrolled part (for example washer)
		bool IsFreeMoving { get; }

		bool CanHaveLimits { get; }

		float MaxTorque { get; }

		float MaxSpeed { get; }

		float MaxAcceleration { get; }

		// Amount of EC consumed by the servo
		float ElectricChargeRequired { get; }

// FEHLER, ab hier... evtl. gehören einige ins IMotor, einige ins IPresetable
		////////////////////////////////////////
		// Input

		void MoveTo(float position, float speed);

		void Stop();

		////////////////////////////////////////
		// Editor

		void EditorReset();

		void EditorMove(float position);
		void EditorSetTo(float position);

		void CopyPresetsToSymmetry();
		void CopyLimitsToSymmetry();

		void DoTransformStuff(Transform trf);

	}
}

