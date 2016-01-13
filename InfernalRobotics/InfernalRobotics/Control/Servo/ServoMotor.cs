using InfernalRobotics.Module;
using System;

namespace InfernalRobotics.Control.Servo
{
    internal class ServoMotor : IServoMotor
    {
        private readonly ModuleIRServo rawServo;

        public ServoMotor(ModuleIRServo rawServo)
        {
            this.rawServo = rawServo;
        }

        public float MaxTorque
        {
            get { return RawServo.torqueTweak; }
            set
            {
                RawServo.torqueTweak = Math.Max(value, 0.00f);
            }
        }

        //Change the implementation of Position as soon as you figure out for to get real position.
        public float TargetPosition
        {
            get { return rawServo.Translator.ToExternalPos(rawServo.Position); }
        }

        protected ModuleIRServo RawServo
        {
            get { return rawServo; }
        }

        public float CurrentSpeed
        {
            get { return RawServo.Interpolator.Velocity; }
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
            set
            {
                rawServo.invertAxis = value;
                rawServo.Events["InvertAxisToggle"].guiName = rawServo.invertAxis ? "Un-invert Axis" : "Invert Axis";
            }
        }

        //TODO: change this implementation
        public float DefaultSpeed
        {
            get { return RawServo.rotateJoint ? RawServo.keyRotateSpeed : RawServo.keyTranslateSpeed; }
        }


        public void MoveLeft()
        {
            if (HighLogic.LoadedSceneIsEditor)
                RawServo.EditorMoveLeft();
            else
            {
                RawServo.Translator.Move(float.NegativeInfinity, RawServo.customSpeed * RawServo.speedTweak);
            }
        }

        public void MoveCenter()
        {
            if (HighLogic.LoadedSceneIsEditor)
                RawServo.EditorMoveCenter();
            else
            {
                RawServo.Translator.Move(RawServo.Translator.ToExternalPos(RawServo.defaultPosition), RawServo.customSpeed * RawServo.speedTweak);
            }
        }

        public void MoveRight()
        {
            if (HighLogic.LoadedSceneIsEditor)
                RawServo.EditorMoveRight();
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
            if (HighLogic.LoadedSceneIsEditor)
            {
                var deltaPosition = rawServo.Translator.ToInternalPos(position) - (rawServo.Position);
                rawServo.EditorApplyDeltaPos(deltaPosition);
            }
            else
            {
                RawServo.Translator.Move(position, RawServo.customSpeed * RawServo.speedTweak);
            }
        }

        public void MoveTo(float position, float speed)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                var deltaPosition = rawServo.Translator.ToInternalPos(position) - (rawServo.Position);
                rawServo.EditorApplyDeltaPos(deltaPosition);
            }
            else
            {
                RawServo.Translator.Move(position, speed);
            }
        }

    }
}