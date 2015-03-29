using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    public interface IServoGroup    
    {
        string Name { get; set; }
        float ElectricChargeRequired { get; }
    }

    internal class ServoGroup : IServoGroup
    {
        private readonly MuMechToggle rawServo;

        public ServoGroup(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }

        public string Name
        {
            get { return rawServo.groupName; }
            set { rawServo.groupName = value; }
        }

        public float ElectricChargeRequired
        {
            get { return rawServo.GroupElectricChargeRequired; }
        }
    }
}