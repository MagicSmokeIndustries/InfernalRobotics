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
        public void Init(bool axisInversion, bool motionLock, MuMechToggle servo)
        {
            IsAxisInverted = axisInversion;
            IsMotionLock = motionLock;
            Servo = servo;
        }

        public MuMechToggle Servo;

        // conversion data
        public bool IsAxisInverted;
        public bool IsMotionLock;
        public float GetSpeedUnit()
        {
            // the speed from part.cfg is used as the default unit of speed
            return Servo.rotateJoint ? Servo.keyRotateSpeed : Servo.keyTranslateSpeed;
        }

        // external interface
        /// <summary>
        /// Move the servo to the specified pos and speed.
        /// </summary>
        /// <param name="pos">Position in external coordinates</param>
        /// <param name="speed">Speed as multiplier</param>
        public void Move(float pos, float speed)
        {
            if (!Servo.Interpolator.Active)
                Servo.ConfigureInterpolator();

            if (!IsMotionLock)
                Servo.Interpolator.SetCommand(ToInternalPos(pos), speed * GetSpeedUnit());
            else
                Servo.Interpolator.SetCommand(0, 0);
        }

        public void MoveIncremental(float posDelta, float speed)
        {
            if (!Servo.Interpolator.Active)
                Servo.ConfigureInterpolator();

            float axisCorrection = IsAxisInverted ? -1 : 1;
            Servo.Interpolator.SetIncrementalCommand(posDelta*axisCorrection, speed * GetSpeedUnit());
        }

        public void Stop()
        {
            Move(0, 0);
        }

        public bool IsMoving()
        {
            return Servo.Interpolator.Active && (Servo.Interpolator.CmdVelocity != 0f);
        }

        public float ToInternalPos(float externalPos)
        {
            if (IsAxisInverted)
                return Servo.MinPosition + Servo.MaxPosition - externalPos;
            else
                return externalPos;
        }
        public float ToExternalPos(float internalPos)
        {
            if (IsAxisInverted)
                return Servo.MinPosition + Servo.MaxPosition - internalPos;
            else
                return internalPos;
        }
    }
}