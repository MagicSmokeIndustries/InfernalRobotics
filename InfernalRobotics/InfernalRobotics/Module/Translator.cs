using System;
using InfernalRobotics.Module;

namespace InfernalRobotics
{
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

        internal float getSpeedUnit()
        {
            return SpeedUnit;
        }
    }
}

