using System.Collections.Generic;
using UnityEngine;


namespace InfernalRobotics_v3.Interfaces
{
	public interface IServo : IMotor
	{
		IServo servo { get; }

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

		// Implementation of presets
		IPresetable Presets { get; }

		// Servo's Group related implementation
		string GroupName { get; set; }

		////////////////////////////////////////
		// Settings

		List<float> PresetPositions { get; set; }

		// Returns default or 'zero' position of the part
		float DefaultPosition { get; set; }

		bool IsLimitted { get; }
		void ToggleLimits();

		// Returns/sets current tweaked MinPosition value
		float MinPositionLimit { get; set; }

		// Returns/sets current tweaked MaxPosition value
		float MaxPositionLimit { get; set; }

		// Gets or sets the spring power.
		float SpringPower { get; set; }

		// Gets or sets the damping power for spring. Usen in conjuction with SpringPower to create suspension effect.
		float DampingPower { get; set; }


		float GroupSpeedFactor { get; set; }

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

		////////////////////////////////////////
		// Editor

		void EditorReset();

		void CopyPresetsToSymmetry();
		void CopyLimitsToSymmetry();

		void DoTransformStuff(Transform trf);

	}
}

