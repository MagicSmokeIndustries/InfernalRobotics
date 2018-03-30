

namespace InfernalRobotics.Control
{
    internal class ServoInput : IServoInput
    {
       
		private string _fwKey = "";
		private string _rvKey = "";

        public ServoInput()
        {
           
        }

        public string Forward
        {
			get { return _fwKey; }
            set 
            { 
				_fwKey= value.ToLower();
            }
        }

        public string Reverse
        {
            get { return _rvKey; }
            set 
            { 
				_rvKey = value.ToLower();
            }
        }
    }
}