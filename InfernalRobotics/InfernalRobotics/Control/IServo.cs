using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    public interface IPart
    {
        string Name { get; set; }
        bool Highlight { set; }
    }

    public interface IServo : IPart
    {
        IMechanism Mechanism { get; }
        IPresetable Preset { get; }
        IServoGroup Group { get; }
        IServoInput Input { get; }
        
        // Scheduled for execution
        MuMechToggle RawServo { get; }
    }
}
