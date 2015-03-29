using InfernalRobotics.Control;

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
        public void Init(bool axisInversion, bool motionLock, IServo servo)
        {
            IsAxisInverted = axisInversion;
            IsMotionLock = motionLock;
            Servo = servo;
        }

        public IServo Servo;

        // conversion data
        public bool IsAxisInverted;
        public bool IsMotionLock;
        public float GetSpeedUnit()
        {
            // the speed from part.cfg is used as the default unit of speed
            return Servo.RawServo.rotateJoint ? Servo.RawServo.keyRotateSpeed : Servo.RawServo.keyTranslateSpeed;
        }

        // external interface
        /// <summary>
        /// Move the servo to the specified pos and speed.
        /// </summary>
        /// <param name="pos">Position in external coordinates</param>
        /// <param name="speed">Speed as multiplier</param>
        public void Move(float pos, float speed)
        {
            if (!Servo.RawServo.Interpolator.Active)
                Servo.RawServo.ConfigureInterpolator();

            if (!IsMotionLock)
                Servo.RawServo.Interpolator.SetCommand(ToInternalPos(pos), speed * GetSpeedUnit());
            else
                Servo.RawServo.Interpolator.SetCommand(0, 0);
        }

        public void MoveIncremental(float posDelta, float speed)
        {
            if (!Servo.RawServo.Interpolator.Active)
                Servo.RawServo.ConfigureInterpolator();

            float axisCorrection = IsAxisInverted ? -1 : 1;
            Servo.RawServo.Interpolator.SetIncrementalCommand(posDelta*axisCorrection, speed * GetSpeedUnit());
        }

        public void Stop()
        {
            Move(0, 0);
        }

        public bool IsMoving()
        {
            return Servo.RawServo.Interpolator.Active && (Servo.RawServo.Interpolator.CmdVelocity != 0f);
        }

        public float ToInternalPos(float externalPos)
        {
            if (IsAxisInverted)
                return Servo.RawServo.MinPosition + Servo.RawServo.MaxPosition - externalPos;
            return externalPos;
        }

        public float ToExternalPos(float internalPos)
        {
            if (IsAxisInverted)
                return Servo.RawServo.MinPosition + Servo.RawServo.MaxPosition - internalPos;
            return internalPos;
        }
    }
}