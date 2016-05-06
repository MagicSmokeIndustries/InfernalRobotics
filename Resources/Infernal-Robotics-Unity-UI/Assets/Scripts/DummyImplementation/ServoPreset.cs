using System.Collections.Generic;

namespace InfernalRobotics.Control
{
    internal class ServoPreset : IPresetable
    {
        private readonly IServo servo;
		private List<float> presets;

        public ServoPreset(IServo servo)
        {
            this.servo = servo;
			this.presets = new List<float> { -20f, 0f, 20f, 120f };
        }

        public int Count
        {
			get { return presets.Count; }
        }

        public void MovePrev()
        {
			MoveTo (0);
        }

        public void MoveNext()
        {
			MoveTo (1);
        }

        public void Save(bool symmetry = false)
        {
            
        }

        public void Add(float? position = null)
        {
			presets.Add(position == null ? servo.Mechanism.Position : position.Value);
        }

        public void Sort(IComparer<float> sorter = null)
        {
            if (sorter != null)
            {
				presets.Sort(sorter);
            }
            else
            {
				presets.Sort();
            }
        }

        public float this[int index]
        {
            get
            {
				return presets[index];
            }
            set
            {
				presets[index] = value;
            }
        }

        public void MoveTo(int presetIndex)
        {
			((MechanismBase)servo.Mechanism).SetPosition(presets [presetIndex]);
        }

        public void GetNearestPresets(out int floor, out int ceiling)
        {
            floor = -1;
            ceiling = -1;

			if (presets == null || presets.Count == 0)
                return;

			ceiling = presets.FindIndex(p => p > servo.Mechanism.Position);

            if (ceiling == -1)
				ceiling = presets.Count - 1;

			floor = presets.FindLastIndex(p => p < servo.Mechanism.Position);
            if (floor == -1)
                floor = 0;

			if(servo.Motor.IsAxisInverted)
            {
                //if axis is inverted swap two nearest presets
                var tmp = ceiling;
                ceiling = floor;
                floor = tmp;
            }
        }

        public void RemoveAt(int presetIndex)
        {
            presets.RemoveAt(presetIndex);
        }
    }
}