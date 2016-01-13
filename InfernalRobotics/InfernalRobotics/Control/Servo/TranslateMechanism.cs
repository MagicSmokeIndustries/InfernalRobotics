using InfernalRobotics.Module;
using UnityEngine;

namespace InfernalRobotics.Control.Servo
{
    internal class TranslateMechanism : MechanismBase
    {
        public TranslateMechanism(ModuleIRServo rawServo)
            : base(rawServo)
        {
        }

        public override float MinPositionLimit
        {
            get { return RawServo.minTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, RawServo.translateMin, RawServo.translateMax);
                RawServo.minTweak = clamped;
            }
        }

        public override float MaxPositionLimit
        {
            get { return RawServo.maxTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, RawServo.translateMin, RawServo.translateMax);
                RawServo.maxTweak = clamped;
            }
        }

        public override float MinPosition
        {
            get { return RawServo.translateMin; }
        }

        public override float MaxPosition
        {
            get { return RawServo.translateMax; }
        }
            
        public override void Reset()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                RawServo.FixedMeshTransform.position = RawServo.part.transform.position;
                RawServo.translation = 0;
            }
        }
    }
}