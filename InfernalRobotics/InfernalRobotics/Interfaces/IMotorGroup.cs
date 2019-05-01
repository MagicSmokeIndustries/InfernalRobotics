using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfernalRobotics_v3.Interfaces
{
	public interface IMotorGroup
	{
// FEHLER, weiss nicht genau was das ist... klären
		bool MovingNegative { get; set; }

		bool MovingPositive { get; set; }

		float GroupSpeedFactor { get; set; }

		// Commands the servo to move in the direction that decreases its Position
		void MoveLeft();

		// Comands the servo to move towards its DefaultPosition
		void MoveCenter();

		// Commands the servo to move in the direction that increases its Position
		void MoveRight();

// FEHLER, hab ich hier, nicht aber im IMotor... wieso das? -> dort ist es im IPresetable... -> angleichen
		void MoveNextPreset();

		void MovePrevPreset();

		// Commands the servos to stop
		void Stop();

		// Keybinding for servo group's MoveForward key
		string ForwardKey { get; set; }

		// Keybinding for servo group's MoveBackward key
		string ReverseKey { get; set; }
	}
}
