using System;

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
            Active = false;
            CmdVelocity = 0f;
            CmdPosition = 0f;
        }

        // dynamic state
        public float CmdPosition { get; set; }
        public float CmdVelocity { get; set; }
        public bool Active { get; set; }
        public float Position { get; set; }
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
            SetCommand(oldCmd + cPosDelta, cVel);
        }

        public void SetCommand(float cPos, float cVel)
        {
            CmdPosition = cPos;

            if (cVel != CmdVelocity || cPos != CmdPosition)
            {
                if (IsModulo)
                {
                    if ((cVel != 0) && (!float.IsPositiveInfinity(cVel)) && (!float.IsNegativeInfinity(cVel))) // modulo & positioning mode:
                    {                                                                     // add full turns if we move fast
                        Position = ReduceModulo(Position);
                        float brakeDist = 0.5f * Velocity * Velocity / (MaxAcceleration * 0.9f);                  // 10% acc reserve for interpolation errors
                        Position -= Math.Sign(Velocity) * (MaxPosition - MinPosition) * (float)Math.Round(brakeDist / (MaxPosition - MinPosition));
                        //Debug.Log(string.Format("[Interpolator]: setCommand modulo correction: newPos= {0}", Position));
                    }
                }
                //Debug.Log(string.Format("[Interpolator]: setCommand {0}, {1}, (vel={2})\n", cPos, cVel, Velocity));
                CmdVelocity = cVel;
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

        public void SetFinished()
        {
            Velocity = 0;
            Active = false;
            //Debug.Log("[Interpolator] finished! (pos=" + Position.ToString() + ")\n");
        }

        public void Update(float deltaT)
        {
            if (!Active)
                return;

            bool isSpeedMode = IsModulo && (float.IsPositiveInfinity(CmdPosition) || float.IsNegativeInfinity(CmdPosition) || (CmdVelocity == 0));
            float maxDeltaVel = MaxAcceleration * deltaT;
            float targetPos = Math.Min(CmdPosition, MaxPosition);
            targetPos = Math.Max(targetPos, MinPosition);
            //Debug.Log("Update: targetPos=" +targetPos.ToString() +", cmdPos="+cmdPos + ",min/maxpos=" +minPos.ToString()+","+maxPos.ToString());

            if ((Math.Abs(Velocity) < maxDeltaVel) && // end conditions
                (Math.Abs(targetPos - Position) < (2f * maxDeltaVel * deltaT)))
            { // (generous to avoid oscillations)
                //Debug.Log("pos=" + pos.ToString() + "targetPos="+targetPos.ToString() +", 2f*maxDeltaVel*dalteT=" + (2f * maxDeltaVel * deltaT).ToString());
                Position = targetPos;
                SetFinished();
                return;
            }
            else if (CmdVelocity == 0 && Math.Abs(Velocity) < maxDeltaVel)
            {
                SetFinished();
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
            var result = "Ipo: act=" + String.Format("{0,6:0.0}", Position);
            result += ", " + String.Format("{0,6:0.0}", Velocity);
            result += ", cmd= " + String.Format("{0,6:0.0}", CmdPosition);
            result += ", " + String.Format("{0,6:0.0}", CmdVelocity);
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