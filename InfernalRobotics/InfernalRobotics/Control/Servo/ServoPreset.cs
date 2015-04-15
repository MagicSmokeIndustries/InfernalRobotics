using InfernalRobotics.Module;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics.Control.Servo
{
    internal class ServoPreset : IPresetable
    {
        private readonly MuMechToggle rawServo;
        private readonly IServo servo;

        public ServoPreset(MuMechToggle rawServo, IServo servo)
        {
            this.rawServo = rawServo;
            this.servo = servo;
        }

        public int Count
        {
            get { return rawServo.PresetPositions.Count; }
        }

        public void MovePrev()
        {
            if (HighLogic.LoadedSceneIsEditor) 
            {
                int f, c;
                GetNearestPresets (out f, out c);
                MoveTo (f);
            }
            else
                rawServo.MovePrevPreset ();
        }

        public void MoveNext()
        {
            if (HighLogic.LoadedSceneIsEditor) 
            {
                int f, c;
                GetNearestPresets (out f, out c);
                MoveTo (c);
            }
            else
                rawServo.MoveNextPreset();
        }

        public void Save(bool symmetry = false)
        {
            Sort();

            rawServo.presetPositionsSerialized = rawServo.SerializePresets();

            if (symmetry && rawServo.part.symmetryCounterparts.Count > 1)
            {
                foreach (Part part in rawServo.part.symmetryCounterparts)
                {
                    ((MuMechToggle)part.Modules["MuMechToggle"]).presetPositionsSerialized = rawServo.presetPositionsSerialized;
                    ((MuMechToggle)part.Modules["MuMechToggle"]).ParsePresetPositions();
                }
            }
        }

        public void Add(float? position = null)
        {
            rawServo.PresetPositions.Add(position == null ? rawServo.Position : position.Value);
        }

        public void Sort(IComparer<float> sorter = null)
        {
            if (sorter != null)
            {
                rawServo.PresetPositions.Sort(sorter);
            }
            else
            {
                rawServo.PresetPositions.Sort();
            }
        }

        public float this[int index]
        {
            get
            {
                var internalPosition = rawServo.PresetPositions[index];
                return rawServo.Translator.ToExternalPos(internalPosition);
            }
            set
            {
                var tmpValue = rawServo.Translator.ToInternalPos(value);
                tmpValue = Mathf.Clamp(tmpValue, servo.Mechanism.MinPositionLimit, servo.Mechanism.MaxPositionLimit);
                rawServo.PresetPositions[index] = tmpValue;
            }
        }

        public void MoveTo(int presetIndex)
        {
            if (rawServo.PresetPositions == null || rawServo.PresetPositions.Count == 0
            || presetIndex < 0 || presetIndex >= rawServo.PresetPositions.Count)
                return;

            float nextPosition = rawServo.PresetPositions[presetIndex];

            if (HighLogic.LoadedSceneIsEditor)
            {
                var deltaPosition = nextPosition - (rawServo.Position);
                rawServo.ApplyDeltaPos(deltaPosition);
            }
            else
            {
                //because Translator expects position in external coordinates
                nextPosition = rawServo.Translator.ToExternalPos(nextPosition);
                rawServo.Translator.Move(nextPosition, rawServo.customSpeed * rawServo.speedTweak);
            }

            Logger.Log("[Action] MoveToPreset, index=" + presetIndex + " currentPos = " + rawServo.Position + ", nextPosition=" + nextPosition, Logger.Level.Debug);
        }

        public void GetNearestPresets(out int floor, out int ceiling)
        {
            floor = -1;
            ceiling = -1;

            if (rawServo.PresetPositions == null || rawServo.PresetPositions.Count == 0)
                return;

            ceiling = rawServo.PresetPositions.FindIndex (p => p > rawServo.Position);

            if (ceiling == -1)
                ceiling = rawServo.PresetPositions.Count - 1;
            
            floor = rawServo.PresetPositions.FindLastIndex (p => p < rawServo.Position);
            if (floor == -1)
                floor = 0;

            Logger.Log ("GetNearestPresets, f = " + floor + ", c =  " + ceiling, Logger.Level.Debug);
        }

        public void RemoveAt(int presetIndex)
        {
            rawServo.PresetPositions.RemoveAt(presetIndex);
        }
    }
}