using System;
using InfernalRobotics_v3.Control;
using InfernalRobotics_v3.Control.Servo;
using InfernalRobotics_v3.Module;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace InfernalRobotics_v3.Command
{
	public class ControlGroup
	{
		public static ControlGroup pActiveGroup = null; // currently active group (for IK at least) -> FEHLER, prüfen ob's bleibt

		protected static bool UseElectricCharge = true;

		private bool bDirty;

		private float totalElectricChargeRequirement;

		private float groupSpeedFactor;
		private string forwardKey;
		private string reverseKey;

// FEHLER FEHLER, supertemp
public bool bIsAdvancedOn = false;
public bool bIsBuildAidOn = false;
public class IServoState { public bool bIsBuildAidOn = false; }
public Dictionary<IServo, IServoState> servosState;

		private readonly List<IServo> servos;

		public List<ModuleIRServo_v3> ikServosForward = null;	// die, welche ich für IK brauche -> vorwärts (0 = Basis)
		public List<ModuleIRServo_v3> ikServosBackward = null;// die, welche ich für IK brauche -> rückwärts (0 = EndEffector)
			// -> FEHLER, temp als ModuleIRServo_v3 geführt... später evtl. wieder als IServo zu führen versuchen

		private /*readonly*/ Vessel vessel;

		public Part pEndEffector;

		public Vector3 pUrsprungAberNichtDasBuch;
		public Vector3 pPos, pUp, pRight;
		public Quaternion pRot, pRot2;

		public ControlGroup(IServo servo, Vessel v)
			: this(servo)
		{
			vessel = v;
		}

		public ControlGroup(IServo servo)
			: this()
		{
			Name = servo.GroupName;
			ForwardKey = servo.Motor.ForwardKey;
			ReverseKey = servo.Motor.ReverseKey;
			groupSpeedFactor = 1;

			servos.Add(servo);
			servosState.Add(servo, new IServoState());
		}

		public ControlGroup()
		{
			servos = new List<IServo>();
			servosState = new Dictionary<IServo,IServoState>();

			Expanded = false;
			Name = "New Group";
			ForwardKey = string.Empty;
			ReverseKey = string.Empty;
			GroupSpeedFactor = 1;
			MovingNegative = false;
			MovingPositive = false;
			ButtonDown = false;
			bDirty = true;
		}

		public void ResetTargetPosition()
		{
			pPos = Vector3.zero;
			pUp = pEndEffector.transform.up;
			pRight = pEndEffector.transform.right;

			pRot = pEndEffector.transform.rotation;
			pRot2 = Quaternion.identity;
		}

		public bool SetActive(bool bActive)
		{
			if(bActive)
			{
				if(pActiveGroup == null)
				{
					pActiveGroup = this;
					ResetTargetPosition();
				}

				return pActiveGroup == this;
			}
			else
			{
				if(pActiveGroup == this)
					pActiveGroup = null;

				return true;
			}
		}

		public static ControlGroup pSetEndEffectorGroup = null;	// dieser Gruppe setzen wir gerade den EndEffector

		public void StartSelectEndEffector()
		{
			if(pSetEndEffectorGroup == null)
				pSetEndEffectorGroup = this;
			else if(pSetEndEffectorGroup == this)
				pSetEndEffectorGroup = null;
		}

		public void EndSetEndEffector(Part p)
		{
			pSetEndEffectorGroup = null;
			SetEndEffector(p);
			pUrsprungAberNichtDasBuch = p.transform.position;
		}

		public void SetEndEffector(Part p)
		{
			pEndEffector = p;

			ResetTargetPosition();
		}

		public bool ButtonDown { get; set; }

		public bool Expanded { get; set; }

		private string name = "New Group";
		public string Name 
		{ 
			get { return this.name; } 
			set { 
				this.name = value;
				if(this.servos != null && this.servos.Count > 0)
					this.servos.ForEach(s => s.GroupName = this.name);
			} 
		}

		public bool MovingNegative { get; set; }

		public bool MovingPositive { get; set; }

		public IList<IServo> Servos
		{
			get { return servos; }
		}

		public Vessel Vessel
		{
			get { return vessel; }
		}

		public void MurksBugFixVessel(Vessel v)
		{
			vessel = v;
		}

		public string ForwardKey
		{
			get { return forwardKey; }
			set
			{
				forwardKey = value;
				PropogateForward();
			}
		}

		public string ReverseKey
		{
			get { return reverseKey; }
			set
			{
				reverseKey = value;
				PropogateReverse();
			}
		}

		public float GroupSpeedFactor
		{
			get { return groupSpeedFactor; }
			set
			{
				groupSpeedFactor = value;
				PropogateGroupSpeedFactor();
			}
		}

		public float TotalElectricChargeRequirement
		{
			get
			{
				if(bDirty) Freshen();
				return totalElectricChargeRequirement;
			}
		}

		public void AddControl(IServo control, int index)
		{
			servos.Insert(index < 0 ? servos.Count : index, control);
			control.GroupName = Name;
			control.Motor.ForwardKey = ForwardKey;
			control.Motor.ReverseKey = ReverseKey;
			servosState.Add(control, new IServoState());
			bDirty = true;
		}

		public void RemoveControl(IServo control)
		{
			servos.Remove(control);
			servosState.Remove(control);
			bDirty = true;
		}

		public void MoveRight()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.MoveRight();
			}
		}

		public void MoveLeft()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.MoveLeft();
			}
		}

		public void MoveCenter()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.MoveCenter();
			}
		}

		public void MoveNextPreset()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Presets.MoveNext();
			}
		}

		public void MovePrevPreset()
		{
			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Presets.MovePrev();
			}
		}

		public void Stop()
		{
			MovingNegative = false;
			MovingPositive = false;

			if(Servos.Any())
			{
				foreach(var servo in Servos)
					servo.Motor.Stop();
			}
		}

		private void Freshen()
		{
			if(Servos == null) return;

			if(UseElectricCharge)
			{
				float chargeRequired = Servos.Where (s => s.IsFreeMoving == false).Sum (s => s.ElectricChargeRequired);
	//			foreach(var servo in Servos)
	//				servo.ElectricChargeRequired = chargeRequired;		// why set?
				totalElectricChargeRequirement = chargeRequired;
			}

			bDirty = false;
		}

		private void PropogateForward()
		{
			if(Servos == null) return;

			foreach(var servo in Servos)
				servo.Motor.ForwardKey = ForwardKey;
		}

		private void PropogateReverse()
		{
			if(Servos == null) return;

			foreach(var servo in Servos)
				servo.Motor.ReverseKey = ReverseKey;
		}

		private void PropogateGroupSpeedFactor()
		{
			if(Servos == null) return;

			foreach(var servo in Servos)
				servo.GroupSpeedFactor = groupSpeedFactor;
		}

		public void RefreshKeys()
		{
			foreach(var servo in Servos)
			{
				servo.Motor.ReverseKey = ReverseKey;
				servo.Motor.ForwardKey = ForwardKey;
			}
		}

		public void Plot(int colorIdx, int type)
		{
			for(int i = 0; i < ikServosForward.Count; i++)
				((ModuleIRServo_v3)ikServosForward[i]).Plot(colorIdx, type);
		}

		void FindIKServos()
		{
			ikServosBackward = new List<ModuleIRServo_v3>();

			Part p = pActiveGroup.pEndEffector;

			while(p.parent)
			{
				p = p.parent;

				if(p.GetComponent<ModuleIRServo_v3>())
				{
					int i = 0;
					while((i < servos.Count) && (servos[i].HostPart != p)) ++i;

					if(i < servos.Count)
						ikServosBackward.Add(p.GetComponent<ModuleIRServo_v3>());
				}
			}

			// umgedrehte Reihenfolge -> macht das experimentieren einfacher... vielleicht :-)

			ikServosForward = new List<ModuleIRServo_v3>();

			for(int j = ikServosBackward.Count - 1; j >= 0; j--)
				ikServosForward.Add(ikServosBackward[j]);


// FEHLER, temp mal... ich hoffe das reicht dann so
			ikServosBackward[0].pointerPart = pActiveGroup.pEndEffector;

			for(int i = 1; i < ikServosBackward.Count; i++)
				ikServosBackward[i].pointerPart = ikServosBackward[i - 1].part;
		}

		/*
		 * Remarks: Position of servos change constantly, maybe Rotion and Target also, but you
		 * can see this effect very good on the Postion (on the launch pad)... algorithms must
		 * take into account, that this happens!
		*/

		public void berechneMal()
		{
			if(pEndEffector == null)
				return; // ja, weil... ich kann ja nicht auf nichts zeigen...

			if(this != pActiveGroup)
				return; // nun ja... wieso sollte ich im Moment überhaupt aufgerufen werden sonst? hä?

			if(ikServosBackward == null)
				FindIKServos();

			if(!Controller.bUserInput)
			{
				if(Controller.bMove)
					testMal();
				return; // ohne neuen UserInput tun wir gar nichts
			}

			Controller.bUserInput = false;

			IKSolver s = new IKSolver();

			s.Reset(ikServosBackward);

		//	Plot(8, 0);	// current position

			// FEHLER, nur mal um was zu sehen
			//	s.SolveMoveToTarget(this);
			//	Plot(6, 1);

			s.SolveBackward(this, false);
				// wenn die Variable fix bleibt und von nichts mehr beeinflusst wird, dann stimmt das hier
			Plot(6, 1);

		//	s.SolveForward(this, true);
				// produziert relativ miese Resultate...
		//	Plot(11, 2);

			s.SolveForward_b(this, true);
			Plot(11, 2);

			if(Controller.bMove)
				testMal();

/*
was ist falsch? ... das obere ist irgendwie falsch... nur wieso??? pUrsprungAberNichtDasBuch hier zeichnet doch
die ... gelben linien... das target ohne Drehung... oder? -> ist aber falsch... die wahrheit liegt
	irgendwo zwischen dem gezeichneten UnderwaterFog dem Ziel oder so? ... *hmm*
		for(int i = 0; i < group.ikServosForward.Count; i++)
		{
Quaternion q =
i == 0 ? Quaternion.identity :
	group.ikServosForward[i - 1].aStat[2].Rotation * Quaternion.Inverse(group.ikServosForward[i - 1].aStat[0].Rotation);
// 0 ist korrekt... mal sehen wie 1 ist -> 1 ist... wie 0 ... also, gar nicht gesetzt... -> logo, weil nicht restricted, dann ergäbe das keinen Sinn
// also bleibt jetzt 2 ... und die ist schlicht falsch... total... zwar... nein, offenbar ist da nur meine Drehung auch noch drin... ich müsste die vom vorherigen nehmen... *hmm*
// das probier ich echt gleich aus...
Vector3 tgttest = q * group.ikServosForward[i].aStat[0].Target;
group.ikServosForward[i].DrawRelative(2, group.ikServosForward[i].curStat.Position, tgttest);
		}*/
		
			/*	folgendes haben wir gelernt
				* 
				*	false false -> ohne Restriktion -> dann liegt das Zeug super, schon bei einer Iteration
				*	
				* 
				*	true true -> mit allen Restriktionen -> wenn nicht per Zufall irgend ein Gelenk richtig liegt,
				*	dann geht gar nichts... alles bleibt verklemmt, fast keine Bewegung im Spiel
				*	
				* 
				*	false true -> also, zuerst mal spekulativ, danach mit Restriktion arbeiten... das gibt
				*	teilweise bessere Resultate... aber sie sind im Grunde alle unbrauchbar... auch hier ist
				*	das Problem, dass die blöden Gelenke oft einfach nicht in der Richtigen Richtung zeigen
				*	
				* 
				* 
				*  dennoch ist eines aufgefallen... es gibt unter Umständen ein Gelenk, das richtig liegt...
				*  also probieren wir jetzt mal, ob wir eine Lösung bauen könnten, indem wir die Auswirkungen
				*  der Gelenke auf den EndEffector prüfen und nicht nur um das nächste Teil zu erreichen...
				*  
				*  mal sehen ob das was wird -> die End-Idee ist es zwar nicht, weil die eher drauf abzielt
				*  mit einem Gelenk das andere Gelenk so zu drehen, damit es eine optimale Lösung gibt... aber
				*  zuerst bau ich mir jetzt mal die andere Lösung... vielleicht lerne ich ja noch was
				* 
				*/
				
				// eine Korrektur machen... nur mal eine... mal sehen ob's damit besser wird
/*
				{
					Vector3 ShouldTarget = pActiveGroup.pUrsprungAberNichtDasBuch + pActiveGroup.pPos;
					Vector3 ReachedTarget = ((ModuleIRServo_v3)ikServos[0]).Position4_f + ((ModuleIRServo_v3)ikServos[0]).Target4_f;

					Vector3 ShouldToReached = ReachedTarget - ShouldTarget;

					int idx = 0; float angle = 0f; Quaternion rot3 = Quaternion.identity;

					Quaternion _rt3 = Quaternion.identity;
					for(int i = ikServos.Count - 1; i >= 0; i--)
					{
						Vector3 currentAxis = _rt3 * ((ModuleIRServo_v3)ikServos[i]).GetAxis();

						float currentAngle = Vector3.Angle(ShouldToReached, currentAxis);
						if(angle < currentAngle)
						{ idx = i; angle = currentAngle; rot3 = _rt3; }

						_rt3 *= ((ModuleIRServo_v3)ikServos[i]).Rotation4_f;
					}

					// das idx Teil so drehen, dass wir möglichst nahe kommen
					Vector3 turnAxis = rot3 * ((ModuleIRServo_v3)ikServos[idx]).GetAxis();
					Vector3 ShouldTargetPlane = Vector3.ProjectOnPlane(ShouldTarget, turnAxis);
					Vector3 ReachedTargetPlane = Vector3.ProjectOnPlane(ReachedTarget, turnAxis);

					Quaternion correctionRotation = Quaternion.FromToRotation(ReachedTargetPlane, ShouldTargetPlane);

					((ModuleIRServo_v3)ikServos[idx]).Rotation4_f *= correctionRotation;

					Vector3 newPosition4_f = ((ModuleIRServo_v3)ikServos[idx]).Position4_f;

					for(int i = idx; i >= 0; i--)
					{
						((ModuleIRServo_v3)ikServos[i]).Position4_f = newPosition4_f;
						((ModuleIRServo_v3)ikServos[i]).Target4_f = correctionRotation * ((ModuleIRServo_v3)ikServos[i]).Target4_f;

						newPosition4_f += ((ModuleIRServo_v3)ikServos[i]).Target4_f;
					}*/
//				}
//			}

/*	Problem: der, bei dem die Achse direkt auf sein Kind zeigt, der verdreht seinen Target-Pointer
	* am Schluss total in den Mist raus... wieso? der dürfte das gar nie drehen
	* */
		}

		public static float to180(float v)
		{
			while(v > 180f) v -= 360f;
			while(v < -180f) v += 360f;
			return v;
		}

		public void testMal()
		{
			Quaternion q = Quaternion.identity;

			int maxmax = 10;

			List<float> aDaneben = new List<float>();

			for(int i = 0; (i < maxmax) && (i < ikServosForward.Count); i++)
			{
// FEHLER, temp... ich zeige mal einfach auf das, wohin mein Target zielen soll... also das Ziel
ikServosForward[i].DrawRelative(0, ikServosForward[i].IsPosition, ikServosForward[i].curStat.Target);

ikServosForward[i].DrawRelative(2, ikServosForward[i].IsPosition, q * ikServosForward[i].IsTarget);
// FEHLER, ich will beides sehen...

ikServosForward[i].DrawRelative(7, ikServosForward[i].IsPosition, (q * ikServosForward[i].GetAxis()).normalized);


//erst auf die ebene projezieren, danach winkel rechnen... sonst geht's ned
// FEHLER, bin zwar nicht sicher, ob "AngleSigned" das schon machen sollte? ... echt nicht... mal klären
	Vector3 Target0 = q * ikServosForward[i].aStat[0].Target;
Target0 = q * ikServosForward[i].IsTarget;
// FEHLER, neue Idee...
	Vector3 Targetcur = ikServosForward[i].curStat.Target;
	Vector3 Axis = (q * ikServosForward[i].GetAxis()).normalized;

						if(ikServosForward[i].GetFuckingSwap())	// sonst sind die scheiss Winkel verdreht und somit rennt er in die falsche Richtung, der Idiot
							Axis = -Axis;

	Target0 = Vector3.ProjectOnPlane(Target0, Axis);
	Targetcur = Vector3.ProjectOnPlane(Targetcur, Axis);


ikServosForward[i].DrawRelative(8, ikServosForward[i].IsPosition, Target0.normalized);
ikServosForward[i].DrawRelative(9, ikServosForward[i].IsPosition, Targetcur.normalized);

	//					ikServosForward[i].Move(
						aDaneben.Add(
							to180(
				//			-1.0f * // FEHLER, weiss nicht wieso das verdreht ist... -> doch, jetzt weiss ich's... weil AngleSigned das falsch lieferte !! :-)
								ModuleIRServo_v3.AngleSigned(
									Target0,
									Targetcur,
									Axis))
									);
	//								, 0.5f);
							
						// FEHLER, riesen Gebastel... einfach mal so...

// FEHLER, evtl. hier jetzt ausgeben, was ich angepeilt habe... nur mal so als Idee...

						q = ikServosForward[i].curStat.Rotation * Quaternion.Inverse(ikServosForward[i].aStat[0].Rotation);
q = ikServosForward[i].curStat.Rotation * Quaternion.Inverse(ikServosForward[i].IsRotation);
// FEHLER, neue Idee... -> nein, doch wieder die alte... wir nehmen IMMER aStat 0 und ändern den NIE
			}

			for(int i = 0; (i < maxmax) && (i < ikServosForward.Count); i++)
			{
				ikServosForward[i].Move(aDaneben[i], 0.5f);
			}
		}
	}
}
