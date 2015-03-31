using System;
using InfernalRobotics.Module;
using UnityEngine;

namespace InfernalRobotics.Control.Servo
{
    internal class RotatingMechanism : IMechanism
    {
        private readonly MuMechToggle rawServo;

        public RotatingMechanism(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }
        public float Position
        {
            get { return rawServo.Position; }
        }

        public float MinPosition
        {
            get { return rawServo.MinPosition; }
        }

        public float MinPositionLimit
        {
            get { return rawServo.minTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, rawServo.rotateMin, rawServo.rotateMax);
                rawServo.minTweak = clamped;
            }
        }

        public float MaxPosition
        {
            get { return rawServo.MaxPosition; }
        }

        public float MaxPositionLimit
        {
            get { return rawServo.maxTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, rawServo.rotateMin, rawServo.rotateMax);
                rawServo.minTweak = clamped;
            }
        }

        public bool IsFreeMoving
        {
            get { return rawServo.freeMoving; }
        }

        public bool IsMoving
        {
            get { return rawServo.Translator.IsMoving(); }
        }

        public bool IsLocked
        {
            get { return rawServo.Translator.IsMotionLock; }
            set { rawServo.SetLock(value); }
        }

        public float CurrentSpeed
        {
            get { return rawServo.Translator.GetSpeedUnit(); }
        }

        public float MaxSpeed
        {
            get { return rawServo.customSpeed * rawServo.speedTweak; }
        }

        public float SpeedLimit
        {
            get { return rawServo.speedTweak; }
            set
            {
                rawServo.speedTweak = Math.Max(value, 0.01f);
            }
        }

        public float AccelerationLimit
        {
            get { return rawServo.accelTweak; }
            set
            {
                rawServo.accelTweak = Math.Max(value, 0.01f);
            }
        }

        public void MoveLeft()
        {
            if (HighLogic.LoadedSceneIsEditor)
                rawServo.MoveLeft();
            else
            {
                rawServo.Translator.Move(float.NegativeInfinity, rawServo.customSpeed * rawServo.speedTweak);
            }
        }

        public void MoveCenter()
        {
            if (HighLogic.LoadedSceneIsEditor)
                rawServo.MoveCenter();
            else
            {
                rawServo.Translator.Move(rawServo.Translator.ToExternalPos(0f), rawServo.customSpeed * rawServo.speedTweak);
            }
        }

        public void MoveRight()
        {
            if (HighLogic.LoadedSceneIsEditor)
                rawServo.MoveRight();
            else
            {
                rawServo.Translator.Move(float.PositiveInfinity, rawServo.customSpeed * rawServo.speedTweak);
            }
        }

        public void Stop()
        {
            if (HighLogic.LoadedSceneIsFlight)
                rawServo.Translator.Stop();
        }

        public void MoveTo(float position)
        {
            rawServo.Translator.Move(position, rawServo.customSpeed * rawServo.speedTweak);
        }

        public void MoveTo(float position, float speed)
        {
            rawServo.Translator.Move(position, speed);
        }
    }
}