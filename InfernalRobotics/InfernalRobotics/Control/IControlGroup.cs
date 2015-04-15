namespace InfernalRobotics.Control
{
    public interface IControlGroup    
    {
        /// <summary>
        /// Name of the Group
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Amount of EC consumed by the Group
        /// </summary>
        float ElectricChargeRequired { get; set; }
    }
}