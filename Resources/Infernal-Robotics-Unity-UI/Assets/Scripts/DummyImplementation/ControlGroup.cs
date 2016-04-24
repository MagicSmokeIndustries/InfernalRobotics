

namespace InfernalRobotics.Control
{
    internal class ControlGroup : IControlGroup
    {
		private string _name = "New Dummy Servo";
		private float _ECRequired = 1f;

        public ControlGroup()
        {
            
        }

        public string Name
        {
            get { return _name; }
			set { _name = value; }
        }

        public float ElectricChargeRequired
        {
			get { return _ECRequired; }
			set { _ECRequired = value; }
        }
    }
}