namespace InfernalRobotics.Control
{
    public interface IServoGroup    
    {
        string Name { get; set; }
        float ElectricChargeRequired { get; }
    }
}