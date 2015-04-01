using InfernalRobotics.Module;

namespace InfernalRobotics.Control.Servo
{
    internal class ServoPreset : IPresetable
    {
        private readonly MuMechToggle rawServo;

        public ServoPreset(MuMechToggle rawServo)
        {
            this.rawServo = rawServo;
        }

        public void MovePrev()
        {
            rawServo.MovePrevPreset();
        }

        public void MoveNext()
        {
            rawServo.MoveNextPreset();
        }

        public void MoveTo(int presetIndex)
        {
            rawServo.MoveToPreset(presetIndex);
        }

        public float GetPositionAt (int presetIndex)
        {
            //TODO: add some checks for presetIndex
            return rawServo.PresetPositions [presetIndex];
        }
        public void SetPositionAt (int presetIndex, float position)
        {
            rawServo.PresetPositions [presetIndex] = position;
        }

        public void RemoveAt (int presetIndex)
        {
            rawServo.PresetPositions.RemoveAt (presetIndex);
        }
        public void Add (float position)
        {
            rawServo.PresetPositions.Add (position);
        }

        public void Save()
        {
            rawServo.PresetPositions.Sort();
            rawServo.presetPositionsSerialized = rawServo.SerializePresets();
        }

        public void SaveSymmetry()
        {
            rawServo.PresetPositions.Sort();
            rawServo.presetPositionsSerialized = rawServo.SerializePresets();

            if (rawServo.part.symmetryCounterparts.Count > 1)
            {
                foreach (Part part in rawServo.part.symmetryCounterparts)
                {
                    ((MuMechToggle)part.Modules["MuMechToggle"]).presetPositionsSerialized = rawServo.presetPositionsSerialized;
                    ((MuMechToggle)part.Modules["MuMechToggle"]).ParsePresetPositions();
                }
            }
        }
    }
}