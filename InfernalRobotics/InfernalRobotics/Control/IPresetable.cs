using System.Collections.Generic;

namespace InfernalRobotics.Control
{
    public interface IPresetable
    {
        /// <summary>
        /// Number of preset positions
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Orders servo to move to previous preset position.
        /// </summary>
        void MovePrev();
        /// <summary>
        /// Orders servo to move to next preset position.
        /// </summary>
        void MoveNext();
        /// <summary>
        /// Orders servo to move to a present number presetIndex
        /// </summary>
        /// <param name="presetIndex"></param>
        void MoveTo(int presetIndex);
        /// <summary>
        /// Removes preset at index presetIndex from the list of preset positions
        /// </summary>
        /// <param name="presetIndex"></param>
        void RemoveAt(int presetIndex);
        /// <summary>
        /// Preset sorter to implement sorting of the list of preset positions
        /// </summary>
        /// <param name="sorter"></param>
        void Sort(IComparer<float> sorter = null);

        /// <summary>
        /// Sets floor to nearest preset position index below current position and 0 if there are none, -1 in case of no Presets
        /// and ceiling to nearest preset position index above current position and Max(Preset.Count - 1,0) if there are none, -1 in case of no Presets
        /// </summary>
        /// <param name="floor">Floor.</param>
        /// <param name="ceiling">Ceiling.</param>
        void GetNearestPresets(out int floor, out int ceiling);

        /// <summary>
        /// Persists the current presets to the save file
        /// </summary>
        /// <param name="symmetry">If the part is part of a symmetry group, should the changes get propagated to all parts?</param>
        void Save(bool symmetry = false);
        /// <summary>
        /// Adds a preset at "position" to the list of presets.
        /// </summary>
        /// <param name="position"></param>
        void Add(float? position = null);
        /// <summary>
        /// Iterator to access preset at "index"
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        float this[int index] { get; set; }
    }
}