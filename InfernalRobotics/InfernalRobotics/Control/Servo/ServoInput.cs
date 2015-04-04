using InfernalRobotics.Module;

namespace InfernalRobotics.Control.Servo
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
            set 
            { 
                rawServo.forwardKey = value.ToLower();
                rawServo.rotateKey = rawServo.translateKey = rawServo.forwardKey;
            }
        }

        public string Reverse
        {
            get { return rawServo.reverseKey; }
            set 
            { 
                rawServo.reverseKey = value.ToLower();
                rawServo.revRotateKey = rawServo.revTranslateKey = rawServo.reverseKey;
            }
        }
    }
}