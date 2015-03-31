namespace InfernalRobotics.Control
{
    public interface IControlGroup    
    {
        string Name { get; set; }
        float ElectricChargeRequired { get; }
    }
}