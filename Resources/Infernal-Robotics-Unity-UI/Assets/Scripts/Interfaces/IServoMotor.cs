using System;

namespace InfernalRobotics.Control
{
    public interface IServoMotor
    {
        /// <summary>
        /// Gets or sets the max torque of the servo motor
        /// </summary>
        /// <value>The max torque.</value>
        float MaxTorque { get; set;}

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
        /// Gets the garget position.
        /// TargetPosition should be set with Move commands.
        /// </summary>
        /// <value>The position.</value>
        float TargetPosition { get; }

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
    }
}

