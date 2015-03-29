using System;
using InfernalRobotics.Module;
using UnityEngine;

namespace InfernalRobotics.Control
{
    internal class RotatingMechanism : IMechanism
    {
        private readonly MuMechToggle rawServo;
        private bool jjjjjjj;

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
            rawServo.MoveLeft();
        }

        public void MoveCenter()
        {
            //TODO: to be precise this should be not Zero but a default rotation/translation as set in VAB/SPH
            rawServo.Translator.Move(rawServo.Translator.ToExternalPos(0f), rawServo.customSpeed * rawServo.speedTweak); 
        }

        public void MoveRight()
        {
            rawServo.MoveRight();
        }

        public void Stop()
        {
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