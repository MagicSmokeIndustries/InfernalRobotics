namespace InfernalRobotics.Control
{
    public interface IMechanism
    {
        /// <summary>
        /// Gets the current position.
        /// </summary>
        /// <value>The position.</value>
        float Position { get; }

        /// <summary>
        /// Returns rotateMin or translateMin, i.e. config values
        /// </summary>
        float MinPosition { get; }

        /// <summary>
        /// Returns/sets current tweaked MinPosition value
        /// </summary>
        float MinPositionLimit { get; set; }

        /// <summary>
        /// Returns rotateMax or translateMax, i.e. config values
        /// </summary>
        float MaxPosition { get; }

        /// <summary>
        /// Returns/sets current tweaked MaxPosition value
        /// </summary>
        float MaxPositionLimit { get; set; }

        /// <summary>
        /// Returns/sets default position for the part, i.e. position to which MoveCenter/Reset returns the servo
        /// </summary>
        float DefaultPosition { get; set; }

        /// <summary>
        /// Returns true if servo is currently moving
        /// </summary>
        bool IsMoving { get; }

        /// <summary>
        /// Returns true is the servo is an uncontrolled part (for example washer)
        /// </summary>
        bool IsFreeMoving { get; }

        /// <summary>
        /// Returns/set locked state of the servo. Locked servos do not move until unlocked.
        /// </summary>
        bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets the spring power.
        /// </summary>
        /// <value>The spring power.</value>
        float SpringPower { get; set; }

        /// <summary>
        /// Gets or sets the damping power for spring. Usen in conjuction with SpringPower to create suspension effect.
        /// </summary>
        /// <value>The damping power.</value>
        float DampingPower { get; set; }

        /// <summary>
        /// Reinitialize the Mechanism, update joint parameters.
        /// </summary>
        void Reconfigure();

        /// <summary>
        /// Used in the editor to reset a parts state to Default
        /// </summary>
        void Reset();

        /// <summary>
        /// Used in the editor to apply Servo limits to symmetry counterparts
        /// </summary>
        void ApplyLimitsToSymmetry();
    }
}