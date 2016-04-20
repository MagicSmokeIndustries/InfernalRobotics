using InfernalRobotics.Module;

namespace InfernalRobotics.Control.Servo
{
    class Servo : IServo
    {

        private readonly ModuleIRServo rawServo;
        private readonly IPresetable preset;
        private readonly IMechanism mechanism;
        private readonly IServoMotor motor;
        private readonly IControlGroup controlGroup;
        private readonly IServoInput input;

        public Servo(ModuleIRServo rawServo)
        {
            this.rawServo = rawServo;
            controlGroup = new ControlGroup(rawServo);
            input = new ServoInput(rawServo);

            if (rawServo.rotateJoint)
            {
                mechanism = new RotatingMechanism(rawServo);
            }
            else
            {
                mechanism = new TranslateMechanism(rawServo);
            }
            motor = new ServoMotor (rawServo);

            preset = new ServoPreset(rawServo, this);
        }

        public string Name
        {
            get { return rawServo.servoName; }
            set { rawServo.servoName = value; }
        }
        public uint UID
        {
            get {return rawServo.part.craftID; }
        }

        public Part HostPart
        {
            get { return rawServo.part; }
        }

        public bool Highlight
        {
            set { rawServo.part.SetHighlight(value, false); }
        }

        public float ElectricChargeRequired
        {
            get { return rawServo.electricChargeRequired; }
            set { rawServo.electricChargeRequired = value; }
        }

        public IMechanism Mechanism
        {
            get { return mechanism; }
        }

        public IServoMotor Motor
        {
            get { return motor; }
        }

        public IPresetable Preset
        {
            get { return preset; }
        }

        public IControlGroup Group
        {
            get { return controlGroup; }
        }

        public IServoInput Input
        {
            get { return input; }
        }

        public ModuleIRServo RawServo
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