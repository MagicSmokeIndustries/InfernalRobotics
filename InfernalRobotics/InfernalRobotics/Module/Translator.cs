using System;
using InfernalRobotics.Module;

namespace InfernalRobotics
{
    /* 
     * <summary>
     * This class acts as a translator for UI - it implement basic commands for servos 
     * and translates them to internal routines for servo controller.
     * 
     * Thus later we can add more sophsticated controller commands, or change their behaviour 
     * per part basis without breaking the way UI works.
     * 
     * More basic commands may be added too. Future API calls will most likely be hooked to this class.
     * 
     */
    public class Translator
    {
        public Translator()
        {
        }

        public void Init(Interpolator interpolator, float speedUnit)
        {
            Interpolator = interpolator;
            SpeedUnit = speedUnit;
        }

        protected Interpolator Interpolator;
        protected float SpeedUnit;

        // external interface
        public void Move(float pos, float speed)
        {
            Interpolator.SetCommand(pos, speed * SpeedUnit);
        }
        public void MoveIncremental(float pos, float speed)
        {
            Interpolator.SetIncrementalCommand(pos, speed * SpeedUnit);
        }
        public void Stop()
        {
            Move(0, 0);
        }
        public bool IsMoving()
        {
            return Interpolator.Active && Interpolator.CmdVelocity != 0;
        }
        internal float getSpeedUnit()
        {
            return SpeedUnit;
        }
    }
}

