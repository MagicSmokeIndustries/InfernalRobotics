using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    class Servo : IServo
    {
        protected bool Equals(Servo other)
        {
            return Equals(rawServo, other.rawServo);
        }

        public override int GetHashCode()
        {
            return (rawServo != null ? rawServo.GetHashCode() : 0);
        }

        public static bool operator ==(Servo left, Servo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Servo left, Servo right)
        {
            return !Equals(left, right);
        }

        private readonly MuMechToggle rawServo;

        public Servo(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }

        public string Name
        {
            get { return rawServo.servoName; }
            set { rawServo.servoName = value; }
        }

        public ILinearControl Linear
        {
            get { return rawServo; }
        }

        public IPresetableControl Preset
        {
            get { return rawServo; }
        }

        public MuMechToggle RawServo
        {
            get { return rawServo; }
        }

        public override bool Equals(object o)
        {
            var servo = o as Servo;
            return servo != null && rawServo.Equals(servo.RawServo);
        }
    }
}