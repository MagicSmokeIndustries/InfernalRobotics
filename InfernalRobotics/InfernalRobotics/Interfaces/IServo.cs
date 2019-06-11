using System.Collections.Generic;
using UnityEngine;


namespace InfernalRobotics_v3.Interfaces
{
	// FEHLER, nicht kompatibel mit dem rotor/control Modus -> ah und evtl. einige Dinge noch je nach Modus absperren...

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

		// Adds a preset at "position" to the list of presets.
		void AddPresetPosition(float position);

		// Removes preset at index presetIndex from the list of preset positions
		void RemovePresetPositionsAt(int presetIndex);

		// Preset sorter to implement sorting of the list of preset positions
		void SortPresetPositions(IComparer<float> sorter = null);

		// Returns default or 'zero' position of the part
		float DefaultPosition { get; set; }

		bool IsLimitted { get; set; }
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

		float MaxForce { get; }

		float MaxSpeed { get; }

		float MaxAcceleration { get; }

		// Amount of EC consumed by the servo
		float ElectricChargeRequired { get; }

		////////////////////////////////////////
		// Editor

		void EditorReset();

		void DoTransformStuff(Transform trf);

	}
}

