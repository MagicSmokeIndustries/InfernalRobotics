using System.Collections.Generic;
using UnityEngine;

using InfernalRobotics_v3.Module;

namespace InfernalRobotics_v3.Interfaces
{
	public enum ModeType { servo = 1, rotor = 2 };

	public enum InputModeType { manual = 1, control = 2, linked = 3, tracking = 4 };

	public interface IServo
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
		// Status

		// Gets the target position (the final position the motor want's to move to)
		// TargetPosition should be set with Move commands
		float TargetPosition { get; }

		// Gets the target speed (the final speed the motor want's to accelerate to)
		// TargetSpeed should be set with Move commands
		float TargetSpeed { get; }

		// Gets the commanded position (the position the motor want's to be in)
		// CommandedPosition should be set with Move commands
		float CommandedPosition { get; }

		// Gets the commanded speed (the speed the motor want's to move with)
		// CommandedSpeed should be set with Move commands
		float CommandedSpeed { get; }

		// The current position (the real position)
		// This value is influenced by forces applyed to the joint.
		float Position { get; }

		// Returns true if servo is currently moving
		bool IsMoving { get; }

		////////////////////////////////////////
		// Settings

		ModeType Mode { get; set; }

		InputModeType InputMode { get; set; }

		// Returns/set locked state of the servo. Locked servos do not move until unlocked.
		bool IsLocked { get; set; }

		////////////////////////////////////////
		// Settings (servo)

		// Returns/sets servo's inverted status
		bool IsInverted { get; set; }

		List<float> PresetPositions { get; set; }

		// Adds a preset at "position" to the list of presets.
		void AddPresetPosition(float position);

		// Removes preset at index presetIndex from the list of preset positions
		void RemovePresetPositionsAt(int presetIndex);

		// Preset sorter to implement sorting of the list of preset positions
		void SortPresetPositions(IComparer<float> sorter = null);

		// Returns default or 'zero' position of the part
		float DefaultPosition { get; set; }

		// Gets or sets the max force of the servo motor
		float ForceLimit { get; set; }

		// Returns/sets servo's Acceleration multiplier
		float AccelerationLimit { get; set; }

		// the speed from part.cfg is used as the default unit of speed
		float DefaultSpeed { get; set; }

		// User setting to limit the speed of the servo
		float SpeedLimit { get; set; }

		// Gets or sets the spring power.
		float SpringPower { get; set; }

		// Gets or sets the damping power for spring. Usen in conjuction with SpringPower to create suspension effect.
		float DampingPower { get; set; }

		bool IsLimitted { get; set; }
		void ToggleLimits();

		// Returns/sets current tweaked MinPosition value
		float MinPositionLimit { get; set; }

		// Returns/sets current tweaked MaxPosition value
		float MaxPositionLimit { get; set; }

		////////////////////////////////////////
		// Settings (servo - control input)

		float ControlDeflectionRange { get; set; }

		float ControlNeutralPosition { get; set; }

		float PitchControl { get; set; }
		float RollControl { get; set; }
		float YawControl { get; set; }

		float ThrottleControl { get; set; }

		float XControl { get; set; }
		float YControl { get; set; }
		float ZControl { get; set; }

		////////////////////////////////////////
		// Settings (servo - link input)

		void LinkInput();

		////////////////////////////////////////
		// Settings (servo - track input)

		bool TrackSun { get; set; }

		float TrackAngle { get; set; }

		////////////////////////////////////////
		// Settings (rotor)

		float RotorAcceleration { get; set; }

		float BaseSpeed { get; set; }

		float PitchSpeed { get; set; }
		float RollSpeed { get; set; }
		float YawSpeed { get; set; }

		float ThrottleSpeed { get; set; }

		float XSpeed { get; set; }
		float YSpeed { get; set; }
		float ZSpeed { get; set; }

		////////////////////////////////////////
		// Characteristics

		bool IsRotational { get; }

		// Returns rotateMin or translateMin
		float MinPosition { get; }

		// Returns rotateMax or translateMax
		float MaxPosition { get; }

		// Returns true is the servo is an uncontrolled part (for example washer)
		bool IsFreeMoving { get; }

		// Returns true if the part is in mode 'servo'
		bool IsServo { get; }

		bool CanHaveLimits { get; }

		float MaxForce { get; }

		float MaxAcceleration { get; }

		float MaxSpeed { get; }

		// Amount of EC consumed by the servo
		float ElectricChargeRequired { get; }

		bool HasSpring { get; }

		////////////////////////////////////////
		// Input (servo)

		// Commands the servo to move in the direction that decreases its Position
		void MoveLeft(float targetSpeed);

		// Comands the servo to move towards its DefaultPosition
		void MoveCenter(float targetSpeed);

		// Commands the servo to move in the direction that increases its Position
		void MoveRight(float targetSpeed);

		void Move(float deltaPosition, float targetSpeed);

		void PrecisionMove(float targetPosition, float targetSpeed, float accelerationLimit);

		// Commands the servo to move to specified position at current speed
		void MoveTo(float targetPosition);

		// Commands the servo to move to specified position at specified speed multiplier
		void MoveTo(float targetPosition, float targetSpeed);

		// Commands the servo to stop
		void Stop();

		void SetRelaxMode(float relaxFactor);

		void ResetRelaxMode();

		bool RelaxStep();

		////////////////////////////////////////
		// Input (rotor)

		bool IsRunning { get; set; }

		////////////////////////////////////////
		// Editor

		void EditorReset();

		void EditorMoveLeft(float targetSpeed);
		void EditorMoveCenter(float targetSpeed);
		void EditorMoveRight(float targetSpeed);

		void EditorMove(float targetPosition, float targetSpeed);
		void EditorSetTo(float targetPosition);

		void DoTransformStuff(Transform trf);

	}
}

