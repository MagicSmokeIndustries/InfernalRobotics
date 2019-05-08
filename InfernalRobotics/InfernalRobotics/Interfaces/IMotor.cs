using System;


namespace InfernalRobotics_v3.Interfaces
{
	public interface IMotor
	{
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

		// Returns/set locked state of the servo. Locked servos do not move until unlocked.
		bool IsLocked { get; set; }

		////////////////////////////////////////
		// Settings

		// Returns/sets servo's inverted status
		bool IsInverted { get; set; }

		// Gets or sets the max force of the servo motor
		float ForceLimit { get; set;}

		// Returns/sets servo's Acceleration multiplier
		float AccelerationLimit { get; set; }

		// User setting to limit the speed of the servo
		float SpeedLimit { get; set; }

		// the speed from part.cfg is used as the default unit of speed
		float DefaultSpeed { get; set; } // FEHLER, das wird nicht korrekt genutzt im Moment...

		////////////////////////////////////////
		// Input

		// Commands the servo to move in the direction that decreases its Position
		void MoveLeft();

		// Comands the servo to move towards its DefaultPosition
		void MoveCenter();

		// Commands the servo to move in the direction that increases its Position
		void MoveRight();

		// Commands the servo to move to specified position at current speed
		void MoveTo(float position);

		// Commands the servo to move to specified position at specified speed multiplier
		void MoveTo(float position, float speed);

		// Commands the servo to stop
		void Stop();

		// Keybinding for servo's MoveForward key
		string ForwardKey { get; set; }

		// Keybinding for servo's MoveBackward key
		string ReverseKey { get; set; }

		////////////////////////////////////////
		// Editor

		void EditorMoveLeft();
		void EditorMoveCenter();
		void EditorMoveRight();

		void EditorMove(float position);
		void EditorSetTo(float position);
	}
}

