using InfernalRobotics.Module;

namespace InfernalRobotics.Command
{
    public interface IServo
    {
        ILinearControl Linear { get; }
        IPresetableControl Preset { get; }
        
        // Scheduled for execution
        MuMechToggle RawServo { get; }
    }

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

    public interface ILinearControl
    {
    }

    public interface IPresetableControl
    {
    }
}
