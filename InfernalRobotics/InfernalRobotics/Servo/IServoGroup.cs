namespace InfernalRobotics.Servo
{
    public interface IServoGroup    
    {
        string Name { get; set; }
        float ElectricChargeRequired { get; }
    }
}