
namespace InfernalRobotics.Control
{
    public interface IPresetable
    {
        void MovePrev();
        void MoveNext();
        void MoveTo (int presetIndex);

        float GetPositionAt (int presetIndex);
        void SetPositionAt (int presetIndex, float position);
        void RemoveAt (int presetIndex);
        void Add (float position);

        void Save();
        void SaveSymmetry();

    }
}