using InfernalRobotics.Module;

namespace InfernalRobotics.Control.Servo
{
    internal class ControlGroup : IControlGroup
    {
        private readonly MuMechToggle rawServo;

        public ControlGroup(MuMechToggle rawServo)
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
            set { rawServo.GroupElectricChargeRequired = value; }
        }
    }
}