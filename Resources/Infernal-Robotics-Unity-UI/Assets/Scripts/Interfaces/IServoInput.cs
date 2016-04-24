namespace InfernalRobotics.Control
{
    public interface IServoInput
    {
        /// <summary>
        /// Keybinding for servo's MoveForward key
        /// </summary>
        string Forward { get; set; }
        /// <summary>
        /// Keybinding for servo's MoveBackward key
        /// </summary>
        string Reverse { get; set; }
    }
}