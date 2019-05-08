using System;


namespace InfernalRobotics_v3.Servo
{
	public class Interpolator
	{
		public bool isModulo { get; set; }

		public float minPosition { get; set; }
		public float maxPosition { get; set; }

		public float maxSpeed { get; set; }
		public float maxAcceleration { get; set; }

	//	private const float precisionDelta = 0.001f; // FEHLER, hab anderswo 0.005f genommen -> evtl. mal festlegen (global)

		private enum TypeOfMovement { Stopped = 0, Accel = 4, Decel = 8, UpAccel = 5, Up = 1, UpDecel = 9, DownAccel = 6, Down = 2, DownDecel = 10 };
		private TypeOfMovement MovingType;

		private float position;
		private float speed;
		private float direction;

		private float targetPosition;
		private float targetSpeed;
		private float targetDirection;

		private float newPosition;
		private float newSpeed;


		public Interpolator()
		{
			MovingType = TypeOfMovement.Stopped;

			position = targetPosition = 0f;
			speed = targetSpeed = 0f;
			direction = targetDirection = 1f;
		}

		public void Initialize(float p_position, bool p_isModulo, float p_minPosition, float p_maxPosition, float p_maxSpeed, float p_maxAcceleration)
		{
			position = p_position;
			isModulo = p_isModulo;
			minPosition = p_minPosition;
			maxPosition = p_maxPosition;
			maxSpeed = p_maxSpeed;
			maxAcceleration = p_maxAcceleration;
		}

		public float TargetPosition { get { return targetPosition; } }
		public float TargetSpeed { get { return targetSpeed; } }

		public float Speed { get { return speed; } }
		public float NewSpeed { get { return newSpeed; } }

		public bool IsMoving { get { return MovingType != TypeOfMovement.Stopped; } }
		public bool IsStopping { get { return targetSpeed == 0f; } }

		public void SetIncrementalCommand(float p_PositionDelta, float p_TargetSpeed)
		{
			Logger.Log(string.Format("setIncCmd: oldCmd={0}, cPosDelta={1},cVel={2}", targetPosition, p_PositionDelta, p_TargetSpeed), Logger.Level.SuperVerbose);
			SetCommand(targetPosition + p_PositionDelta, p_TargetSpeed, (p_PositionDelta > 0.0f) && (direction > 0.0f));
		}

		public void SetCommand(float p_TargetPosition, float p_TargetSpeed, bool p_keepDirection)
		{
// FEHLER, p_keepDirection wird ignoriert...

			if(p_TargetSpeed == 0.0f) // this is a 'stop'
				Stop();
			else
			{
				if(targetSpeed != p_TargetSpeed)
				{
					if(p_TargetSpeed > maxSpeed)
						p_TargetSpeed = maxSpeed;

					targetSpeed = p_TargetSpeed;
				}

				if(targetPosition != p_TargetPosition)
				{
					if(!isModulo)
					{
						if(p_TargetPosition > maxPosition)
							p_TargetPosition = maxPosition;
						else if(p_TargetPosition < minPosition)
							p_TargetPosition = minPosition;
					}
					else if(!float.IsPositiveInfinity(p_TargetPosition) && !float.IsNegativeInfinity(p_TargetPosition))
					{
						// absolute value of p_TargetPosition <= 360
						while(p_TargetPosition > position + 360f) p_TargetPosition -= 360f;
						while(p_TargetPosition <= position - 360f) p_TargetPosition += 360f;

						// absolute value of position <= 180
						while(position > 180f) position -= 360f;
						while(position <= -180f) position += 360f;

						// correct p_TargetPosition so that we move into the correct direction (short way, not long way)
						if(p_TargetPosition > position)
						{
							if(p_TargetPosition - position > 180.0f)
								p_TargetPosition -= 360.0f;
						}
						else
						{
							if(p_TargetPosition - position < -180.0f)
								p_TargetPosition += 360.0f;
						}
					}

					targetPosition = p_TargetPosition;
					targetDirection = targetPosition < position ? -1.0f : 1.0f;
				}

				switch(MovingType)
				{
				case TypeOfMovement.Stopped:
					if((targetSpeed != 0.0f) && (targetPosition != position))
					{
						direction = targetDirection;
						MovingType = targetDirection < 0.0f ? TypeOfMovement.DownAccel : TypeOfMovement.UpAccel;
							// FEHLER sinnlos, MovingType UND Direction ... kann ich neu zusammen nehmen
					}
					break;

				case TypeOfMovement.UpAccel:
					if((targetSpeed < speed) || (targetDirection != direction))
						MovingType = TypeOfMovement.UpDecel;
					break;

				case TypeOfMovement.Up:
					if((targetSpeed < speed) || (targetDirection != direction))
						MovingType = TypeOfMovement.UpDecel;
					else if(targetSpeed > speed)
						MovingType = TypeOfMovement.UpAccel;
					break;

				case TypeOfMovement.UpDecel:
					if((targetSpeed > speed) && (targetDirection == direction))
						MovingType = TypeOfMovement.UpAccel;
					break;

				case TypeOfMovement.DownAccel:
					if((targetSpeed < speed) || (targetDirection != direction))
						MovingType = TypeOfMovement.DownDecel;
					break;

				case TypeOfMovement.Down:
					if((targetSpeed < speed) || (targetDirection != direction))
						MovingType = TypeOfMovement.DownDecel;
					else if(targetSpeed > speed)
						MovingType = TypeOfMovement.DownAccel;
					break;

				case TypeOfMovement.DownDecel:
					if((targetSpeed > speed) && (targetDirection == direction))
						MovingType = TypeOfMovement.DownAccel;
					break;
				}
			}
		}

		public bool Stop()
		{
			if(speed == 0f)
			{
				MovingType = TypeOfMovement.Stopped;

				return false;
			}
			else
			{
				if(IsMoving)
					MovingType = (MovingType & (TypeOfMovement.Up | TypeOfMovement.Down)) | TypeOfMovement.Decel;

				targetSpeed = 0f;

				return true;
			}
		}

		/*
			Explanation for NewPosition calculations
			
				s = v0*t + 1/2 a*t^2   and   v1 = v0+a*t   thus   s = v0*t + 1/2(v1-v0)*t
				this is not perfectly correct for cases in which v1 > vmax, but good enough
				because it's just a little bit a shorter movement and is easier/faster to calculate
		*/

// Idee um stuck zu handeln...
		public void ResetPosition(float p_position)
		{
			position = p_position;
		}
	
// FEHLER FEHLER -> hier evtl. sich anpassen an das, was wirklich ist und nicht weiterdrehen, wenn es nicht läuft? ... evtl. bei feststecken dann auch zurückgehen mit der realen Position?
// halt einfach so, dass wir nicht die orgPos weiter drehen als wir wirklich kommen, weil alles verklemmt ist
		public void PrepareUpdate(float p_deltaTime)
		{
			if(MovingType == TypeOfMovement.Stopped)
				return;

			// calculate new speed and position

			if(targetDirection != direction) // FEHLER, ein Versuch... vielleicht müssen wir das irgendwann mal noch etwas schöner bauen...
			{
				// decelerate at max rate but stop at target speed

				newSpeed = speed - maxAcceleration * p_deltaTime;

// FEHLER, man müsste nicht auf 0 fallen, man könnte auch gleich negativ gehen und... Werte umdrehen und so... aber gut, ist wohl eher ein Detail
				if(newSpeed < 0.0f)
					newSpeed = 0.0f;

				newPosition = position + direction * 0.5f * (speed + newSpeed) * p_deltaTime;

				if(newSpeed == 0.0f)
				{
					direction = targetDirection;
					MovingType = targetDirection < 0.0f ? TypeOfMovement.DownAccel : TypeOfMovement.UpAccel;
				}

				return;
			}

			switch(MovingType)
			{
			case TypeOfMovement.UpAccel:
			case TypeOfMovement.DownAccel:
				// accelerate at max rate

				newSpeed = speed + maxAcceleration * p_deltaTime;

				if(newSpeed >= targetSpeed)
				{
					MovingType = MovingType & (TypeOfMovement.Up | TypeOfMovement.Down);

					newSpeed = targetSpeed;
				}
	
				newPosition = position + direction * 0.5f * (speed + newSpeed) * p_deltaTime;
				break;

			case TypeOfMovement.UpDecel:
			case TypeOfMovement.DownDecel:
				if(targetSpeed < speed)
				{
					// decelerate at max rate but stop at target speed

					newSpeed = speed;
					if(targetSpeed < newSpeed)
						newSpeed -= maxAcceleration * p_deltaTime;
					if(targetSpeed > newSpeed)
						newSpeed = targetSpeed;

					newPosition = position + direction * 0.5f * (speed + newSpeed) * p_deltaTime;
				}
				else
				{
					// keep speed - braking is done by the overshoot protection

					newSpeed = speed;
					newPosition = position + p_deltaTime * speed * direction;
				}
				break;

			case TypeOfMovement.Up:
			case TypeOfMovement.Down:
				// keep speed

				newSpeed = speed;
				newPosition = position + p_deltaTime * speed * direction;
				break;
			}

			// overshoot protection

			float MinBrakeTime = newSpeed / maxAcceleration; // t = v/a
			float MinBrakeDistance = 0.5f * maxAcceleration * MinBrakeTime * MinBrakeTime; // s = 1/2 a*t^2

// prüfen, ob ich ohne Bremsen über das Target schiesse -> FEHLER, auf Direction achten...
//if(
//    (Math.Max(0.0f, speed - maxAcceleration * p_deltaTime) == 0.0f)
//&& ((direction * position < direction * targetPosition)
//            && (direction *(position + p_deltaTime * Math.Max(speed, newSpeed) /* eigentlich nur Speed, aber wenn ich von 0.fastnix her beschleunige um auf 0.0 zu kommen ginge das schief*/ * direction) > direction * targetPosition)))
//{
//newSpeed = 0.0f;
//newPosition = targetPosition;
//}
//            else
//            {

			if(direction * targetPosition < direction * newPosition + MinBrakeDistance)
			{
				float travelDistance = direction * newPosition - direction * position;

				if(travelDistance >= (direction * targetPosition - direction * position))
				{
					newSpeed = 0.0f;
					newPosition = targetPosition;
				}
				else
				{
					// not braking
					float noBrakeDistance = (direction * targetPosition - direction * newPosition) - MinBrakeDistance;

					if(noBrakeDistance > 0f)
					{
						float noBrakeTime = (noBrakeDistance / speed);

						newPosition =
							targetPosition - direction * MinBrakeDistance
							+ direction * 0.5f *(speed + newSpeed) * (p_deltaTime - noBrakeTime);
					}
					else
					{
						newPosition =
							position
							+ direction * 0.5f *(speed + newSpeed) * p_deltaTime;
					}

					// speed calculated inversely (less efficient, but more accurate)
					newSpeed = (float)Math.Sqrt((double)(2 * (direction * targetPosition - direction * newPosition) / maxAcceleration)) * maxAcceleration;
				}

				MovingType = (MovingType & (TypeOfMovement.Up | TypeOfMovement.Down)) | TypeOfMovement.Decel;
			}

			if(newSpeed == 0.0f)
			{
				MovingType = TypeOfMovement.Stopped;

				// check if we need to move into the other direction now
// FEHLER, sollte doch gar nie mehr hier landen, oder?

				if(Math.Abs(targetPosition - position) > 0.01f)
					SetCommand(targetPosition, targetSpeed, false);
			}
		}

		public void Update()
		{
			speed = newSpeed;
			position = newPosition;

			if(isModulo)
			{
				if(position < minPosition)
					position += 360f;
				else if(position > maxPosition)
					position -= 360f;
			}
		}

		public float GetPosition()
		{
			float res = position;

			if(isModulo)
			{
				if(res < minPosition)
					res += 360f;
				else if(res > maxPosition)
					res -= 360f;
			}

			return res;
		}
	}
}
