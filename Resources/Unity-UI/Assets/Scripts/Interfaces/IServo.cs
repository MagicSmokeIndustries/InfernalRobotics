namespace InfernalRobotics.Control
{
    public interface IPart
    {
        /// <summary>
        /// Servo's name
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Servo's unique identifier
        /// </summary>
        uint UID { get; }
        /// <summary>
        /// Settable only, Highlight the servo part on the vessel
        /// </summary>
        bool Highlight { set; }
        /// <summary>
        /// Amount of EC consumed by the servo
        /// </summary>
        float ElectricChargeRequired { get; set; }
    }

    public interface IServo : IPart
    {
        /// <summary>
        /// Implementation of servo's mechanical components 
        /// </summary>
        IMechanism Mechanism { get; }

        /// <summary>
        /// Implementation of servo's motor
        /// </summary>
        /// <value>The motor.</value>
        IServoMotor Motor { get; }

        /// <summary>
        /// Implementation of presets
        /// </summary>
        IPresetable Preset { get; }
        /// <summary>
        /// Servo's Group related implementation
        /// </summary>
        IControlGroup Group { get; }
        /// <summary>
        /// Implementation of servo's keyboard control elements
        /// </summary>
        IServoInput Input { get; }

        /// <summary>
        /// Soon to be deprecated reference to the actual servo
        /// </summary>
        bool RawServo { get; }
    }
}
