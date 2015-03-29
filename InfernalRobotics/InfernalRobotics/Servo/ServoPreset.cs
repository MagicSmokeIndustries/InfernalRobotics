using InfernalRobotics.Module;

namespace InfernalRobotics.Servo
{
    internal class ServoPreset : IPresetable
    {
        private readonly MuMechToggle rawServo;

        public ServoPreset(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }

        public void MovePrev()
        {
            rawServo.MovePrevPreset();
        }

        public void MoveNext()
        {
            rawServo.MoveNextPreset();
        }
    }
}