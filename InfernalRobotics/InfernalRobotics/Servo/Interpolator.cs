using System;


namespace InfernalRobotics_v3.Servo
{
	public class CommandHistory // FEHELR FEHLER, Debug !!! -> Probleme suchen, dann das Zeug wieder raus
	{
		public int iType; // 0 = command, 1 = update, 2 = overshoot protection

		public float position;
		public float speed;
		public float direction;

		public float targetPosition;
		public float targetSpeed;
		public float targetDirection;
	};

	public class Interpolator
	{
		public System.Collections.Generic.List<CommandHistory> hist = new System.Collections.Generic.List<CommandHistory>();

		public bool isModulo { get; set; }

		public float minPosition { get; set; }
		public float maxPosition { get; set; }

		public float maxSpeed { get; set; }
		public float maxAcceleration { get; set; }

		private enum TypeOfMovement { Stopped = 0, Accel = 4, Decel = 8, UpAccel = 5, Up = 1, UpDecel = 9, DownAccel = 6, Down = 2, DownDecel = 10, Adjust = 16 };
		private TypeOfMovement MovingType;

		private float position;
		private float speed;
		private float direction;

		private float targetPosition;
		private float targetSpeed;
		private float targetDirection;

		private float newPosition;
		private float newSpeed;

		private float oldPosition;

		private float resetPrecision = 0.5f;


		public Interpolator()
		{
			MovingType = TypeOfMovement.Stopped;

			position = targetPosition = 0f;
			speed = targetSpeed = 0f;
			direction = targetDirection = 1f;
		}

		public void Initialize(float p_position, bool p_isModulo, float p_minPosition, float p_maxPosition, float p_maxSpeed, float p_maxAcceleration, float p_resetPrecision)
		{
			// FEHLER, prüfen, ob wir stopped sind? sonst wär's keine gute Idee das neu zu setzen... glaub ich... aber, sollte ja eigentlich auch nicht passieren...

			position = p_position;
			isModulo = p_isModulo;
			minPosition = p_minPosition;
			maxPosition = p_maxPosition;
			maxSpeed = p_maxSpeed;
			maxAcceleration = p_maxAcceleration;
			resetPrecision = p_resetPrecision;

			targetPosition = position;
			targetSpeed = 0f;

			newPosition = position;
			newSpeed = speed;

			oldPosition = position;
		}

		public float TargetPosition { get { return targetPosition; } }
		public float TargetSpeed { get { return targetSpeed; } }

		public float Speed { get { return speed; } }
		public float NewSpeed { get { return newSpeed; } }

		public bool IsMoving { get { return MovingType != TypeOfMovement.Stopped; } }
		public bool IsStopping { get { return targetSpeed == 0f; } }

		public void SetIncrementalCommand(float p_PositionDelta, float p_TargetSpeed)
		{
			SetCommand(targetPosition + p_PositionDelta, p_TargetSpeed);
		}

		public void SetCommand(float p_TargetPosition, float p_TargetSpeed)
		{
if(MovingType == TypeOfMovement.Adjust) // FEHLER, neue Idee -> Overshoot hat auf 0 befohlen
	MovingType = TypeOfMovement.Stopped;

			CommandHistory he = new CommandHistory();
			he.iType = 0;
			he.position = position;
			he.speed = speed;
			he.direction = direction;
			he.targetPosition = p_TargetPosition;
			he.targetSpeed = p_TargetSpeed;

			hist.Add(he);
			while(hist.Count > 5000)
				hist.RemoveAt(0);

			if(p_TargetSpeed == 0f) // this is a 'stop'
				Stop();
			else
			{
				if(targetSpeed != p_TargetSpeed)
				{
					if(p_TargetSpeed > maxSpeed)
						p_TargetSpeed = maxSpeed;

					targetSpeed = p_TargetSpeed;
				}

				float _position = position;

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
						while(_position - 180f > p_TargetPosition) _position -= 360f;
						while(_position + 180f < p_TargetPosition) _position += 360f;

						// correct p_TargetPosition so that we move into the correct direction (short way, not long way)
						if(p_TargetPosition > _position)
						{
							if(p_TargetPosition - _position > 180f)
								p_TargetPosition -= 360f;
						}
						else
						{
							if(p_TargetPosition - _position < -180f)
								p_TargetPosition += 360f;
						}
					}

					targetPosition = p_TargetPosition;

					targetDirection = targetPosition < _position ? -1f : 1f;
				}
				else if(Math.Abs(targetPosition - _position) < 25f) // FEHLER, Gewürge... ich darf nur innerhalb von 25 "Einheiten" die Richtung wechseln... einfach so -> weil ich überschiessen könnte... na ja... Gewürge, sag's ja
					targetDirection = targetPosition < _position ? -1f : 1f;

he.targetDirection = targetDirection;

				switch(MovingType)
				{
				case TypeOfMovement.Stopped:
					if((targetSpeed != 0f) && (targetPosition != position))
					{
						direction = targetDirection;
						MovingType = targetDirection < 0f ? TypeOfMovement.DownAccel : TypeOfMovement.UpAccel;
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

				targetPosition = position;

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

		public void ResetPosition(float p_position)
		{
			float _oldPosition = UnModulo(oldPosition, p_position);
			float _newPosition = UnModulo(newPosition, p_position);

			if(direction > 0f)
			{
				if(p_position + resetPrecision < _oldPosition)
				{
					position = oldPosition;
					speed = 0f;
				}
				else if(p_position + resetPrecision < _newPosition)
				{
					position = p_position;
					speed = Math.Abs(oldPosition - position);
				}
			}
			else
			{
				if(p_position - resetPrecision > _oldPosition)
				{
					position = oldPosition;
					speed = 0f;
				}
				else if(p_position - resetPrecision > _newPosition)
				{
					position = p_position;
					speed = Math.Abs(oldPosition - position);
				}
			}

			if(isModulo)
				position = Modulo(position);
			else
			{
				if(position < minPosition)
					position = minPosition;
				else if(position > maxPosition)
					position = maxPosition;
			}
		}
	
		public void OvershootProtection(float p_deltaTime)
		{
			// -> factor 0.97 to prevent an overshoot better

			float MinBrakeTime = newSpeed / (0.97f * maxAcceleration); // t = v/a
			float MinBrakeDistance = 0.5f * (0.97f * maxAcceleration) * MinBrakeTime * MinBrakeTime; // s = 1/2 a*t^2

			float _targetPosition = targetPosition;

			if(isModulo)
			{
				while(Math.Abs(_targetPosition + 360f - position) < Math.Abs(_targetPosition - position))
					_targetPosition += 360f;
				while(Math.Abs(_targetPosition - 360f - position) < Math.Abs(_targetPosition - position))
					_targetPosition -= 360f;
			}

			if(direction * _targetPosition < direction * newPosition + MinBrakeDistance)
			{
				float travelDistance = /*direction **/ newPosition - /*direction **/ position;

				if(Math.Abs(travelDistance) >= Math.Abs(_targetPosition - position)) // FEHLER, unschön, dass wir 2 mal abs brauchen... aber gut... was soll man machen, das Zeug ist noch überarbeitungswürdig...
				{
					newSpeed = 0f;
					newPosition = targetPosition;

			CommandHistory he = new CommandHistory();
					he.iType = 2;
					he.position = position;
					he.speed = speed;
					he.direction = direction;

					he.targetPosition = newPosition;
					he.targetSpeed = newSpeed;
					he.targetDirection = direction; // FEHLER, dass wir diese direction nicht ändern, das ist doch ein Problem, oder?

					hist.Add(he);
					while (hist.Count > 5000)
						hist.RemoveAt(0);
				}
				else
				{
					// not braking
					float noBrakeDistance = (direction * _targetPosition - direction * newPosition) - MinBrakeDistance;

					if(noBrakeDistance > 0f)
					{
						float noBrakeTime = (noBrakeDistance / speed);

						newSpeed = speed - maxAcceleration * (p_deltaTime - noBrakeTime);

if (newSpeed < 0.0f)
	newSpeed = speed * 0.5f; // FEHLER, so 'ne Art Bugfix und "Glättung" -> könnte man evtl. schöner lösen... nur wie? ich brauch 2 Frames, also... machen wir mal halbe halbe, oder?

						newPosition =
							_targetPosition - direction * MinBrakeDistance
							+ direction * 0.5f * (speed + newSpeed) * (p_deltaTime - noBrakeTime);
					}
					else
					{
						newSpeed = speed - maxAcceleration * p_deltaTime;

if(newSpeed < 0.0f)
{
	if(speed < 0.01f)
		newSpeed = 0.2f; // FEHLER, noch mehr Bugfix, weil sonst bei super kleinen Änderungen kein Start zustande kommt
	else
		newSpeed = speed * 0.5f; // FEHLER, so 'ne Art Bugfix und "Glättung" -> könnte man evtl. schöner lösen... nur wie? ich brauch 2 Frames, also... machen wir mal halbe halbe, oder?
}

						newPosition =
							position
							+ direction * 0.5f * (speed + newSpeed) * p_deltaTime;
					}


			CommandHistory he = new CommandHistory();
					he.iType = 2;
					he.position = position;
					he.speed = speed;
					he.direction = direction;

					he.targetPosition = newPosition;
					he.targetSpeed = newSpeed;
					he.targetDirection = direction;	// FEHLER, dass wir diese direction nicht ädnern, das ist doch ein Problem, oder?

					hist.Add(he);
					while (hist.Count > 5000)
						hist.RemoveAt(0);

					// speed calculated inversely (less efficient, but more accurate)
					//			newSpeed = (float)Math.Sqrt((double)(2 * (direction * _targetPosition - direction * newPosition) / maxAcceleration)) * maxAcceleration;
				}

				MovingType = (MovingType & (TypeOfMovement.Up | TypeOfMovement.Down)) | TypeOfMovement.Decel;
			}
		}

		public void PrepareUpdate(float p_deltaTime)
		{
			if(MovingType == TypeOfMovement.Adjust) // FEHLER, neue Idee -> Overshoot hat auf 0 befohlen
				MovingType = TypeOfMovement.Stopped;

			if(MovingType == TypeOfMovement.Stopped)
				return;

/*
 sich 'ne History merken
von dem was hier berechnet wurde und von dem was sonst passiert (commanded) wurde
und auch ob ein command von der overshoot-protecton kam... dann sehen, ob wir ein Muster erkennen...

und auch klären, wieso das Teil bei -> goto 0.0 auf 0.01 oder 0.02 anhält? ... das ist doch auch komisch... aber, zuerst die Histry
 */
			CommandHistory he = new CommandHistory();
			he.iType = 1;
			he.position = position;
			he.speed = speed;
			he.direction = direction;

			hist.Add(he);
			while (hist.Count > 5000)
				hist.RemoveAt(0);

			// calculate new speed and position

			if (targetDirection != direction) // FEHLER, ein Versuch... vielleicht müssen wir das irgendwann mal noch etwas schöner bauen...
			{
				// decelerate at max rate but stop at target speed

				newSpeed = speed - maxAcceleration * p_deltaTime;

				// OPTION: we could also change the direction immediately without going to 0 first
				if(newSpeed < 0f)
					newSpeed = 0f;

				newPosition = position + direction * 0.5f * (speed + newSpeed) * p_deltaTime;

				if(newSpeed == 0f)
				{
					direction = targetDirection;
					MovingType = targetDirection < 0f ? TypeOfMovement.DownAccel : TypeOfMovement.UpAccel;
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

	he.targetPosition = newPosition;
	he.targetSpeed = newSpeed;
	he.targetDirection = direction;

			// overshoot protection
			OvershootProtection(p_deltaTime);

			if(newSpeed == 0f)
			{
				MovingType = TypeOfMovement.Adjust; // FEHLER, war stopped, soll aber noch nicht "IsStopped" sein -> daher mal die Idee -> oder IsStoped anpassen?

				// check if we need to move into the other direction now (due to an overshooting)

				if(Math.Abs(targetPosition - position) > 0.01f)
					SetCommand(targetPosition, targetSpeed);
			}
		}

		public void Update()
		{
			speed = newSpeed;
			oldPosition = position;
			position = newPosition;

			if(isModulo)
				position = Modulo(position);
		}

		public float GetPosition()
		{
			if(isModulo)
				return Modulo(position);
			return position;
		}

		public float Modulo(float value)
		{
			if(value <= minPosition)
				return value + 360f;
			else if(value >= maxPosition)
				return value - 360f;
			return value;
		}

		public float UnModulo(float value, float target)
		{
			if(value < target)
			{
				if(Math.Abs((value + 360f) - target) < Math.Abs(value - target))
 					return value + 360f;
			}
			else
			{
				if(Math.Abs((value - 360f) - target) < Math.Abs(value - target))
 					return value - 360f;
			}
			return value;
		}
	}
}
