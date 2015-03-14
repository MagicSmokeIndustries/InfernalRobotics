using System;
using UnityEngine;

namespace InfernalRobotics.Module
{
	public class Interpolator
	{
		// dynamic state
		public float cmdPos = 0f;
		public float cmdVel = 0f;
		public bool active = false;
		public float pos = 0f;
		public float vel = 0f;

		// config
		public float minPos = -180f;
		public float maxPos = 180f;
		public float maxVel = 5000f;
		public float maxAcc = 200f;
		public bool isModulo = true;

        public float getPos()
        {
            return reduceModulo(pos);
        }

        // incremental Command
        public void setIncCommand(float cPosDelta, float cVel)
        {
            float oldCmd = active ? cmdPos : pos;
            setCommand(oldCmd + cPosDelta, cVel);
        }

		public void setCommand(float cPos, float cVel)
		{
			cmdPos = cPos;

			if (cVel != cmdVel || cPos != cmdPos)
			{
				if (isModulo) {
					if ((cVel != 0) && (!float.IsPositiveInfinity(cVel)) && (!float.IsNegativeInfinity(cVel))) // modulo & positioning mode:
					{                                                                     // add full turns if we move fast
						pos = reduceModulo (pos);
						float brakeDist = 0.5f * vel*vel / (maxAcc*0.9f);                  // 10% acc reserve for interpolation errors
						pos -= Math.Sign(vel) * (maxPos - minPos) * (float)Math.Round (brakeDist / (maxPos - minPos));
                        Debug.Log("[Interpolator]: setCommand modulo correction: newPos=" + pos.ToString());
					}
				}
				Debug.Log ("[Interpolator]: setCommand " + cPos.ToString () + ", " + cVel.ToString () + ", (vel=" + vel.ToString() +")\n");
				cmdVel = cVel;
				active = true;
			}

			// make cVel non-negative?
		}

		public float reduceModulo(float value)
		{
            if (!isModulo)
                return value;

            float range = maxPos - minPos;
            float result = (value - minPos) % range;
            if (result < 0)        // result of % operation can be negative!
                result += range;

            result += minPos;
            return result;
		}

		public void SetFinished()
		{
			vel = 0;
			active = false;
			Debug.Log ("[Interpolator] finished! (pos=" + pos.ToString() + ")\n");
		}

		public void Update(float deltaT)
		{
			if (!active)
				return;

			bool isSpeedMode = isModulo && (float.IsPositiveInfinity (cmdPos) || float.IsNegativeInfinity (cmdPos) || (cmdVel == 0));
			float maxDeltaVel = maxAcc * deltaT;
			float targetPos = Math.Min (cmdPos, maxPos);
			targetPos = Math.Max (targetPos, minPos);
            //Debug.Log("Update: targetPos=" +targetPos.ToString() +", cmdPos="+cmdPos + ",min/maxpos=" +minPos.ToString()+","+maxPos.ToString());

			if ((Math.Abs(vel) < maxDeltaVel) && // end conditions
				(Math.Abs(targetPos - pos) < (2f * maxDeltaVel * deltaT))) { // (generous to avoid oscillations)
                //Debug.Log("pos=" + pos.ToString() + "targetPos="+targetPos.ToString() +", 2f*maxDeltaVel*dalteT=" + (2f * maxDeltaVel * deltaT).ToString());
				pos = targetPos;
				SetFinished ();
				return;
			} else if (cmdVel == 0 && Math.Abs (vel) < maxDeltaVel) {
				SetFinished ();
				return;
			}

			float newVel = Math.Min(cmdVel, maxVel);
			if (!isSpeedMode)
			{
				float brakeVel = (float)Math.Sqrt (1.8f * maxAcc * Math.Abs (targetPos - pos)); // brake ramp to cmdPos
				newVel = Math.Min (newVel, brakeVel);                                           // (keep 10% acc reserve)
			}
			newVel *= Math.Sign (cmdPos - pos);            // direction
			newVel = Math.Min (newVel, vel + maxDeltaVel); // acceleration limit
			newVel = Math.Max (newVel, vel - maxDeltaVel);

			vel = newVel;
			pos += vel * deltaT;

			if (!isModulo)
			{
				if (pos >= maxPos) { 			     // hard limit on Endpositions
					pos = maxPos;
					vel = 0f;
				} else if (pos <= minPos) {
					pos = minPos;
					vel = 0f;
				}
			}
			else
			{
				if (isSpeedMode)
				{
					pos = reduceModulo (pos);
				}
			}
		}

        public string StateToString()
        {
            var result = "Ipo: act=" +String.Format("{0,6:0.0}", pos);
            result += ", " + String.Format("{0,6:0.0}", vel);
            result += ", cmd= " + String.Format("{0,6:0.0}", cmdPos);
            result += ", " + String.Format("{0,6:0.0}", cmdVel);
            return result;
        }
        public override string ToString()
        {
            var result = "Interpolator {";
            result += "\n cmdPos = " + cmdPos;
            result += "\n cmdVel = " + cmdVel;
            result += "\n active = " + active;
            result += "\n pos = " + pos;
            result += "\n vel = " + vel;
            result += "\n minPos = " + minPos;
            result += "\n maxPos = " + maxPos;
            result += "\n maxVel = " + maxVel;
            result += "\n maxAcc = " + maxAcc;
            result += "\n isModulo = " + isModulo;
            return result + "\n}";
        }
	}
}

