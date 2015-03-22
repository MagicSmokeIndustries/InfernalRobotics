using System;
using InfernalRobotics.Module;

namespace InfernalRobotics.Command
{
    /*
     * <summary>
     * This class acts as a translator for UI - it implement basic commands for servos
     * and translates them to internal routines for servo controller.
     *
     * Thus later we can add more sophisticated controller commands, or change their behavior
     * per part basis without breaking the way UI works.
     *
     * More basic commands may be added too. Future API calls will most likely be hooked to this class.
     *
     */

    public class Translator
    {
        public void Init(Interpolator interpolator, bool axisInversion, bool motionLock, MuMechToggle servo)
        {
            Interpolator = interpolator;
            IsAxisInverted = axisInversion;
            IsMotionLock = motionLock;
            Servo = servo;
        }

        public MuMechToggle Servo;
        protected Interpolator Interpolator;

        // conversion data
        public bool IsAxisInverted;
        public bool IsMotionLock;
        public float GetSpeedUnit()
        {
            // the speed from part.cfg is used as the default unit of speed
            return Servo.rotateJoint ? Servo.keyRotateSpeed : Servo.keyTranslateSpeed;
        }

        // external interface
        public void Move(float pos, float speed)
        {
            if (!Interpolator.Active)
                Servo.ConfigureInterpolator();

            if (!IsMotionLock)
                Interpolator.SetCommand(ToInternalPos(pos), speed * GetSpeedUnit());
            else
                Interpolator.SetCommand(0, 0);
        }

        public void MoveIncremental(float posDelta, float speed)
        {
            if (!Interpolator.Active)
                Servo.ConfigureInterpolator();

            float axisCorrection = IsAxisInverted ? -1 : 1;
            Interpolator.SetIncrementalCommand(posDelta*axisCorrection, speed * GetSpeedUnit());
        }

        public void Stop()
        {
            Move(0, 0);
        }

        public bool IsMoving()
        {
            return Interpolator.Active && (Interpolator.CmdVelocity != 0f);
        }

        public float ToInternalPos(float externalPos)
        {
            if (IsAxisInverted)
                return Interpolator.MinPosition + Interpolator.MaxPosition - externalPos;
            else
                return externalPos;
        }
        public float ToExternalPos(float internalPos)
        {
            if (IsAxisInverted)
                return Interpolator.MinPosition + Interpolator.MaxPosition - internalPos;
            else
                return internalPos;
        }
    }
}