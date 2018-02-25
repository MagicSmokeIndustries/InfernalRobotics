using System;
using InfernalRobotics_v3.Control;
using InfernalRobotics_v3.Control.Servo;
using InfernalRobotics_v3.Module;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace InfernalRobotics_v3.Command
{
/*
ein forwardkinematics teil bauen... eine Klasse, der ich
wie bei der Liste oben angeben kann -> von hier bis hier...

* damit könnte man dann arbeiten... dann kann ich Zeug sagen wie -> gib das von hier bis hier und leg's jetzt in
* die Ebene... ... denn dann könnte man leichter damit arbeiten... also, bsp.
* ich drehe mich um damit das foreward-Teil in bessere position zu bekommen...
* 
* dazu muss einiges möglich werden... bsp. ich - a - b - servo - c - d - end -> ich muss sehen können,
* was eine ich-Drehung bringen würde, um mit dem servo danach das end weiter in die passende Position
* bringen zu können
*/

// das Teil hier ist etwas wie eine Gruppe von Servos,
// aber in einer Art, dass der erste der zu bewegende ist
// und zwar so, damit ein anderer richtig zu liegen kommt, damit
// ein bestimmtes Ziel erreicht werden kann mit dem EndEffector

/* also so -> erster - b - c - servo - d - e - end
	* -> die Idee ist, man will mit 'servo' das 'end' näher zum Ziel bringen... damit das aber geht,
	* muss man zuerst mit 'erster' das ganze Zeug irgendwie drehen (z.B. in eine Ebene legen)
	* ... um solche Operationen oder Analysen jetzt zu vereinfachen, brauchen wir eine Klasse, die das tut...
	* darum bau ich mir jetzt diese hier... kann voll Scheisse sein, aber... vielleicht lernen wir ja was
	* */

/*
class KinematicsGroup
{
public List<ModuleIRServo_v3> servos;
public int idx; // des 'servo'

Quaternion shouldRotation0;
Quaternion shouldRotationIdx;

// Abstand von der Ebene zeigen (Qualität vom shouldRotation0)
// Abstand vom Ende zeigen (Qualität vom shouldRotationIdx)

public KinematicsGroup()
{
	// FEHLER, im Moment muss alles manuell gesetzt werden
}

	// FEHLER, wir rechnen mit den _f Werten... evtl. eine Fkt. noch für die _r Werte bauen?
public void FindOptimum(Vector3 target) // also, 0 und idx drehen, damit wir so nahe wie möglich an target rankommen... verstanden? -> gut, probieren wir's
{
	for(int i = 0; i < servos.Count; i++)
		servos[i].AddStat();

	// die Idee -> mit 0 die Achse vom idx so drehen, damit wir rechtwinklig zur Ebene
	// 0, idx, target liegen... dann mit idx versuchen sich dem target zu nähern... also, drauf zu zeigen eigentlich

	// jetzt 'servo' in die richtige Ebene legen
	Vector3 axis = servos[0].lastStat.Rotation * servos[0].GetAxis();

	Vector3 shouldAxis = Vector3.Cross(
		servos[0].lastStat.Position - servos[idx].lastStat.Position,
		target - servos[idx].lastStat.Position);

	Quaternion shouldRotation = Quaternion.FromToRotation(axis.normalized, shouldAxis.normalized);

	// oben ist uneingeschränkt, jetzt machen wir's eingeschränkt
	// einschränken -> also auf die mögliche Drehebene runterprojezieren....

	Vector3 axis_Plane = Vector3.ProjectOnPlane(axis, servos[0].GetAxis());
	Vector3 shouldAxis_Plane = Vector3.ProjectOnPlane(shouldAxis, servos[0].GetAxis());

	shouldRotation0 = Quaternion.FromToRotation(axis_Plane.normalized, shouldAxis_Plane.normalized);

	// jetzt haben wir den Scheiss gedreht in die Ebene (oder, ich wüsste zumindest wie ich's drehen muss)...

	// jetzt versuchen wir, mit dem 'servo' auf das Target zu zeigen... ist vielleicht völlig idiotisch, aber ich mach das jetzt mal... als Test vielleicht

	servos[0].curStat.Rotation = shouldRotation0 * servos[0].lastStat.Rotation;
	servos[0].curStat.Target = shouldRotation0 * servos[0].lastStat.Target;

	for(int i = 1; i < servos.Count; i++)
	{
		servos[i].curStat.Position = servos[i - 1].curStat.Position + servos[i - 1].curStat.Target;

		servos[i].curStat.Rotation = shouldRotation0 * servos[i].lastStat.Rotation;
		servos[i].curStat.Target = shouldRotation0 * servos[i].lastStat.Target;
	}

	// jetzt den 'servo' drehen... und danach sehen wir, wer am nächsten kommt... gut, oder?

	Vector3 zeiger = servos[servos.Count - 1].curStat.Position + servos[servos.Count - 1].curStat.Target - servos[idx].curStat.Position;
		// da drauf zeigt das, was am 'servo' hängt... das Teil kann ich drehen... ok, angenommen wir hätten keine Einschränkungen :-) aber das ist mir jetzt mal egal

	Vector3 shouldZeiger = target - servos[idx].curStat.Position;

	Vector3 zeiger_Plane = Vector3.ProjectOnPlane(zeiger, servos[idx].GetAxis());
	Vector3 shouldZeiger_Plane = Vector3.ProjectOnPlane(shouldZeiger, servos[idx].GetAxis());

	shouldRotationIdx = Quaternion.FromToRotation(zeiger_Plane.normalized, shouldZeiger_Plane.normalized);

	// jetzt versuchen wir, mit dem 'servo' auf das Target zu zeigen... ist vielleicht völlig idiotisch, aber ich mach das jetzt mal... als Test vielleicht

	servos[idx].curStat.Rotation = shouldRotation0 * servos[idx].curStat.Rotation;
	servos[idx].curStat.Target = shouldRotation0 * servos[idx].curStat.Target;

	for(int i = idx + 1; i < servos.Count; i++)
	{
		servos[i].curStat.Position = servos[i - 1].curStat.Position + servos[i - 1].curStat.Target;

		servos[i].curStat.Rotation = shouldRotation0 * servos[i].curStat.Rotation;
		servos[i].curStat.Target = shouldRotation0 * servos[i].curStat.Target;
	}
}
};
*/
	class IKSolver
	{
	// Reset, Vorbereitung...

		public void ResetPositions(ModuleIRServo_v3 servo)
		{
			servo.aStat.Clear();

			servo.lastStat = null;
			servo.curStat = new ModuleIRServo_v3.Stat();

			servo.curStat.bRestricted = true;
			servo.curStat.Position = servo.IsPosition;
			servo.curStat.Rotation = servo.IsRotation;
			servo.curStat.Target = servo.IsTarget;

			servo.aStat.Add(servo.curStat);
		}

		public void Reset(List<ModuleIRServo_v3> servos)
		{
			for(int i = 0; i < servos.Count; i++)
				ResetPositions(servos[i]);
		}

	// Funktionen

		public void Restrict(ModuleIRServo_v3 servoparent, ModuleIRServo_v3 servo, int statidx)
		{
			Quaternion q = Quaternion.identity;

			if(servoparent != null)
			{
				int idx = statidx;

				while(!servoparent.aStat[idx].bRestricted) --idx;

				q = servoparent.aStat[idx].Rotation * Quaternion.Inverse(servoparent.aStat[0].Rotation);
			}

			// ok, q ist also die Rotation, welche der Parent auf mich ausübt... verdammt noch eins!!!

			Vector3 TargetNurDurchDenParentGedrehtUndNormalized =
				(q * servo.aStat[0].Target).normalized;

			Vector3 TargetWohinEsEigentlichGehenSollUndNormalized =
				servo.aStat[statidx].Target.normalized;

			Vector3 AxisDurchDenParentGedreht =
				q * servo.GetAxis();

			// jetzt lege ich die alvektoren in die Ebene der al-Achse

			Vector3 Target0 = Vector3.ProjectOnPlane(TargetNurDurchDenParentGedrehtUndNormalized, AxisDurchDenParentGedreht).normalized;

			Vector3 Target1 = Vector3.ProjectOnPlane(TargetWohinEsEigentlichGehenSollUndNormalized, AxisDurchDenParentGedreht).normalized;

			// die Drehung jetzt holen

			Quaternion diealDrehung = Quaternion.FromToRotation(Target0, Target1);

			// jetzt drehen wir den Scheissdreck

			servo.aStat[statidx].Rotation = diealDrehung * (q * servo.aStat[0].Rotation);
			servo.aStat[statidx].Target = diealDrehung * (q * servo.aStat[0].Target);

			servo.aStat[statidx].bRestricted = true;

			// die Scheiss Position muss man anders berechnen
		}

		private void CalculateShouldPositionBackward(ModuleIRServo_v3 servo, Vector3 target, bool restrict)
		{
			// reverse ausrechnen

			servo.curStat.Target = (target - servo.lastStat.Position).normalized * servo.IsTarget.magnitude;
				// hier nicht restricted nehmen... das wollen wir ja explizit nicht

			if(restrict)
			{
	// FEHLER, auch per fkt machen später
				// einschränken

				Quaternion turn2 =  servo.lastRestrictedStat().Rotation * Quaternion.Inverse(servo.IsRotation); // direkt Axix berechnen
				Vector3 turnAxis = turn2 * servo.GetAxis();

				Vector3 lastRestrictedTarget_Plane = Vector3.ProjectOnPlane(servo.lastRestrictedStat().Target, turnAxis);
				Vector3 shouldUnrestrictedTarget_Plane = Vector3.ProjectOnPlane(servo.curStat.Target, turnAxis);

				Quaternion rotation = Quaternion.FromToRotation(lastRestrictedTarget_Plane, shouldUnrestrictedTarget_Plane);

				servo.curStat.Rotation = rotation * servo.lastRestrictedStat().Rotation;
				servo.curStat.Target = rotation * servo.lastRestrictedStat().Target;


				servo.curStat.bRestricted = true;
			}

			servo.curStat.Position = target - servo.curStat.Target;
		}

		// jetzt drauf zeigen, aber nur wenn möglich -> das ist für vorwärts, das obere für rückwärts... -> evtl. blöd... mal sehen ob's einfacher ginge
		public void CalculateShouldPositionForward(ModuleIRServo_v3 servoparent, ModuleIRServo_v3 servo, Vector3 target, Quaternion turn, bool restrict) // target ist meine neue Position, turn ist meine neue Drehung
		{
			// forward ausrechnen

			servo.curStat.Position = target;

			servo.curStat.Target = ((servo.lastStat.Position + servo.lastStat.Target) - servo.curStat.Position).normalized * servo.IsTarget.magnitude;
				// hier nicht restricted nehmen... das wollen wir ja explizit nicht

			// einschränken
			if(restrict)
				Restrict(servoparent, servo, servo.aStat.Count - 1);
		}

// das hier soll einen restricted stat (den curstat nämlich) korrigieren... mal sehen ob's klappt
		public Quaternion CalculateShouldPositionForward_b(ModuleIRServo_v3 servo, Vector3 target, Vector3 servotarget, Quaternion turn)
		{
	//			Quaternion turn = servo.curStat.Rotation * Quaternion.Inverse(servo.IsRotation); // direkt Axix berechnen
			Vector3 turnAxis = turn * servo.GetAxis();

			Vector3 servoTarget_Plane = Vector3.ProjectOnPlane(servotarget, turnAxis);
			Vector3 shouldTarget_Plane = Vector3.ProjectOnPlane(target - servo.curStat.Position, turnAxis);

			return Quaternion.FromToRotation(servoTarget_Plane.normalized, shouldTarget_Plane.normalized);
		}

// Solver

		public void SolveMoveToTarget(ControlGroup group)
		{
			for(int i = 0; i < group.ikServosBackward.Count; i++)
				group.ikServosBackward[i].AddStat();

			Vector3 tgt = group.pUrsprungAberNichtDasBuch + group.pPos;

			for(int i = 0; i < group.ikServosBackward.Count; i++)
			{
				group.ikServosBackward[i].curStat.Position = tgt - group.ikServosBackward[i].lastStat.Target;
				group.ikServosBackward[i].curStat.Target = group.ikServosBackward[i].lastStat.Target;
				group.ikServosBackward[i].curStat.Rotation = group.ikServosBackward[i].lastStat.Rotation;

				tgt = group.ikServosBackward[i].curStat.Position;
			}
		}

		public void SolveBackward(ControlGroup group, bool bRestricted)
		{
			for(int i = 0; i < group.ikServosBackward.Count; i++)
				group.ikServosBackward[i].AddStat();

			// alles berechnen und anzeigen... mal... oder so

			Vector3 tgt = group.pUrsprungAberNichtDasBuch + group.pPos;

			for(int i = 0; i < group.ikServosBackward.Count; tgt = group.ikServosBackward[i++].curStat.Position)
			{
				CalculateShouldPositionBackward(group.ikServosBackward[i], tgt, bRestricted);
			}
		}

//ok, die Idee -> backward und forward rechnen können (beides restricted und nicht)
// und zwar für so viele Teils wie man will
// und: nur für ein Teil (die 0) wobei der hintere Teil als "Target-Pointer" angsehen wird für's Rechnen

// ---->>>>>>> weiter umbauen

	// nur einen rechnen -> das hintere Zeug als einen einzigen Teil betrachten
// FEHELR, diese Funktion wurde nie überarbeitet
		public void SolveBackwardFirst(ControlGroup group, bool bRestricted)
		{
			for(int i = 0; i < group.ikServosBackward.Count; i++)
				group.ikServosBackward[i].AddStat();

			// alles berechnen und anzeigen... mal... oder so

			Vector3 tgt;
			Quaternion tgtt;

			Quaternion qfactor = group.pEndEffector.transform.rotation * Quaternion.Inverse(group.pRot);
			qfactor = Quaternion.identity;
			tgt = //pActiveGroup.pEndEffector.transform.position
				group.pUrsprungAberNichtDasBuch
				+ qfactor * group.pPos;

			tgtt = Quaternion.identity;

			// gut, das da oben ist unser Target... jetzt rechne ich aber nichts, sondern nehme das Zeug direkt
			// zu einem Teil zusammen und richte es aus...

			float mag =
				(group.ikServosBackward[group.ikServosBackward.Count - 1].curStat.Position + group.ikServosBackward[group.ikServosBackward.Count - 1].curStat.Target
				- group.ikServosBackward[0].curStat.Position).magnitude;

			CalculateShouldPositionBackward(group.ikServosBackward[0], tgt, bRestricted/*, mag*/);
		}

		public void SolveForward(ControlGroup group, bool bRestricted)
		{
			for(int i = 0; i < group.ikServosForward.Count; i++)
				group.ikServosForward[i].AddStat();

			Vector3 _tgt2 = group.ikServosForward[0].lastStat.Position;
			Quaternion _turn = Quaternion.identity;

			for(int i = 0; i < group.ikServosForward.Count; i++)
			{
				CalculateShouldPositionForward(
					i > 0 ? group.ikServosForward[i - 1] : null,
					group.ikServosForward[i], _tgt2, _turn, bRestricted);

				_tgt2 = group.ikServosForward[i].curStat.Position + group.ikServosForward[i].curStat.Target;

					// _turn ist das kumulative turn, um das meine Parents gedreht haben seit dem lastRestrictedState...
		//		_turn = group.ikServosForward[i].curStat.Rotation * Quaternion.Inverse(group.ikServosForward[i].lastRestrictedStat.Rotation);

					// so ein Scheiss... ich will das totale BIS zum lastRestrictedState...
				_turn = group.ikServosForward[i].lastRestrictedStat().Rotation * Quaternion.Inverse(group.ikServosForward[i].aStat[0].Rotation);
			}
		}

// FEHLER, erste Idee -> wir lösen 0, dann 1 und 0 erneut mit Target vom 1, dann 2 und 1 erneut mit Target vom 2 und 0 erneut mit Target vom 0 ... und so weiter
// eine blödsinnigkeit ist wohl, dass wir vorwärts mit -- machen müssen und rückwärts mit ++ ... evtl. sollte ich einfach 2 Arrays bauen um das im Moment einfacher zu haben... kann das später optimieren... oder?

		public void SolveForward_b(ControlGroup group, bool bRestricted)
		{
			// ich will was prüfen
	/*
			Quaternion org = group.ikServosForward[0].IsRotation;
			Quaternion a = Quaternion.FromToRotation(Vector3.up, Vector2.right);
			Quaternion orggedreht = a * org;
			Quaternion aerrechnet = orggedreht * Quaternion.Inverse(org); -> stimmt !!!
			Quaternion aerrechnet2 = Quaternion.Inverse(org) * orggedreht;
	*/

			for(int i = 0; i < group.ikServosForward.Count; i++)
				group.ikServosForward[i].AddStat();

			Vector3 _tgt2 = group.ikServosForward[0].aStat[0].Position;	// da woni bin... das mues 's 0 sii
			Quaternion _turn = Quaternion.identity;

			for(int i = 0; i < group.ikServosForward.Count; i++)
			{
// FEHLER, temp
//group.ikServosForward[i].DrawRelative(10, group.ikServosForward[i].lastRestrictedStat().Position,
//	_turn * group.ikServosForward[i].GetAxis());

				CalculateShouldPositionForward(
					i > 0 ? group.ikServosForward[i - 1] : null,
					group.ikServosForward[i], _tgt2, _turn, bRestricted);

				// ausrechnen wohin der Müll vom j zeigt... wenn man bis zum i alles dranhängt

				Vector3 servotarget = group.ikServosForward[i].curStat.Target; // logo, oder? ... ja, klar...

					// das, was wir hätten erreichen wollen...
				Vector3 _tgt2_goal = group.ikServosForward[i].lastStat.Position + group.ikServosForward[i].lastStat.Target;

//	if(false)		// FEHLER, aber die Drehung ist am Ende NICHT um die korrekte Achse... entweder durch die obere oder die untere Funktion... mal sehen was es ist -> ich schliesse daher mal die untere aus
				for(int j = i - 1; j >= 0; j--)
				{
			//	_tgt2 = noch das gleiche... logo, oder? ja gut...

					servotarget += group.ikServosForward[j].curStat.Target; // ist ja egal in welcher Reihenfolge wir Vektoren adieren

/*
// FEHLER, ich geb die zwei Zeiger jetzt mal aus... -> tgt2 zeigt auf zu früh... ah, weil verdreht... ja logisch irgendwie
Vector3 _tgt2_goal_relative = _tgt2_goal - group.ikServosForward[j].curStat.Position;
group.ikServosForward[j].DrawRelative(0, group.ikServosForward[j].transform.position, _tgt2_goal_relative);
group.ikServosForward[j].DrawRelative(1, group.ikServosForward[j].transform.position, servotarget);
*/
				Quaternion _innerturn = Quaternion.identity;
					
				if(j > 0)
					_innerturn = group.ikServosForward[j - 1].lastRestrictedStat().Rotation * Quaternion.Inverse(group.ikServosForward[j - 1].aStat[0].Rotation);
						// Hinweis: das ist eigentlich der curStat in unserem Fall

					Quaternion cor = CalculateShouldPositionForward_b(group.ikServosForward[j], _tgt2_goal, servotarget, _innerturn);
					// um das hier müsste ich noch zusätzlich drehen... also, mein Teil hier jetzt...
					// das beeinflusst dann alles gegen oben noch... *hmm*...

					servotarget = cor * servotarget; // logo, ich drehe ja den ganzen Müll, der da an mir dran hängt...

					group.ikServosForward[j].curStat.Rotation = cor * group.ikServosForward[j].curStat.Rotation;
					group.ikServosForward[j].curStat.Target = cor * group.ikServosForward[j].curStat.Target;

					for(int k = j + 1; k <= i; k++)
					{
						group.ikServosForward[k].curStat.Position = group.ikServosForward[k - 1].curStat.Position + group.ikServosForward[k - 1].curStat.Target;

						group.ikServosForward[k].curStat.Rotation = cor * group.ikServosForward[k].curStat.Rotation;
						group.ikServosForward[k].curStat.Target = cor * group.ikServosForward[k].curStat.Target;
					}
				}

				_tgt2 = group.ikServosForward[i].curStat.Position + group.ikServosForward[i].curStat.Target;

				// _turn ist das kumulative turn, um das meine Parents gedreht haben seit dem lastRestrictedState...
	//		_turn = group.ikServosForward[i].curStat.Rotation * Quaternion.Inverse(group.ikServosForward[i].lastRestrictedStat.Rotation);

				// so ein Scheiss... ich will das totale BIS zum lastRestrictedState...
				_turn = group.ikServosForward[i].lastRestrictedStat().Rotation * Quaternion.Inverse(group.ikServosForward[i].aStat[0].Rotation);
			}

/* -> das wohin ich zeigen würde, wenn ich nur jeweils die Parents vom servo drehen würde (sozusagen der Anfang von dem her der servo selber drehen muss)
		for(int i = 0; i < group.ikServosForward.Count; i++)
		{
Quaternion q =
i == 0 ? Quaternion.identity :
	group.ikServosForward[i - 1].curStat.Rotation * Quaternion.Inverse(group.ikServosForward[i - 1].aStat[0].Rotation);
// 0 ist korrekt... mal sehen wie 1 ist -> 1 ist... wie 0 ... also, gar nicht gesetzt... -> logo, weil nicht restricted, dann ergäbe das keinen Sinn
// also bleibt jetzt 2 ... und die ist schlicht falsch... total... zwar... nein, offenbar ist da nur meine Drehung auch noch drin... ich müsste die vom vorherigen nehmen... *hmm*
// das probier ich echt gleich aus...
Vector3 tgttest = q * group.ikServosForward[i].aStat[0].Target;
group.ikServosForward[i].DrawRelative(2, group.ikServosForward[i].curStat.Position, tgttest);
		}
*/
		}

//	versuchen einen parent zu finden, dessen rotationsachse meinem target entspricht
//		shouldrotation2 * getaxis muss nahe an shouldtraget2 sein... wenn -> dann um das geforderte drehen

				// FEHLER, ok, die Idee ist Schrott, ich bleib dabei... mal sehen was rauskommt...

//ich muss irgend sowas machen... echt
		/*		for(int j = i - 1; j >= 0; j--)
				{
					if(Vector3.Angle(
						((ModuleIRServo_v3)ikServos[i]).ShouldRotation2 * ((ModuleIRServo_v3)ikServos[i]).IsTarget,
						((ModuleIRServo_v3)ikServos[j]).ShouldTarget2) < 5f)
					{
						((ModuleIRServo_v3)ikServos[j]).ShouldRotation2 *=
							((ModuleIRServo_v3)ikServos[i]).qParentTurnAngleRequest;

						_rt2 = Quaternion.identity;

						for(int a = 0; a <= j; a++)
							_rt2 *= ((ModuleIRServo_v3)ikServos[a]).ShouldRotation2;

						break;
					}
				}*/
	};
}
