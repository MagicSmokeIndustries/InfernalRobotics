using System;

namespace InfernalRobotics.Control
{
    internal class MechanismBase : IMechanism
    {
		public float _position = 0f;
		private float _defaultPosition = 0f;

		private bool _isMoving = false;
		private bool _isLocked = false;

        public MechanismBase()
        {
            
        }

		public float MaxPositionLimit { get { return 180f; } set {return; } }
		public float MinPositionLimit { get{ return 0f; }  set {return; } }
		public float MinPosition { get{ return 0f; }  }
		public float MaxPosition { get{ return 180f; }  }

        public float Position
        {
			get { return _position; }
        }

        /// <summary>
        /// Default position, to be used for Revert/MoveCenter
        /// Set to 0 by default to mimic previous behavior
        /// </summary>
        public float DefaultPosition
        {
			get { return _defaultPosition; }
			set { _defaultPosition = value; }
        }

        public bool IsFreeMoving
        {
			get { return false; }
        }

        public bool IsMoving
        {
			get { return _isMoving; }
        }

        public bool IsLocked
        {
			get { return _isLocked; }
            set { _isLocked = value; }
        }


        public void Reconfigure()
        {
            
        }


        public float SpringPower 
        {
			get { return 0f; }
			set { return; }
        }

        public float DampingPower 
        {
			get { return 0f; }
			set { return; }
        }

        public void Reset()
		{
			
		}

        public void ApplyLimitsToSymmetry()
        {
           
        }

		public void SetPosition(float t)
		{
			_position = t;
		}
    }
}