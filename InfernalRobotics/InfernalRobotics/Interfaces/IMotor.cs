using System;


namespace InfernalRobotics_v3.Interfaces
{
	public interface IMotor
	{
		// Gets or sets the max torque of the servo motor
		float TorqueLimit { get; set;}

		// Returns/sets servo's Acceleration multiplier
		float AccelerationLimit { get; set; }

		// Returns/sets servo's inverted status
		bool IsAxisInverted { get; set; }

		// the speed from part.cfg is used as the default unit of speed
		float DefaultSpeed { get; }

		// The current rate of travel, like right now
		float Speed { get; }

		// User setting to limit the speed of the servo
		float SpeedLimit { get; set; }

		// Gets the target position.
		// TargetPosition should be set with Move commands.
		float TargetPosition { get; }

		// Commands the servo to move in the direction that decreases its Position
		void MoveLeft();

		// Comands the servo to move towards its DefaultPosition
		void MoveCenter();

		// Commands the servo to move in the direction that increases its Position
		void MoveRight();

		// Commands the servo to stop
		void Stop();

		// Commands the servo to move to specified position at current speed
		void MoveTo(float position);

		// Commands the servo to move to specified position at specified speed multiplier
		void MoveTo(float position, float speed);


		// Keybinding for servo's MoveForward key
		string ForwardKey { get; set; }

		// Keybinding for servo's MoveBackward key
		string ReverseKey { get; set; }
	}
}

