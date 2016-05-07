using InfernalRobotics.Module;
using System;

namespace InfernalRobotics.Control.Servo
{
    internal abstract class MechanismBase : IMechanism
    {
        private readonly ModuleIRServo rawServo;

        protected MechanismBase(ModuleIRServo rawServo)
        {
            this.rawServo = rawServo;
        }

        public abstract float MaxPositionLimit { get; set; }
        public abstract float MinPositionLimit { get; set; }
        public abstract float MinPosition { get; }
        public abstract float MaxPosition { get; }

        public float Position
        {
            get { return rawServo.Translator.ToExternalPos(rawServo.Position); }
        }

        /// <summary>
        /// Default position, to be used for Revert/MoveCenter
        /// Set to 0 by default to mimic previous behavior
        /// </summary>
        public float DefaultPosition
        {
            get { return RawServo.Translator.ToExternalPos(RawServo.defaultPosition); }
            set { RawServo.defaultPosition = Math.Min(Math.Max(RawServo.Translator.ToInternalPos(value), RawServo.minTweak), RawServo.maxTweak); }
        }

        protected ModuleIRServo RawServo
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


        public void Reconfigure()
        {
            rawServo.ConfigureInterpolator();
            rawServo.SetupJoints ();
        }


        public float SpringPower 
        {
            get { return RawServo.jointSpring; }
            set { RawServo.jointSpring = value; }
        }

        public float DampingPower 
        {
            get { return RawServo.jointDamping; }
            set { RawServo.jointDamping = value; }
        }

        public abstract void Reset();

        public void ApplyLimitsToSymmetry()
        {
            foreach (Part counterPart in RawServo.part.symmetryCounterparts)
            {
                var module = ((ModuleIRServo)counterPart.Modules ["ModuleIRServo"]);
                module.rotateMin = RawServo.rotateMin;
                module.rotateMax = RawServo.rotateMax;
                module.translateMin = RawServo.translateMin;
                module.translateMax = RawServo.translateMax;
                module.minTweak = RawServo.minTweak;
                module.maxTweak = RawServo.maxTweak;
            }
            Logger.Log ("ApplyingSymmetry, number of counterparts: " + RawServo.part.symmetryCounterparts.Count, Logger.Level.Debug);
        }
    }
}