using InfernalRobotics_v3.Module;
using System;

namespace InfernalRobotics_v3.Control.Servo
{
	internal class ServoMotor : IMotor
	{
		private readonly ModuleIRServo_v3 Servo;

		public ServoMotor(ModuleIRServo_v3 Servo)
		{
			this.Servo = Servo;
		}

		public float TorqueLimit
		{
			get { return Servo.TorqueLimit; }
			set { Servo.TorqueLimit = value; }
		}

		public float TargetPosition
		{
			get { return Servo.Position; }
		}

		public float Speed
		{
			get { return Servo.Speed; }
		}

		public float SpeedLimit
		{
			get { return Servo.SpeedLimit; }
			set { Servo.SpeedLimit = value; }
		}

		public float AccelerationLimit
		{
			get { return Servo.AccelerationLimit; }
			set { Servo.AccelerationLimit = value; }
		}

		public bool IsAxisInverted
		{
			get { return Servo.IsInverted; }
			set { Servo.IsInverted = value; }
		}

		public float DefaultSpeed
		{
			get { return Servo.DefaultSpeed; }
		}

		public void MoveLeft()
		{
			if(HighLogic.LoadedSceneIsEditor)
				Servo.EditorMove(float.NegativeInfinity);
			else
				Servo.MoveTo(float.NegativeInfinity, Servo.DefaultSpeed);
		}

		public void MoveCenter()
		{
			if(HighLogic.LoadedSceneIsEditor)
				Servo.EditorSetTo(Servo.DefaultPosition);
			else
				Servo.MoveTo(Servo.DefaultPosition, Servo.DefaultSpeed);
		}

		public void MoveRight()
		{
			if(HighLogic.LoadedSceneIsEditor)
				Servo.EditorMove(float.PositiveInfinity);
			else
				Servo.MoveTo(float.PositiveInfinity, Servo.DefaultSpeed);
		}

		public void Stop()
		{
			if(HighLogic.LoadedSceneIsFlight)
				Servo.Stop();
		}

		public void MoveTo(float position)
		{
			if(HighLogic.LoadedSceneIsEditor)
				Servo.EditorSetTo(position);
			else
				Servo.MoveTo(position, Servo.DefaultSpeed);
		}

		public void MoveTo(float position, float speed)
		{
			if(HighLogic.LoadedSceneIsEditor)
				Servo.EditorSetTo(position);
			else
				Servo.MoveTo(position, speed);
		}


		public string ForwardKey
		{
			get { return Servo.ForwardKey; }
			set { Servo.ForwardKey = value; }
		}

		public string ReverseKey
		{
			get { return Servo.ReverseKey; }
			set { Servo.ReverseKey = value; }
		}
	}
}