using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    public interface IServo
    {
        string Name { get; set; }

        IMechanism Mechanism { get; }
        IPresetable Preset { get; }
        IServoGroup Group { get; }
        IServoInput Input { get; }
        
        // Scheduled for execution
        MuMechToggle RawServo { get; }
    }
}
