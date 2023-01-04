using System.Collections.Generic;


namespace InfernalRobotics_v3.Interfaces
{
	public interface IPresetable
	{
		// Adds a preset at "position" to the list of presets.
		void Add(float? position = null);

		// Removes preset at index presetIndex from the list of preset positions
		void RemoveAt(int presetIndex);
		
		// Number of preset positions
		int Count { get; }

		// Iterator to access preset at "index"
		float this[int index] { get; set; }

		// Preset sorter to implement sorting of the list of preset positions
		void Sort(IComparer<float> sorter = null);

		// Orders the servo to move to previous preset position
		void MovePrev();

		// Orders the servo to move to next preset position
		void MoveNext();

		// Orders the servo to move to a present number presetIndex
		void MoveTo(int presetIndex);

		// Sets floor to nearest preset position index below current position and 0 if there are none, -1 in case of no Presets
		// and ceiling to nearest preset position index above current position and Max(Preset.Count - 1,0) if there are none, -1 in case of no Presets
		void GetNearestPresets(out int floor, out int ceiling);

		////////////////////////////////////////
		// Editor

		void EditorMovePrev();
		void EditorMoveNext();
		void EditorMoveTo(int presetIndex);
	}
}