using System;

namespace InfernalRobotics_v3.Command
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

		public float Speed { get { return speed; } }
		public float NewSpeed { get { return newSpeed; } }

		public bool IsMoving { get { return MovingType != TypeOfMovement.Stopped; } }
		public bool IsStopping { get { return targetSpeed == 0f; } }

		public void SetIncrementalCommand(float p_PositionDelta, float p_TargetSpeed)
		{
			Logger.Log(string.Format("setIncCmd: oldCmd={0}, cPosDelta={1},cVel={2}", targetPosition, p_PositionDelta, p_TargetSpeed), Logger.Level.SuperVerbose);
			SetCommand(targetPosition + p_PositionDelta, p_TargetSpeed);
		}

		public void SetCommand(float p_TargetPosition, float p_TargetSpeed)
		{
			if(p_TargetSpeed == 0.0f) // this is a 'stop'
				Stop();
			else
			{
				p_TargetSpeed = Math.Abs(p_TargetSpeed); // FEHLER, wir gehen wieder zurück auf diese Lösung

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

		public void PrepareUpdate(float p_deltaTime)
		{
			if(MovingType == TypeOfMovement.Stopped)
				return;

			switch(MovingType)
			{
			case TypeOfMovement.UpAccel:
			case TypeOfMovement.DownAccel:
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
// FEHLER, Murks, aber ich probier mal was -> das auf Punkt stoppen geht sonst nicht
	if(targetSpeed < speed) // beim Bremsen auf Punkt passiert das hier nicht
	{


				newSpeed = speed;
				if(targetSpeed < newSpeed)
					newSpeed -= maxAcceleration * p_deltaTime;
				if(targetSpeed > newSpeed)
					newSpeed = targetSpeed;

				newPosition = position + direction * 0.5f * (speed + newSpeed) * p_deltaTime;

				if(newSpeed == 0.0f)
					MovingType = TypeOfMovement.Stopped;
	}
	else
	{
				newSpeed = speed;
				newPosition = position + p_deltaTime * speed * direction;
	}
				break;

			case TypeOfMovement.Up:
			case TypeOfMovement.Down:
				newSpeed = speed;
				newPosition = position + p_deltaTime * speed * direction;
				break;
			}

			switch(MovingType)
			{
			case TypeOfMovement.UpAccel:
			case TypeOfMovement.Up:
			case TypeOfMovement.UpDecel:
			case TypeOfMovement.DownAccel:
			case TypeOfMovement.Down:
			case TypeOfMovement.DownDecel:
				{
					float MinBreakTime = newSpeed / maxAcceleration; // t = v/a
					float MinBreakDistance = 0.5f * maxAcceleration * MinBreakTime * MinBreakTime; // s = 1/2 a*t^2

// prüfen, ob ich ohne Bremsen über das Target schiesse -> FEHLER, auf Direction achten...
if((Math.Max(0.0f, speed - maxAcceleration * p_deltaTime) == 0.0f)
&& ((direction * position < direction * targetPosition)
				&& (direction *(position + p_deltaTime * Math.Max(speed, newSpeed) /* eigentlich nur Speed, aber wenn ich von 0.fastnix her beschleunige um auf 0.0 zu kommen ginge das schief*/ * direction) > direction * targetPosition)))
{
	newSpeed = 0.0f;
	newPosition = targetPosition;
}
				else
				{
					if(direction * targetPosition - MinBreakDistance < direction * newPosition)
					{
						MovingType = (MovingType & (TypeOfMovement.Up | TypeOfMovement.Down)) | TypeOfMovement.Decel;

						newPosition = position;

						// not breaking
						float NoBreakDistance = (direction * targetPosition - direction * newPosition) - MinBreakDistance;

						if(NoBreakDistance > 0.0f)
						{
							float NoBreakTime =(NoBreakDistance / speed);

							if(NoBreakTime > p_deltaTime) // if we were accelerating, we could continue to accelerate for a short time to solve this -> but it's computational more intensive and more complex -> this is why we simply do nothing
								NoBreakTime = p_deltaTime;

							newPosition += direction * speed * NoBreakTime;
							p_deltaTime -= NoBreakTime;
						}

						// breaking
						newSpeed = Math.Max(0.0f, speed - maxAcceleration * p_deltaTime);
						newPosition = newPosition + direction * 0.5f *(speed + newSpeed) * p_deltaTime;
					}
					}
				}
				break;
			}

			if(newSpeed == 0.0f)
			{
//				MovingType = ((targetSpeed * targetDirection < 0.0f) == (speed * direction < 0.0f)) ? TypeOfMovement.Stopped : (targetSpeed * targetDirection < 0.0f ? TypeOfMovement.DownAccel : TypeOfMovement.UpAccel);
//				direction = targetDirection; // FEHLER, etwas Murksig das Zeug hier... evtl. das etwas anders lösen... aber 's kommt besser

				// FEHLER noch 'n Murks... -> das Teil hält nicht korrekt an, wenn man ihm befielt -> geh nach Position xy ... danach blockiert alles und er springt von UpAccel nach DownAccel bei speed 0 ... und da kommt er nie mehr raus
//				if(position == newPosition)
//					MovingType = TypeOfMovement.Stopped;

				MovingType = TypeOfMovement.Stopped;

				if(Math.Abs(targetPosition - position) > 0.01f)
					SetCommand(targetPosition, targetSpeed);

					// FEHLER, ich probier's mal so... evtl. ist das einfacher
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
