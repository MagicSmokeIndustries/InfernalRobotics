using InfernalRobotics.Module;

namespace InfernalRobotics.Control
{
    public interface IServo
    {
        ILinearControl Linear { get; }
        IPresetableControl Preset { get; }
        
        // Scheduled for execution
        MuMechToggle RawServo { get; }
    }
}
