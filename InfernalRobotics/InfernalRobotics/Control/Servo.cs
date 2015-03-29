using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    class Servo : IServo
    {

        private readonly MuMechToggle rawServo;
        private readonly IPresetable preset;
        private readonly IMechanism mechanism;
        private readonly IServoGroup servoGroup;
        private IServoInput input;

        public Servo(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
            preset = new ServoPreset(rawServo);
            servoGroup = new ServoGroup(rawServo);
            input = new ServoInput(rawServo);

            if (rawServo.rotateJoint)
            {
                mechanism = new RotatingMechanism(rawServo);
            }
            else
            {
                mechanism = new TranslateMechanism(rawServo);
            }
        }

        public string Name
        {
            get { return rawServo.servoName; }
            set { rawServo.servoName = value; }
        }

        public IMechanism Mechanism
        {
            get { return mechanism; }
        }

        public IPresetable Preset
        {
            get { return preset; }
        }

        public IServoGroup Group
        {
            get { return servoGroup; }
        }

        public IServoInput Input
        {
            get { return input; }
            private set { input = value; }
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

        protected bool Equals(Servo other)
        {
            return Equals(rawServo, other.rawServo);
        }
    }
}