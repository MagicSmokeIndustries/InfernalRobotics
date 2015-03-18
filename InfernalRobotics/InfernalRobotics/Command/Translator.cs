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
        public void Init(Interpolator interpolator, float speedUnit, bool axisInversion, bool motionLock)
        {
            Interpolator = interpolator;
            SpeedUnit = speedUnit;
            IsAxisInverted = axisInversion;
            IsMotionLock = motionLock;
        }

        protected Interpolator Interpolator;

        // conversion data
        public float SpeedUnit;
        public bool IsAxisInverted;
        public bool IsMotionLock;

        // external interface
        public void Move(float pos, float speed)
        {
            if (!IsMotionLock)
                Interpolator.SetCommand(ToInternalPos(pos), speed * SpeedUnit);
            else
                Interpolator.SetCommand(0, 0);
        }

        public void MoveIncremental(float pos, float speed)
        {
            Interpolator.SetIncrementalCommand(ToInternalPos(pos), speed * SpeedUnit);
        }

        public void Stop()
        {
            Move(0, 0);
        }

        public bool IsMoving()
        {
            return Interpolator.Active && (Interpolator.CmdVelocity != 0f);
        }

        internal float GetSpeedUnit()
        {
            return SpeedUnit;
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