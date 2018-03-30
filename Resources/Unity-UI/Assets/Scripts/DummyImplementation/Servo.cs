using System;

namespace InfernalRobotics.Control
{
    class Servo : IServo
    {
		
        private readonly IPresetable preset;
        private readonly IMechanism mechanism;
        private readonly IServoMotor motor;
        private readonly IControlGroup controlGroup;
        private readonly IServoInput input;

		private string _name = "New dummy servo";
		private readonly uint _UID;

		private bool _highlight = false;
		private float _ecReq = 10f;

        public Servo()
        {
            
            controlGroup = new ControlGroup();
            input = new ServoInput();

            mechanism = new MechanismBase();
            motor = new ServoMotor ();

            preset = new ServoPreset(this);

			_UID = (uint)new Random ().Next ();

        }

        public string Name
        {
			get { return _name; }
            set { _name = value; }
        }
        public uint UID
        {
			get {return _UID; }
        }
        public bool Highlight
        {
			set { _highlight = value; }
        }

        public float ElectricChargeRequired
        {
			get { return _ecReq; }
			set { _ecReq = value; }
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

		public bool RawServo 
		{
			get { return true; }
		}
    }
}