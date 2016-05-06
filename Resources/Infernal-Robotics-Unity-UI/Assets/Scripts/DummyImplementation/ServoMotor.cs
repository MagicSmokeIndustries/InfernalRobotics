
using System;

namespace InfernalRobotics.Control
{
    internal class ServoMotor : IServoMotor
    {
        
		private float _speedLimit = 1f;
		private float _accLimit = 4f;
		private bool _axisInverted = false;

		public ServoMotor()
        {
           
        }

        public float MaxTorque
        {
            get { return 0f; }
            set
            {
				return;
            }
        }

        //Change the implementation of Position as soon as you figure out for to get real position.
        public float TargetPosition
        {
            get { return 0f; }
        }

        public float CurrentSpeed
        {
			get { return 1f; }
        }

        public float MaxSpeed
        {
			get { return 2f; }
        }

        public float SpeedLimit
        {
			get { return _speedLimit; }
            set
            {
				_speedLimit = Math.Max(value, 0.01f);
            }
        }

        public float AccelerationLimit
        {
			get { return _accLimit; }
            set
            {
				_accLimit = Math.Max(value, 0.01f);
            }
        }

        public bool IsAxisInverted
        {
			get { return _axisInverted; }
            set
            {
				_axisInverted = value;
            }
        }

        public float DefaultSpeed
        {
            get { return 0.25f; }
        }


        public void MoveLeft()
        {
            //move left
        }

        public void MoveCenter()
        {
            //move center
        }

        public void MoveRight()
        {
			//move right
        }

        public void Stop()
        {
			//stop
        }

        public void MoveTo(float position)
        {
           
        }

        public void MoveTo(float position, float speed)
        {
            
        }

    }
}