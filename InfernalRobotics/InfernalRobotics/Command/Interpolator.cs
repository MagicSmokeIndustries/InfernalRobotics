using System;
using UnityEngine; // for debug log output

namespace InfernalRobotics.Command
{
    public class Interpolator
    {
        public Interpolator()
        {
            IsModulo = true;
            MaxAcceleration = 200f;
            MaxVelocity = 5000f;
            MaxPosition = 180f;
            MinPosition = -180f;
            Velocity = 0f;
            Position = 0f;
            OldPosition = 0f;
            Active = false;
            CmdVelocity = 0f;
            CmdPosition = 0f;
        }

        // dynamic state
        public float CmdPosition { get; set; }
        public float CmdVelocity { get; set; }
        public bool Active { get; set; }
        public float Position { get; set; }
        public float OldPosition { get; set; }
        public float Velocity { get; set; }

        // config
        public float MinPosition { get; set; }
        public float MaxPosition { get; set; }
        public float MaxVelocity { get; set; }
        public float MaxAcceleration { get; set; }
        public bool IsModulo { get; set; }

        public float GetPosition()
        {
            return ReduceModulo(Position);
        }

        // incremental Command
        public void SetIncrementalCommand(float cPosDelta, float cVel)
        {
            float oldCmd = Active ? CmdPosition : Position;
            Logger.Log(string.Format("setIncCmd: oldCmd={0}, cPosDelta={1},cVel={2}", oldCmd, cPosDelta, cVel), Logger.Level.SuperVerbose);
            SetCommand(oldCmd + cPosDelta, cVel);
        }

        public void SetCommand(float cPos, float cVel)
        {

            if (cVel != CmdVelocity || cPos != CmdPosition)
            {
                if (IsModulo)
                {
                    if ((cVel != 0f) && (!float.IsPositiveInfinity(cVel)) && (!float.IsNegativeInfinity(cVel))) // modulo & positioning mode:
                    {                                                                     // add full turns if we move fast
                        Position = ReduceModulo(Position);
                        float brakeDist = 0.5f * Velocity * Velocity / (MaxAcceleration * 0.9f);                  // 10% acc reserve for interpolation errors
                        Position -= Math.Sign(Velocity) * (MaxPosition - MinPosition) * (float)Math.Round(brakeDist / (MaxPosition - MinPosition));
                        Logger.Log(string.Format("[Interpolator]: setCommand modulo correction: newPos= {0}", Position), Logger.Level.SuperVerbose);
                    }
                }
                Logger.Log(string.Format("[Interpolator]: setCommand {0}, {1}, (vel={2})\n", cPos, cVel, Velocity), Logger.Level.SuperVerbose);

                CmdVelocity = cVel;
                CmdPosition = cPos;
                Active = true;
            }

            // make cVel non-negative?
        }

        public float ReduceModulo(float value)
        {
            if (!IsModulo)
                return value;

            float range = MaxPosition - MinPosition;
            float result = (value - MinPosition) % range;
            if (result < 0)        // result of % operation can be negative!
                result += range;

            result += MinPosition;
            return result;
        }

        public void Update(float deltaT)
        {
            if (!Active)
                return;

            OldPosition = Position;

            bool isSpeedMode = IsModulo && (float.IsPositiveInfinity(CmdPosition) || float.IsNegativeInfinity(CmdPosition) || (CmdVelocity == 0f));
            float maxDeltaVel = MaxAcceleration * deltaT;
            float targetPos = Math.Min(CmdPosition, MaxPosition);
            targetPos = Math.Max(targetPos, MinPosition);
            Logger.Log(string.Format("Update: targetPos={0}, cmdPos={1},min/maxpos={2},{3}", targetPos, CmdPosition, MinPosition, MaxPosition), Logger.Level.SuperVerbose);

            if ((Math.Abs(Velocity) < maxDeltaVel) &&
                ((Position == targetPos) || (CmdVelocity == 0f)))
            {
                Active = false;
                Velocity = 0;
                Logger.Log(string.Format("[Interpolator] finished! pos={0}, target={1}", Position, targetPos), Logger.Level.SuperVerbose);
                return;
            }

            if ((Math.Abs(Velocity) < maxDeltaVel) && // end conditions
                (Math.Abs(targetPos - Position) < (2f * maxDeltaVel * deltaT)))
            { // (generous to avoid oscillations)
                Logger.Log(string.Format("pos={0} targetPos={1}, 2f*maxDeltaVel*dalteT={2}", Position, targetPos, (2f * maxDeltaVel * deltaT)), Logger.Level.SuperVerbose);
                Position = targetPos;
                return;
            }

            float newVel = Math.Min(CmdVelocity, MaxVelocity);
            if (!isSpeedMode)
            {
                var brakeVel = (float)Math.Sqrt(1.8f * MaxAcceleration * Math.Abs(targetPos - Position)); // brake ramp to cmdPos
                newVel = Math.Min(newVel, brakeVel);                                           // (keep 10% acc reserve)
            }
            newVel *= Math.Sign(CmdPosition - Position);            // direction
            newVel = Math.Min(newVel, Velocity + maxDeltaVel); // acceleration limit
            newVel = Math.Max(newVel, Velocity - maxDeltaVel);

            Velocity = newVel;
            Position += Velocity * deltaT;

            if (!IsModulo)
            {
                if (Position >= MaxPosition)
                { 			     // hard limit on Endpositions
                    Position = MaxPosition;
                    Velocity = 0f;
                }
                else if (Position <= MinPosition)
                {
                    Position = MinPosition;
                    Velocity = 0f;
                }
            }
            else
            {
                if (isSpeedMode)
                {
                    Position = ReduceModulo(Position);
                }
            }
        }

        public string StateToString()
        {
            var result = string.Format("Ipo: act= {0,6:0.0}", Position);
            result += string.Format(", {0,6:0.0}", Velocity);
            result += string.Format(", cmd= {0,6:0.0}", CmdPosition);
            result += string.Format(", {0,6:0.0}", CmdVelocity);
            result += string.Format(", active= {0}", Active);
            return result;
        }

        public override string ToString()
        {
            var result = "Interpolator {";
            result += "\n CmdPosition = " + CmdPosition;
            result += "\n CmdVelocity = " + CmdVelocity;
            result += "\n Active = " + Active;
            result += "\n Position = " + Position;
            result += "\n Velocity = " + Velocity;
            result += "\n MinPosition = " + MinPosition;
            result += "\n MaxPosition = " + MaxPosition;
            result += "\n MaxVelocity = " + MaxVelocity;
            result += "\n MaxAcceleration = " + MaxAcceleration;
            result += "\n IsModulo = " + IsModulo;
            return result + "\n}";
        }
    }
}