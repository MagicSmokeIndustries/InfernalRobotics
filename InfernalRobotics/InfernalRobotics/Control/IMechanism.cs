namespace InfernalRobotics.Control
{
    public interface IMechanism
    {
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
        /// Returns/sets servo's Acceleration multiplier
        /// </summary>
        float AccelerationLimit { get; set; }

        /// <summary>
        /// Returns/sets servo's inverted status
        /// </summary>
        bool IsAxisInverted { get; set; }

        /// <summary>
        /// the speed from part.cfg is used as the default unit of speed
        /// </summary>
        float DefaultSpeed { get; }

        /// <summary>
        /// The current rate of travel, like right now
        /// </summary>
        float CurrentSpeed { get; }

        /// <summary>
        /// The maximum speed that the servo can travel
        /// </summary>
        float MaxSpeed { get; }

        /// <summary>
        /// User setting to limit the speed of the servo
        /// </summary>
        float SpeedLimit { get; set; }

        /// <summary>
        /// Commands the servo to move in the direction that decreases its Position
        /// </summary>
        void MoveLeft();

        /// <summary>
        /// Coomands the servo to move towards its DefaultPosition
        /// </summary>
        void MoveCenter();

        /// <summary>
        /// Commands the servo to move in the direction that increases its Position
        /// </summary>
        void MoveRight();

        /// <summary>
        /// Commands the servo to stop
        /// </summary>
        void Stop();

        /// <summary>
        /// Commands the servo to move to specified position at current speed
        /// </summary>
        /// <param name="position"></param>
        void MoveTo(float position);
        
        /// <summary>
        /// Commands the servo to move to specified position at specified speed multiplier
        /// </summary>
        /// <param name="position"></param>
        /// <param name="speed"></param>
        void MoveTo(float position, float speed);
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