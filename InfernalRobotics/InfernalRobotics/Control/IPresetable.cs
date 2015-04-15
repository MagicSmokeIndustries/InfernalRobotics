using System.Collections.Generic;

namespace InfernalRobotics.Control
{
    public interface IPresetable
    {
        int Count { get; }

        void MovePrev();
        void MoveNext();
        void MoveTo(int presetIndex);
        void RemoveAt(int presetIndex);
        void Sort(IComparer<float> sorter = null);
        /// <summary>
        /// Sets floor to nearest preset position index below current position and 0 if there are none, -1 in case of no Presets
        /// and ceiling to nearest preset position index above current position and Max(Preset.Count - 1,0) if there are none, -1 in case of no Presets
        /// </summary>
        /// <param name="floor">Floor.</param>
        /// <param name="ceiling">Ceiling.</param>
        void GetNearestPresets (out int floor, out int ceiling);
        /// <summary>
        /// Persists the current presets to the save file
        /// </summary>
        /// <param name="symmetry">If the part is part of a symmetry group, should the changes get propagated to all parts?</param>
        void Save(bool symmetry = false);
        void Add(float? position = null);
        float this[int index] { get; set; }
    }
}