using System;
using InfernalRobotics.Module;

namespace InfernalRobotics.Control.Servo
{
    internal abstract class MechanismBase : IMechanism
    {
        private readonly MuMechToggle rawServo;

        protected MechanismBase(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }

        public abstract float MaxPositionLimit { get; set; }
        public abstract float MinPositionLimit { get; set; }

        public float MaxPosition
        {
            get { return RawServo.MaxPosition; }
        }


        public float MinPosition
        {
            get { return rawServo.MinPosition; }
        }


        public float Position
        {
            get { return rawServo.Translator.ToExternalPos(rawServo.Position); }
        }

        protected MuMechToggle RawServo
        {
            get { return rawServo; }
        }

        public bool IsFreeMoving
        {
            get { return RawServo.freeMoving; }
        }

        public bool IsMoving
        {
            get { return RawServo.Translator.IsMoving(); }
        }

        public bool IsLocked
        {
            get { return RawServo.Translator.IsMotionLock; }
            set { RawServo.SetLock(value); }
        }

        public float CurrentSpeed
        {
            get { return RawServo.Translator.GetSpeedUnit(); }
        }

        public float MaxSpeed
        {
            get { return RawServo.customSpeed * RawServo.speedTweak; }
        }

        public float SpeedLimit
        {
            get { return RawServo.speedTweak; }
            set
            {
                RawServo.speedTweak = Math.Max(value, 0.01f);
            }
        }

        public float AccelerationLimit
        {
            get { return RawServo.accelTweak; }
            set
            {
                RawServo.accelTweak = Math.Max(value, 0.01f);
            }
        }

        public bool IsAxisInverted
        {
            get { return rawServo.invertAxis; }
            set { rawServo.invertAxis = value; }
        }

        public abstract float DefaultSpeed { get; }

        public void MoveLeft()
        {
            if (HighLogic.LoadedSceneIsEditor)
                RawServo.MoveLeft();
            else
            {
                RawServo.Translator.Move(float.NegativeInfinity, RawServo.customSpeed * RawServo.speedTweak);
            }
        }

        public void MoveCenter()
        {
            if (HighLogic.LoadedSceneIsEditor)
                RawServo.MoveCenter();
            else
            {
                RawServo.Translator.Move(RawServo.Translator.ToExternalPos(0f), RawServo.customSpeed * RawServo.speedTweak);
            }
        }

        public void MoveRight()
        {
            if (HighLogic.LoadedSceneIsEditor)
                RawServo.MoveRight();
            else
            {
                RawServo.Translator.Move(float.PositiveInfinity, RawServo.customSpeed * RawServo.speedTweak);
            }
        }

        public void Stop()
        {
            if (HighLogic.LoadedSceneIsFlight)
                RawServo.Translator.Stop();
        }

        public void MoveTo(float position)
        {
            RawServo.Translator.Move(position, RawServo.customSpeed * RawServo.speedTweak);
        }

        public void MoveTo(float position, float speed)
        {
            RawServo.Translator.Move(position, speed);
        }

        public void Reconfigure()
        {
            rawServo.ConfigureInterpolator();
        }

        public abstract void Reset();
    }
}