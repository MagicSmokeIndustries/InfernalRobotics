using InfernalRobotics.Module;

namespace InfernalRobotics.Servo
{
    internal class ServoInput : IServoInput
    {
        private readonly MuMechToggle rawServo;

        public ServoInput(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }

        public string Forward
        {
            get { return rawServo.forwardKey; }
            set { rawServo.forwardKey = value; }
        }

        public string Reverse
        {
            get { return rawServo.reverseKey; }
            set { rawServo.reverseKey = value; }
        }
    }
}