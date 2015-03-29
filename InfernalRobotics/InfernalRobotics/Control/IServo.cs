using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    public interface IServo
    {
        string Name { get; set; }
        ILinearControl Linear { get; }
        IPresetableControl Preset { get; }
        
        // Scheduled for execution
        MuMechToggle RawServo { get; }
    }
}
