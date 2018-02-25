using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using TweakScale;

namespace InfernalRobotics.Module
{
	class MyRobotics : /*ModuleRotatingJoint*/ PartModule, IRescalable, IJointLockState
	{
		[KSPField(isPersistant = false)] public Vector3 axis = Vector3.forward;
		[KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
		[KSPField(isPersistant = false)] public string jointMesh = string.Empty;

		[KSPField(isPersistant = true)] public float position = 0.0f;
		float poscor = 0.0f; // correction der position, weil alles ein Scheiss ist
		[KSPField(isPersistant = true)] private Quaternion rotationConnectedBodyToJoint;

		[KSPField(isPersistant = true)] private bool swap = false;

		[KSPField(isPersistant = true)] private bool rev = false; // wenn swap änderte... -> wohl unnötig sich das zu merken

		private ConfigurableJoint Joint = null;

		private Transform NonMovingMeshTransform = null;
private Vector3 up_when_everything_was_null;

		private Quaternion rotationJointToNonMoving;
		private Quaternion rotationConnectedBodyToNonMoving;


		private LineDrawer[] arschloch = new LineDrawer[13];
		private Color[] arschlochColor = new Color[13];

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			arschloch[idx].DrawLineInGameView(Vector3.zero, p_vector, arschlochColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			arschloch[idx].DrawLineInGameView(p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformDirection(p_vector) : p_vector), arschlochColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }


		public MyRobotics()
		{
			for(int i = 0; i < 13; i++)
				arschloch[i] = new LineDrawer();

			arschlochColor[0] = Color.red;
			arschlochColor[1] = Color.green;
			arschlochColor[2] = Color.yellow;
			arschlochColor[3] = Color.magenta;	// axis
			arschlochColor[4] = Color.blue;		// secondaryAxis
			arschlochColor[5] = Color.white;
			arschlochColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			arschlochColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			arschlochColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			arschlochColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			arschlochColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
			arschlochColor[11] = new Color(209.0f / 255.0f, 247.0f / 255.0f, 74.0f / 255.0f);
			arschlochColor[12] = new Color(247.0f / 255.0f, 186.0f / 255.0f, 74.0f / 255.0f);
		}

		public override void OnAwake()
		{
			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);
		}

		public void OnVesselCreate(Vessel v)
		{
			if(part.vessel == v)
			{
				// weiss nicht, ob sich was ändert beim undocking? ... evtl. nicht... wobei, evtl. sollte
				// ich trotzdem das Zeug neu rechnen? zwar... wozu??
			}
		}

		public void OnVesselWasModified(Vessel v)
		{
			if(part.vessel == v)
			{

			if(Joint && part.attachJoint.Joint && Joint != part.attachJoint.Joint) // Joint.GetInstanceID() != part.attachJoint.Joint.GetInstanceID()) -> das != tut das da, oder?
			{
/* zweite Lösung... funktioniert auch, die Meshes sind zwar verdreht am Schluss und
 * beim Laden hab ich das gleiche Problem... das ist interessant...*/

		position = -position; // weil wir 'n Swap haben... also, mal als Versuch jetzt :-)

		Joint = part.attachJoint.Joint;

//		MiniLos(); // FEHLER, mal sowas von ein Test... -> ohne lief's eigentlich ganz gut
// -> MiniLos

/*			Quaternion rot = Quaternion.AngleAxis(-position, axis);
			Joint.transform.rotation *= rot;
			part.UpdateOrgPosAndRot(vessel.rootPart);

			part.attachJoint.DestroyJoint();
			part.CreateAttachJoint(vessel.rootPart.attachMode);
			part.ResetJoints();

		//	position = float.NaN;
		//	position = 0.0f; // weiss ned was das soll, nützt aber...
*/
			Initialize();

//			Joint.transform.rotation *= Quaternion.Inverse(rot);
//			part.UpdateOrgPosAndRot(vessel.rootPart);
// <- MiniLos


//arschloch10.DrawLineInGameView(Joint.transform.position, Joint.transform.position + Joint.transform.up, arschloch10color);
//arschloch11.DrawLineInGameView(Joint.transform.position, Joint.transform.position + Joint.transform.right, arschloch11color);
	
			// Anzeige machen
//			DrawAxis(10, Joint.transform, swap ? -Joint.transform.up : Joint.transform.up, false);
//			DrawAxis(11, Joint.transform,
//				Quaternion.AngleAxis(-Joint.lowAngularXLimit.limit, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
//			DrawAxis(12, Joint.transform,
//				Quaternion.AngleAxis(-Joint.highAngularXLimit.limit, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);


		return; // eigentlich müsste schon alles gesetzt sein durch das Initialize da drin





	//		if(NonMovingMeshTransform)
	//			NonMovingMeshTransform.rotation = part.attachJoint.Joint.transform.rotation * rotationJointToNonMoving;





bool _swap = true; // ja, das stimmt nicht, aber nur so sind die Winkel korrekt gezeichnet

	Quaternion dreh = Quaternion.AngleAxis(-position, Joint.transform.TransformDirection(Joint.axis));
dreh = Quaternion.identity;

//arschloch10.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right + dreh * Joint.transform.up, arschloch10color);
/*
arschloch11.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right
	+ Quaternion.AngleAxis(_swap ? -80.0f : -10.0f, Joint.transform.TransformDirection(axis)) * (dreh * Joint.transform.up), arschloch11color);
arschloch12.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right
	+ Quaternion.AngleAxis(_swap ? 10.0f : 80.0f, Joint.transform.TransformDirection(axis)) * (dreh * Joint.transform.up), arschloch12color);
*/
			Joint = part.attachJoint.Joint;

	Quaternion rrot = Quaternion.AngleAxis(180, Vector3.right); // weil, das wo der Joint vor dem Reconnect hinzeigt echt zum up gemacht wird... voll bescheuert... oberbescheuert... aber so is es... und zum Glück kommt's auch so vom Load zurück...
rrot = Quaternion.AngleAxis(position, axis);

// FEHLER, mal ein Versuch
	//		Joint.secondaryAxis = Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
	//								? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);

// FEHLER, noch ein Versuch
/*	Quaternion testrot = Quaternion.AngleAxis(270, axis);
	Joint.transform.rotation *= testrot;

			Joint.axis = axis;
			Joint.secondaryAxis = Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
									? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);

	Joint.transform.rotation *= Quaternion.Inverse(testrot);
*/
	// Resultat der Versuche -> die secondaryAxis liegt am Ende nie da, wo ich sie haben will


	Joint.transform.rotation *= rrot;

_swap = false;

//arschloch7.DrawLineInGameView(Joint.transform.position + 2 * Joint.transform.right, Joint.transform.position + 2 * Joint.transform.right + Joint.transform.up, arschloch7color);
/*
arschloch8.DrawLineInGameView(Joint.transform.position + 2 * Joint.transform.right, Joint.transform.position + 2 * Joint.transform.right
	+ Quaternion.AngleAxis(_swap ? -80.0f : -10.0f, Joint.transform.TransformDirection(axis)) * Joint.transform.up, arschloch8color);
arschloch9.DrawLineInGameView(Joint.transform.position + 2 * Joint.transform.right, Joint.transform.position + 2 * Joint.transform.right
	+ Quaternion.AngleAxis(_swap ? 10.0f : 80.0f, Joint.transform.TransformDirection(axis)) * Joint.transform.up, arschloch9color);
*/

			// set anchor
			Joint.anchor = Vector3.zero; // meistens -> wobei man das echt angeben können müsste - FEHLER

			// correct connectedAnchor
			Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(Joint.transform.TransformPoint(Joint.anchor));


//arschloch10.DrawLineInGameView(Joint.transform.position, Joint.transform.position + Joint.transform.up, arschloch10color);
//arschloch11.DrawLineInGameView(Joint.transform.position, Joint.transform.position + Joint.transform.right, arschloch11color);


			Joint.axis = axis;
Quaternion srot = Quaternion.AngleAxis(-position, Joint.axis);
			Joint.secondaryAxis = srot * (Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
									? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis));

	// ich kann den "up" scheinbar nicht verändern... -> muss man sich merken für Positionsbestimmungen... darum versuche ich die secondaryAxis so zu setzen, dass ich was aussagen kann
//arschloch8.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right + Joint.transform.up, arschloch8color);
//arschloch9.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right + Joint.transform.right, arschloch9color);


		Joint.transform.rotation *= Quaternion.Inverse(rrot);


				// gut... jetzt sollten zwar die Achsen anders sein, aber sonst sollte alles gleich bleiben

			Joint.xMotion = ConfigurableJointMotion.Locked;
			Joint.yMotion = ConfigurableJointMotion.Locked;
			Joint.zMotion = ConfigurableJointMotion.Locked;
			Joint.angularXMotion = ConfigurableJointMotion.Limited;
			Joint.angularYMotion = ConfigurableJointMotion.Locked;
			Joint.angularZMotion = ConfigurableJointMotion.Locked;

			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
			Joint.angularXDrive = new JointDrive
			{
				maximumForce = 0.0f, //0.2
				positionSpring = 0.0f,
				positionDamper = 20.0f // 0.5 weil ich nicht ewiges Schwingen will beim Test
			};

_swap = !_swap;
			SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = _swap ? -80.0f : -10.0f };
			SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = _swap ? 10.0f : 80.0f };

			Joint.lowAngularXLimit = lowAngularXLimit;
			Joint.highAngularXLimit = highAngularXLimit;
			Joint.lowAngularXLimit = lowAngularXLimit;

	// Meshes total falsch... ca. 180°-position oder 180°+position falsch (wenn position negativ ist) ...		

			// detect attachment mode
			bool newswap = (part.attachJoint.Target != part.attachNodes[1].attachedPart);
			swap = newswap;

			// find non rotating mesh
			NonMovingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? jointMesh : fixedMesh);

			// set axis
//			Quaternion oldRotation = Joint.transform.rotation;
	//		Joint.transform.rotation *= Quaternion.Inverse(Quaternion.AngleAxis(position, axis));
//Joint.transform.rotation *= Quaternion.Inverse(Quaternion.AngleAxis(position, axis));


			rotationJointToNonMoving = Quaternion.Inverse(Joint.transform.rotation) * NonMovingMeshTransform.rotation;
			rotationConnectedBodyToNonMoving = Quaternion.Inverse(Joint.connectedBody.transform.rotation) * NonMovingMeshTransform.rotation;



return;


//	_oldpos = _position; // FEHLER, nicht immer -, nur wenn swap ändert

//				_position = float.NaN;

/*			Quaternion reference = Quaternion.LookRotation(
				Joint.connectedBody.transform.position - Joint.transform.position,
				Vector3.Cross(
					Joint.transform.TransformDirection(axis),
					Joint.connectedBody.transform.position - Joint.transform.position));
	
				Quaternion myRel2 = rel2;

// old ref
				Quaternion oldRot = Joint.transform.rotation;
				Joint.transform.rotation = oldRef * myRel2;
*/


	//			Initialize(); // FEHLER, falsch, aber ich will mal sehen was sich dann so tut
			//	hier lande ich nämlich nach docking


// FEHLER, dieser Fall geht von Verdrehung des Dockings aus (swap != newswap) ... mal als Test
				float position2 = position;

			// revert non moving mesh -> sonst wird später 'position' falsch berechnet

			if(NonMovingMeshTransform)
			{
				NonMovingMeshTransform.rotation = part.attachJoint.Joint.transform.rotation * rotationJointToNonMoving;
//				NonMovingMeshTransform.rotation = Joint.transform.rotation * rotationJointToNonMoving; -> FEHLER, war wohl falsch so, ich muss das jetzt auf den neuen Joint setzen

//Quaternion rrr = Quaternion.Inverse(Quaternion.AngleAxis((0 + position2)/* % 360 */, axis)); // FEHLER, wieso invers? -> weil wir wohl secondaryAxis mit right und nicht -right bauen?
				// FEHLER, + 180 ist wegen swap -> ja, das ist aber sinnlos, weil wir ja die Limits dem anderen Teil übergeben... -> darum wieder zurück auf +0

//NonMovingMeshTransform.rotation *= rrr; // FEHLER, ich bin am spielen

			}

//Quaternion NonMovingRot = NonMovingMeshTransform.rotation;

			Joint = part.attachJoint.Joint;


	// FEHLER, blöd, ist fix im Moment das swap
NonMovingMeshTransform = KSPUtil.FindInPartModel(transform, /*_swap*/ true ? jointMesh : fixedMesh);

// FEHLER, neuer Versuch das Teil gerade zu rücken
//NonMovingMeshTransform.rotation = Joint.transform.rotation * rotationJointToNonMoving;


			// set anchor
			Joint.anchor = Vector3.zero; // meistens -> wobei man das echt angeben können müsste - FEHLER

			// correct connectedAnchor
			Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(Joint.transform.TransformPoint(Joint.anchor));



//weil ich die setze, muss ich natürlich erst hochdrehen...
Quaternion revRotation = Quaternion.Inverse(Quaternion.AngleAxis(position2, axis));

Joint.transform.rotation *= revRotation;

			Joint.axis = axis;
			Joint.secondaryAxis = Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
									? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);

Joint.transform.rotation *= Quaternion.Inverse(revRotation);


/*
Quaternion revRotation = Quaternion.Inverse(Quaternion.AngleAxis(position2, Joint.transform.TransformDirection(axis)));

arschloch8.DrawLineInGameView(Joint.transform.position, Joint.transform.position + revRotation * Joint.transform.up, arschloch8color);
*/		// das Zeug zeigt richtig -> das nur so als Info mal...


	//		Quaternion oldRotation = Joint.transform.rotation;
	//		Joint.transform.rotation *= Quaternion.Inverse(Quaternion.AngleAxis(position, axis));

//Quaternion revRotation2 = Quaternion.Inverse(Quaternion.AngleAxis((0 + position2)/* % 360 */, axis)); // FEHLER, wieso invers? -> weil wir wohl secondaryAxis mit right und nicht -right bauen?
				// FEHLER, + 180 ist wegen swap -> ja, das ist aber sinnlos, weil wir ja die Limits dem anderen Teil übergeben... -> darum wieder zurück auf +0
//					ja, die 180 wären schon richtig gewesen... das bringt aber alles andere
//						durcheinander...

//Joint.transform.rotation *= Quaternion.Inverse(revRotation2);
	// FEHLER, ich denke, ich sollte in die andere Richtung drehen oder gar nicht? ... ist der Joint irgendwie jetzt vermurkst, wenn er so umgedockt wird?

/*	bool d = true;
	if(d)
			Joint.axis = axis;
	if(d)
			Joint.secondaryAxis = Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
									? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);

_swap = true;
				*/

/*
arschloch10.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right + Joint.transform.TransformDirection(Joint.secondaryAxis), arschloch10color);

arschloch11.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right
	+ Quaternion.AngleAxis(_swap ? -80.0f : -10.0f, Joint.transform.TransformDirection(axis)) * Joint.transform.TransformDirection(Joint.secondaryAxis), arschloch11color);
arschloch12.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right
	+ Quaternion.AngleAxis(_swap ? 10.0f : 80.0f, Joint.transform.TransformDirection(axis)) * Joint.transform.TransformDirection(Joint.secondaryAxis), arschloch12color);
*/


//arschloch9.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right + Joint.transform.up, arschloch9color); // altes down sozusagen... -> umgedrehtes arschloch8 also

// NonMovingMeshTransform.rotation = NonMovingRot; FEHLER, das hier statt dem inverse revRotation2 ? weiss nicht ob's ginge
//		rotationConnectedBodyToNonMoving = Quaternion.Inverse(ConnectedBody.transform.rotation) * NonMovingMeshTransform.rotation;
			// ja, das stimmt natürlich nicht, weil sich beide Teils mitbewegen dabei da... -> es sei denn, ich würde erst jetzt zurückdrehen auf's orginal
//		rotationConnectedBodyToNonMoving *= Quaternion.Inverse(revRotation2);
	//	rotationConnectedBodyToNonMoving *= Quaternion.Inverse(revRotation2);


//Quaternion revRotation2 = Quaternion.AngleAxis(-position2, Joint.transform.TransformDirection(axis));

//Joint.transform.rotation = oldRotation; -> das hier führ zur absoluten Katastrophe... weiss noch nicht genau wieso
//	Joint.transform.rotation *= revRotation2;

//Joint.transform.rotation *= revRotation2;

				// gut... jetzt sollten zwar die Achsen anders sein, aber sonst sollte alles gleich bleiben

			Joint.xMotion = ConfigurableJointMotion.Locked;
			Joint.yMotion = ConfigurableJointMotion.Locked;
			Joint.zMotion = ConfigurableJointMotion.Locked;
			Joint.angularXMotion = ConfigurableJointMotion.Limited;
			Joint.angularYMotion = ConfigurableJointMotion.Locked;
			Joint.angularZMotion = ConfigurableJointMotion.Locked;

			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
			Joint.angularXDrive = new JointDrive
			{
				maximumForce = 0.0f, //0.2
				positionSpring = 0.0f,
				positionDamper = 20.0f // 0.5 weil ich nicht ewiges Schwingen will beim Test
			};

	//		SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = _swap ? -80.0f : -10.0f };
	//		SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = _swap ? 10.0f : 80.0f };

			Joint.lowAngularXLimit = lowAngularXLimit;
			Joint.highAngularXLimit = highAngularXLimit;
			Joint.lowAngularXLimit = lowAngularXLimit;

//	_oldpos = float.NaN;

//				Joint.transform.rotation = oldRot;
			}

	//		if(Joint && crb && Joint.connectedBody.GetInstanceID() != crb.GetInstanceID())
	//		{
	//			crb = null;
	//		}

			}
		}

		public void OnDestroy()
		{
			GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
		}

		// Called when the flight starts, or when the part is created in the editor. 
		// OnStart will be called before OnUpdate or OnFixedUpdate are ever called.
		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			StartCoroutine(WaitAndInitialize()); // calling Initialize in OnStartFinished should work too, but KSP does it like this internally
		}

		public IEnumerator WaitAndInitialize()
		{
			while(!part.attachJoint || !part.attachJoint.Joint)
				yield return null;

			Initialize();
		}

		bool bVers = true;

		private void Initialize()
		{
//			DrawAxis(7, part.attachJoint.Joint.transform, part.attachJoint.Joint.axis, true, part.attachJoint.Joint.transform.right * 0.15f);
//			DrawAxis(8, part.attachJoint.Joint.transform, part.attachJoint.Joint.secondaryAxis, true, part.attachJoint.Joint.transform.right * 0.15f);


			Vector3 v1 = swap ? part.attachJoint.Joint.transform.up : -part.attachJoint.Joint.transform.up;
			Vector3 v2 = part.attachJoint.Joint.connectedBody.transform.up;

			poscor = AngleSigned(v1, v2, Vector3.Cross(v1, v2));

			if(bVers)
				poscor -= position;
			else
				poscor += position;


			// revert rotation
			if(float.IsNaN(position))
			{
				rotationConnectedBodyToJoint = Quaternion.Inverse(part.attachJoint.Joint.connectedBody.transform.rotation) * part.attachJoint.Joint.transform.rotation;
				position = 0.0f;
			}
	//		else
	//			part.attachJoint.Joint.transform.rotation = Joint.connectedBody.transform.rotation * rotationConnectedBodyToJoint;
					// der NEUE Joint soll so orientiert sein, wie der ALTE es mal WAR!

			// revert non moving mesh
			if(NonMovingMeshTransform)
				NonMovingMeshTransform.rotation = Joint.transform.rotation * rotationJointToNonMoving;

// jetzt setzen wir die SCHEISS JOINTS auf die NEUEN SCHEISS JOINTS
			Joint = part.attachJoint.Joint;




bool _swap = true; // ja, das stimmt nicht, aber nur so sind die Winkel korrekt gezeichnet

	Quaternion dreh = Quaternion.AngleAxis(-position, Joint.transform.TransformDirection(Joint.axis));

//arschloch10.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right + dreh * Joint.transform.up, arschloch10color);
//arschloch11.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right
//	+ Quaternion.AngleAxis(_swap ? -80.0f : -10.0f, Joint.transform.TransformDirection(axis)) * (dreh * Joint.transform.up), arschloch11color);
//arschloch12.DrawLineInGameView(Joint.transform.position + Joint.transform.right, Joint.transform.position + Joint.transform.right
//	+ Quaternion.AngleAxis(_swap ? 10.0f : 80.0f, Joint.transform.TransformDirection(axis)) * (dreh * Joint.transform.up), arschloch12color);


			// revert everything -> hab ich schon getan... ist neu... mal sehen halt
//			if(NonMovingMeshTransform)
//				Joint.transform.rotation *= Quaternion.Inverse(Quaternion.AngleAxis(position, axis));

			// set anchor
			Joint.anchor = Vector3.zero; // meistens -> wobei man das echt angeben können müsste - FEHLER

			// correct connectedAnchor
			Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(Joint.transform.TransformPoint(Joint.anchor));

			// build reference rotation
//			Quaternion _reference = Quaternion.LookRotation(
//				Joint.connectedBody.transform.position - Joint.transform.position,
//				Vector3.Cross(
//					Joint.transform.TransformDirection(axis),
//					Joint.connectedBody.transform.position - Joint.transform.position));

			// detect attachment mode
			bool newswap = (part.attachJoint.Target != part.attachNodes[1].attachedPart);

	//		if(newswap != swap)			// FEHLER, einfacher fix Versuch -> in dem Fall dreht sich die Achse um, oder nicht? -> müsste man prüfen -> also, die Achse zeigt rein statt raus... oder nicht?
	//			position = -position;
			swap = newswap;

			// find non rotating mesh
			NonMovingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? jointMesh : fixedMesh);

			// set axis
	//		Quaternion oldRotation = Joint.transform.rotation;
	//		Joint.transform.rotation *= Quaternion.Inverse(Quaternion.AngleAxis(position, axis));
//Joint.transform.rotation *= Quaternion.Inverse(Quaternion.AngleAxis(position, axis));


			rotationJointToNonMoving = Quaternion.Inverse(Joint.transform.rotation) * NonMovingMeshTransform.rotation;
			rotationConnectedBodyToNonMoving = Quaternion.Inverse(Joint.connectedBody.transform.rotation) * NonMovingMeshTransform.rotation;


		//	rotationJointToNonMoving *= Quaternion.AngleAxis(position, Joint.transform.TransformVector(axis));

//rotationNonMovingToConnectedBody *= Quaternion.AngleAxis(position, /*Joint.transform.TransformVector*/(axis)); // non-moving zu connected ist natürlich nicht das aktuelle, sondern + dem position, weil das ja schon im Ausgangszustand drin war und darum hier fehlt (also: (start + rot) -> connected => umrechnen auf => start -> connected geht mit um rot zusätzlich drehen
	// FEHLER, ist das hier ein Problem? bei Dock ja, bei nicht dock nein?

// FEHLER, ich probier mal was... evtl. hebt das das Docking Problem auf und verursacht kein neues bei Load
//Transform MovingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? fixedMesh : jointMesh);
//rotationNonMovingToConnectedBody *= Quaternion.Inverse(MovingMeshTransform.rotation) * NonMovingMeshTransform.rotation;

			// rotateAxis is given, secondaryAxis must be perpendicular, that's what we create here
			Joint.axis = axis;
			Joint.secondaryAxis = Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
									? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);

		//	Joint.transform.rotation = oldRotation;
//Joint.transform.rotation *= Quaternion.AngleAxis(position, axis);
//Joint.transform.rotation = JointRotation;
	// jetzt drehe ich den SCHEISS JOINT wieder zurück auf das was er war... keine Ahnung was hier nicht funktionieren sollte

// FEHLER, ist gleich wie unpack... zumindest Teile davon
			Initialize2();

			// Anzeige machen
//turn um 180 wenn swap ... oder spiegeln?

	//		DrawAxis(7, Joint.transform, swap ? -Joint.transform.up : Joint.transform.up, false);
	//		DrawAxis(8, Joint.transform,
	//			Quaternion.AngleAxis(-Joint.lowAngularXLimit.limit, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
	//		DrawAxis(9, Joint.transform,
	//			Quaternion.AngleAxis(-Joint.highAngularXLimit.limit, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);

	
			AttachNode nodeToParent = part.FindAttachNodeByPart(part.parent);
			AttachNode nodeFromParent = part.parent.FindAttachNodeByPart(part);

			AttachNode attachNode;
			Transform partTransform;
			if(nodeToParent.size <= nodeFromParent.size)
			{
				attachNode = nodeToParent;
				partTransform = part.partTransform;
			}
			else
			{
				attachNode = nodeFromParent;
				partTransform = part.parent.partTransform;
			}

		Vector3 nodeOrt = attachNode.orientation;
		Vector3 nodeOrt2 = attachNode.secondaryAxis;

		if (nodeOrt2.IsZero())
		{
			Vector3.OrthoNormalize(ref nodeOrt, ref nodeOrt2);
		}

	//		attachNode.size // ACHTUNG: muss < 2 sein... sonst... müsste ich 'n anderen Fall programmieren...

		nodeOrt = part.attachJoint.Child.partTransform.InverseTransformDirection(partTransform.TransformDirection(nodeOrt));
		nodeOrt2 = part.attachJoint.Child.partTransform.InverseTransformDirection(partTransform.TransformDirection(nodeOrt2));


//			DrawAxis(11, Joint.transform, nodeOrt, true, Joint.transform.right * 0.3f);
//			DrawAxis(12, Joint.transform, nodeOrt2, true, Joint.transform.right * 0.3f);
	// das scheinen die Achsen... jetzt mal noch das min/max einzeichnen
		}

		public void Initialize2() // FEHLER, temp, Zeug was ich rausziehe, weil ich da sicher bin, dass ich's immer wieder brauch und sich NIE was ändert in KEINEM Fall
		{
			Joint.xMotion = ConfigurableJointMotion.Locked;
			Joint.yMotion = ConfigurableJointMotion.Locked;
			Joint.zMotion = ConfigurableJointMotion.Locked;
			Joint.angularXMotion = ConfigurableJointMotion.Limited;
			Joint.angularYMotion = ConfigurableJointMotion.Locked;
			Joint.angularZMotion = ConfigurableJointMotion.Locked;

			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
			Joint.angularXDrive = new JointDrive
			{
				maximumForce = 0.0f, // 0.2
				positionSpring = 0.0f,
				positionDamper = 20.0f // 0.5 weil ich nicht ewiges Schwingen will beim Test
			};

			SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = (swap ? -80.0f : -10.0f) + poscor };
			SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = (swap ? 10.0f : 80.0f) + poscor };

			Joint.lowAngularXLimit = lowAngularXLimit;
			Joint.highAngularXLimit = highAngularXLimit;
			Joint.lowAngularXLimit = lowAngularXLimit;

			Joint.enableCollision = false;
			Joint.enablePreprocessing = false;

			Joint.projectionMode = JointProjectionMode.None;
		}

		public override void OnSave(ConfigNode node)
		{
			Logger.Log("[OnSave] Start", Logger.Level.Debug);
			base.OnSave(node);
	//		myrot = part.transform.rotation; // FEHLER, ein Versuch
	//		myrot_valid = true;
			Logger.Log("[OnSave] End", Logger.Level.Debug);
		}

		public override void OnLoad(ConfigNode config)
		{
			Logger.Log("[OnLoad] Start", Logger.Level.Debug);

			base.OnLoad(config);
	//		if(myrot_valid)
	//			part.transform.rotation = myrot;

/*			//save persistent rotation/translation data, because the joint will be initialized at current position.
			moveDelta = movePos;

			Initialize();*/

			Logger.Log("[OnLoad] End", Logger.Level.Debug);
		}


		public void OnVesselGoOnRails(Vessel v)
		{}

		public void OnVesselGoOffRails(Vessel v)
		{
			// set all parameters that were reset
			if(part.vessel == v)
			{
				/*
				 -> restore values
				 xDrive, yDrive, zDrive, targetPosition
				 rotationDriveMode
				 angularXDrive, angularYZDrive
				 highAngularXLimit, lowAngularXLimit
				 angulaYLimit, angularZLimit
				 angularXLimitSpring, angularYZLimitSpring
				 linearLimit, linearLimitSpring
				 */

				Joint.xMotion = ConfigurableJointMotion.Locked;
				Joint.yMotion = ConfigurableJointMotion.Locked;
				Joint.zMotion = ConfigurableJointMotion.Locked;
				Joint.angularXMotion = ConfigurableJointMotion.Limited;
				Joint.angularYMotion = ConfigurableJointMotion.Locked;
				Joint.angularZMotion = ConfigurableJointMotion.Locked;

				Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
				Joint.angularXDrive = new JointDrive
				{
					maximumForce = 0.0f, //0.2
					positionSpring = 0.0f,
					positionDamper = 0.0f // 20 weil ich nicht ewiges Schwingen will beim Test
				};

				SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = swap ? -80.0f : -10.0f };
				SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = swap ? 10.0f : 80.0f };

				Joint.lowAngularXLimit = lowAngularXLimit;
				Joint.highAngularXLimit = highAngularXLimit;
				Joint.lowAngularXLimit = lowAngularXLimit;

				Joint.enableCollision = false;
				Joint.enablePreprocessing = false;

				Joint.projectionMode = JointProjectionMode.None;
			}
		}



		public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
		{
			return Mathf.Atan2(
				Vector3.Dot(n.normalized, Vector3.Cross(v1.normalized, v2.normalized)),
				Vector3.Dot(v1.normalized, v2.normalized)) * Mathf.Rad2Deg;
		}

		public float to180(float v)
		{
			if(v > 180)	v -= 360;
			if(v <= -180)   v += 360;
			return v;
		}

		// set original rotation to current rotation (because limits will be reset in some cases what causes a jump back into the original rotation)
		public void UpdatePos()
		{
			part.UpdateOrgPosAndRot(part.vessel.rootPart);
			foreach(Part child in part.FindChildParts<Part>(true))
				child.UpdateOrgPosAndRot(vessel.rootPart);
		}

		public void FixedUpdate()
		{
	//		base.OnFixedUpdate();

	//		this.SetMotorForce(0.0f);
	//		this.SetMotorMode(Mode.Neutral);

	//		Joint = part.attachJoint.Joint;

	//		if(!part.attachJoint) return;
			if(!Joint)
				return; // kann das sein überhaupt???

			if(Joint.GetInstanceID() != part.attachJoint.Joint.GetInstanceID())
				return;

	//		if(crb.GetInstanceID() != Joint.connectedBody.GetInstanceID())
	//			return;


			if(Joint.angularXDrive.maximumForce > 0.1f)
			{
				UnityEngine.Debug.Log(string.Format("-> Scheisser hat angularXDrive verkackt -> {0}", Joint.angularXDrive.maximumForce));

			//	Initialize(); // dann halt nochmal... verdammt
			}
			else
				UnityEngine.Debug.Log("->-> ok");

//			Logger.Log(string.Format("Winkel {0}", to180(AngleSigned(Joint.secondaryAxis, Joint.transform.right, Joint.axis))),
//				Logger.Level.Info);

			if(HighLogic.LoadedSceneIsFlight)
			{
				Vector3 transup = swap ? -Joint.transform.up : Joint.transform.up;
	transup = Quaternion.AngleAxis(poscor, Joint.transform.TransformDirection(Joint.axis)) * transup;
				DrawAxis(0, Joint.transform, transup, false);
				DrawAxis(6, Joint.transform, Joint.connectedBody.transform.up, false);


//				arschloch0.DrawLineInGameView(Vector3.zero, part.Rigidbody.transform.position, Color.red);
		//		arschloch1.DrawLineInGameView(Vector3.zero, Joint.transform.position, Color.green);
				DrawPointer(1, Joint.transform.position);

		//		Vector3 pos = Joint.transform.TransformPoint(Joint.anchor);
		//		arschloch2.DrawLineInGameView(Vector3.zero, pos, Color.yellow);
				DrawPointer(2, Joint.transform.TransformPoint(Joint.anchor));

		//		arschloch3.DrawLineInGameView(pos, pos + Joint.transform.TransformDirection(Joint.axis), Color.magenta);
				DrawAxis(3, Joint.transform, Joint.axis, true);
		//		arschloch4.DrawLineInGameView(pos, pos + /*Quaternion.Inverse*/(rotationJointToNonMoving) * Joint.transform.TransformDirection(Joint.secondaryAxis), Color.blue);
				DrawAxis(4, Joint.transform, rotationJointToNonMoving* Joint.transform.TransformDirection(Joint.secondaryAxis), false);

		//		arschloch5.DrawLineInGameView(Vector3.zero, Joint.connectedBody.transform.TransformPoint(Joint.connectedAnchor), Color.white);
				DrawPointer(5, Joint.connectedBody.transform.TransformPoint(Joint.connectedAnchor));

	// die Anzeige war interessant... jetzt mal 'ne andere
		//		Transform f = KSPUtil.FindInPartModel(transform, "Base");
		//		if(f)
		//			arschloch6.DrawLineInGameView(f.position, f.position + f.forward, arschloch6color);
		//		Transform j = KSPUtil.FindInPartModel(transform, "Joint");
		//		if(j)
		//			arschloch7.DrawLineInGameView(j.position, j.position + j.forward, arschloch7color);

//				arschloch6.DrawLineInGameView(Joint.transform.position, Joint.transform.position + Joint.transform.up, arschloch6color);
//				arschloch7.DrawLineInGameView(Joint.transform.position, Joint.transform.position + Quaternion.Inverse(Quaternion.AngleAxis(_position, Joint.transform.TransformDirection(axis))) * Joint.transform.up, arschloch7color);
			}

			// we have to do an update, because OnSave fires whithout previous warning and doesn't let us do it right before
			UpdatePos();

	//		Mesh.rotation = Joint.connectedBody.transform.rotation * rel;
		NonMovingMeshTransform.rotation = Joint.connectedBody.transform.rotation * rotationConnectedBodyToNonMoving;

//NonMovingMeshTransform.rotation = Joint.transform.rotation * rotationJointToNonMoving;
	// FEHLER, ich will ihn immer in der 0 Position sehen um den Bug zu finden
	// -> funktioniert, die Rotation stimmt
		
		Vector3 _v1 = NonMovingMeshTransform.forward;
		Vector3 _v2 = transform.forward;
		Vector3 n = NonMovingMeshTransform.right;

		position = to180(AngleSigned(_v1, _v2, n));


			Vector3 v1 = swap ? part.attachJoint.Joint.transform.up : -part.attachJoint.Joint.transform.up;
			Vector3 v2 = part.attachJoint.Joint.connectedBody.transform.up;

			float myposcor = AngleSigned(v1, v2, Vector3.Cross(v1, v2));
			float myposcor2 = AngleSigned(v2, v1, Vector3.Cross(v2, v1));
// FEHLER, temp hier nach UpdatePos um mit position vergleichen zu können


			return;
		}

		////////////////////////////////////////
		// IRescalable

		public void OnRescale(ScalingFactor factor)
		{
		}

		////////////////////////////////////////
		// IJointLockState

		bool IJointLockState.IsJointUnlocked()
		{
// FEHLER nope, nur wenn ers ist
			return true;
		}

		////////////////////////////////////////
		// KSPEvents, KSPActions

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "-- los !!")]
		public void Los()
		{
			// mit allen Achsen frei -> ging... jetzt begrenze ich's mal auf nur x-Achse (was ich am Ende brauch)

			Joint.angularXMotion = ConfigurableJointMotion.Limited;
	//		Joint.angularYMotion = ConfigurableJointMotion.Free;
	//		Joint.angularZMotion = ConfigurableJointMotion.Free;

			JointDrive drv = new JointDrive();
	//		drv.mode = JointDriveMode.PositionAndVelocity; -> veraltet... gut, lassen wir's halt
			drv.maximumForce = 0.0f;
			drv.positionSpring = 0f;
			drv.positionDamper = 0.5f; // weil ich nicht ewiges Schwingen will beim Test

			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
			Joint.angularXDrive = drv;
	//		Joint.angularYZDrive = drv;

			SoftJointLimit sjl;
			
			sjl = new SoftJointLimit();
			sjl.limit = 30.0f;
			Joint.highAngularXLimit = sjl;

			sjl = new SoftJointLimit();
			sjl.limit = -40.0f;
			Joint.lowAngularXLimit = sjl;

			// es flippt aus... weiss nicht genau wieso
			part.attachJoint.Joint.enableCollision = false;
			Joint.projectionMode = JointProjectionMode.None;

			Joint.enablePreprocessing = false;

			Joint.enableCollision = false;



			Joint.xMotion = ConfigurableJointMotion.Locked;
			Joint.yMotion = ConfigurableJointMotion.Locked;
			Joint.zMotion = ConfigurableJointMotion.Locked;
			Joint.angularXMotion = ConfigurableJointMotion.Locked;
			Joint.angularYMotion = ConfigurableJointMotion.Locked;
			Joint.angularZMotion = ConfigurableJointMotion.Locked;

			Joint.angularXMotion = ConfigurableJointMotion.Free;


		//	Joint.angularXMotion = ConfigurableJointMotion.Free;
//			Joint.angularYMotion = ConfigurableJointMotion.Free;
//			Joint.angularZMotion = ConfigurableJointMotion.Free;
//			Joint.xMotion = ConfigurableJointMotion.Free;
//			Joint.yMotion = ConfigurableJointMotion.Free;
//			Joint.zMotion = ConfigurableJointMotion.Free;


			var resetDrv = new JointDrive
			{
				//mode = JointDriveMode.PositionAndVelocity,
				positionSpring = 0,
				positionDamper = 0,
				maximumForce = 0
			};

			bool bDo = false;

			if(bDo)
				Joint.angularYZDrive = resetDrv;



			bool swap = false;

			if(part.attachJoint.Target != part.attachNodes[1].attachedPart)	// liegt am top/bottom Zeugs... das muss halt stimmen -> definieren, top muss Element bla sein, bottom Element blö
				swap = true;


			Joint.anchor = Vector3.zero; // meistens -> wobei man das echt angeben können müsste - FEHLER

			// correct connectedAnchor
			Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(
				Joint.transform.TransformPoint(Joint.anchor));

			// rotateAxis is given, secondaryAxis must be perpendicular, that's what we create here
			Joint.axis = axis;
			Joint.secondaryAxis =
				Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
				? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);




			// und jetzt den Mesh-Müll
			Transform ModelTransform = part.transform.FindChild("model");
	//		RotateModelTransform = ModelTransform.FindChild(rotateModel);

			Transform FixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? "Joint" : fixedMesh);
				// FEHLER, nicht fix

			FixedMeshTransform.parent = Joint.connectedBody.transform;
		}
		
		int iStep = 0;



	public static PartJoint PartJoint_Create(Part owner, Part parent, AttachNode nodeToParent, AttachNode nodeFromParent, AttachModes mode)
	{
		Transform partTransform = owner.partTransform;
		bool flag = false;
		AttachNode attachNode;
		if (nodeToParent == null)
		{
			attachNode = nodeFromParent;
			partTransform = parent.partTransform;
			if (attachNode != null)
			{
				flag = attachNode.rigid;
			}
		}
		else if (nodeFromParent == null)
		{
			attachNode = nodeToParent;
			partTransform = owner.partTransform;
			flag = attachNode.rigid;
		}
		else
		{
			if (nodeToParent.size <= nodeFromParent.size)
			{
				attachNode = nodeToParent;
				partTransform = owner.partTransform;
			}
			else
			{
				attachNode = nodeFromParent;
				partTransform = parent.partTransform;
			}
			flag = (nodeToParent.rigid || nodeFromParent.rigid);
		}
		if (attachNode == null)
		{
			attachNode = parent.srfAttachNode;
			partTransform = parent.partTransform;
			flag = attachNode.rigid;
		}
		return PartJoint_create(owner, parent, partTransform, attachNode.position, attachNode.orientation, attachNode.secondaryAxis, attachNode.size, mode, flag);
	}


	private static PartJoint PartJoint_create(Part child, Part parent, Transform nodeSpace, Vector3 nodePos, Vector3 nodeOrt, Vector3 nodeOrt2, int nodeSize, AttachModes mode, bool rigid)
	{
		return new PartJoint();
/*
		Part part;
		if (child.physicalSignificance == Part.PhysicalSignificance.FULL && parent.physicalSignificance == Part.PhysicalSignificance.FULL)
		{
			part = child;
		}
		else if (child.physicalSignificance != Part.PhysicalSignificance.FULL)
		{
			part = parent;
		}
		else
		{
			if (parent.physicalSignificance == Part.PhysicalSignificance.FULL)
			{
				Debug.LogError("[PartJoint]: Cannot create a PartJoint between two physicsless parts. Something is very wrong here.", child);
				Debug.Break();
				return null;
			}
			part = child;
		}
		PartJoint partJoint = part.gameObject.AddComponent<PartJoint>();
		partJoint.child = child;
		partJoint.parent = parent;
		partJoint.host = part;
		partJoint.target = ((!(part == child)) ? child : parent);
		nodePos = child.partTransform.InverseTransformPoint(nodeSpace.TransformPoint(nodePos));
		nodeOrt = child.partTransform.InverseTransformDirection(nodeSpace.TransformDirection(nodeOrt));
		nodeOrt2 = child.partTransform.InverseTransformDirection(nodeSpace.TransformDirection(nodeOrt2));
		partJoint.mode = mode;
		partJoint.rigid = rigid;
		partJoint.linearJointLimit = default(SoftJointLimit);
		partJoint.linearJointLimitSpring = default(SoftJointLimitSpring);
		partJoint.angularLimit = default(SoftJointLimit);
		partJoint.angularLimitSpring = default(SoftJointLimitSpring);
		partJoint.linearDrive = default(JointDrive);
		partJoint.angXDrive = default(JointDrive);
		partJoint.angYZDrive = default(JointDrive);
		if (nodeOrt2.IsZero())
		{
			Vector3.OrthoNormalize(ref nodeOrt, ref nodeOrt2);
		}
		partJoint.joints = new List<ConfigurableJoint>();
		if (nodeSize < 2)
		{
			partJoint.internalJoints = 1;
			partJoint.joints.Add(partJoint.SetupJoint(nodePos, nodeOrt, nodeOrt2, nodeSize));
		}
		else
		{
			partJoint.internalJoints = 3;
			for (int i = 0; i < partJoint.internalJoints; i++)
			{
				partJoint.joints.Add(partJoint.SetupJoint(nodePos + Quaternion.AngleAxis(120f * (float)i, nodeOrt) * (nodeOrt2 * ((float)nodeSize - 1f)), nodeOrt, nodeOrt2, nodeSize));
			}
		}
		partJoint.goodSetup = true;
		return partJoint;
*/	}


	public void CreateAttachJoint(Part p, AttachModes mode)
	{
		if (p.parent && p.isAttached)
		{
			AttachNode nodeToParent = p.FindAttachNodeByPart(p.parent);
			AttachNode nodeFromParent = p.parent.FindAttachNodeByPart(p);
			p.attachJoint = PartJoint_Create(p, p.parent, nodeToParent, nodeFromParent, mode);
		}
	}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "-- mini los 2 !!")]
		public void MiniLos2()
		{
			part.attachJoint.DestroyJoint();

			Quaternion rot = Quaternion.AngleAxis(25, axis);
			part.gameObject.transform.rotation *= rot;
		//	Joint.transform.rotation *= rot;
			part.UpdateOrgPosAndRot(vessel.rootPart);

		//	part.attachJoint.DestroyJoint();
			part.CreateAttachJoint(vessel.rootPart.attachMode);
			part.ResetJoints();

			Joint = part.attachJoint.Joint;

			DrawAxis(7, Joint.transform, part.gameObject.transform.up, false, Joint.transform.right * 0.2f);
			DrawAxis(8, Joint.transform, Joint.transform.up, false, Joint.transform.right * 0.3f);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "-- mini los 3 !!")]
		public void MiniLos3()
		{
			part.attachJoint.DestroyJoint();

			Quaternion rot = Quaternion.AngleAxis(10, axis);
			part.gameObject.transform.rotation *= rot;
		//	Joint.transform.rotation *= rot;
			part.UpdateOrgPosAndRot(vessel.rootPart);

		//	part.attachJoint.DestroyJoint();
			part.CreateAttachJoint(vessel.rootPart.attachMode);
			part.ResetJoints();

			Joint = part.attachJoint.Joint;

			DrawAxis(9, Joint.transform, part.gameObject.transform.up, false, Joint.transform.right * 0.4f);
			DrawAxis(10, Joint.transform, Joint.transform.up, false, Joint.transform.right * 0.5f);
		}


		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "-- mini los !!")]
		public void MiniLos()
		{
// was wir annehmen -> GameObject und Joint haben die gleiche Ausrichtung

			DrawAxis(11, Joint.transform, part.gameObject.transform.up, false, Joint.transform.right * 0.0f);
			DrawAxis(12, Joint.transform, Joint.transform.up, false, Joint.transform.right * 0.1f);
	// das scheinen die Achsen... jetzt mal noch das min/max einzeichnen



	//		DrawAxis(0, Joint.transform, part.attachJoint.HostAnchor, true, Joint.transform.right);
	//		DrawAxis(6, Joint.transform, part.attachJoint.TgtAnchor, true, Joint.transform.right);

/*			float f = AngleSigned(Joint.transform.rotation.eulerAngles, Joint.connectedBody.transform.rotation.eulerAngles,
				Vector3.Cross(Joint.transform.rotation.eulerAngles, Joint.connectedBody.transform.rotation.eulerAngles));

			DrawAxis(0, Joint.transform, Joint.connectedBody.transform.position - Joint.transform.position, false);
			DrawAxis(6, Joint.transform,
				Quaternion.AngleAxis(f, Joint.transform.TransformDirection(Joint.axis)) * (Joint.connectedBody.transform.position - Joint.transform.position), false);
*/

//			DrawAxis(0, Joint.transform, Joint.transform.up, false);
//			DrawAxis(6, Joint.transform, Joint.connectedBody.transform.up, false);

//Quaternion.FromToRotation(joint.axis, joint.connectedBody.transform.rotation.eulerAngles)

// das soll die "current rotation" sein -> return(Quaternion.FromToRotation(joint.axis, joint.connectedBody.transform.rotation.eulerAngles));

return;
	
			DrawAxis(0, Joint.transform, swap ? -Joint.transform.up : Joint.transform.up, false, Joint.transform.right);

/*
			Transform jointMesh = KSPUtil.FindInPartModel(transform, "Joint");
			Transform baseMesh = KSPUtil.FindInPartModel(transform, "Base");

			float rotang = AngleSigned(jointMesh.up, baseMesh.up, Joint.transform.right);
*/

			Quaternion rot = Quaternion.AngleAxis(-position, axis);
			Joint.transform.rotation *= rot;
			part.UpdateOrgPosAndRot(vessel.rootPart);

			part.attachJoint.DestroyJoint();
		//	CreateAttachJoint(part, vessel.rootPart.attachMode);
			part.CreateAttachJoint(vessel.rootPart.attachMode);
			part.ResetJoints();


			Joint = part.attachJoint.Joint;

			DrawAxis(6, Joint.transform, swap ? -Joint.transform.up : Joint.transform.up, false, 2 * Joint.transform.right);

return;
			Joint.transform.rotation *= Quaternion.Inverse(rot);
			part.UpdateOrgPosAndRot(vessel.rootPart);

			position = float.NaN;

			Initialize();


// FEHLER, nur wegen Test, gehört hier ganz sicher nicht rein -> geht noch nicht ganz
	// also, folgende Aufgabe -> redock, reload -> zusehen, dass das MiniLos dann den Joint auf 0 dreht...
	// und dass es noch immer läuft, was wir beim Redock brauchen...

			// Anzeige machen
			DrawAxis(10, Joint.transform, swap ? -Joint.transform.up : Joint.transform.up, false);
			DrawAxis(11, Joint.transform,
				Quaternion.AngleAxis(-Joint.lowAngularXLimit.limit, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
			DrawAxis(12, Joint.transform,
				Quaternion.AngleAxis(-Joint.highAngularXLimit.limit, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);



			return;

/*


			float position2 = position;

			Quaternion rot = Quaternion.AngleAxis(-position, axis);
			Joint.transform.rotation *= rot;
			part.UpdateOrgPosAndRot(vessel.rootPart);

			part.SetHierarchyRoot(part);

			position = float.NaN;

			Initialize();

			part.attachJoint.Joint.transform.rotation *= Quaternion.Inverse(rot);
			part.UpdateOrgPosAndRot(vessel.rootPart);

			return;


*/

			float _position = position;

Quaternion revRotation = Quaternion.Inverse(Quaternion.AngleAxis(_position, Joint.transform.TransformDirection(axis)));
//arschloch8.DrawLineInGameView(Joint.transform.position, Joint.transform.position + revRotation * Joint.transform.up, arschloch8color);

			return;


			part.attachJoint.Joint.transform.rotation = Joint.connectedBody.transform.rotation * rotationConnectedBodyToJoint;
				// der NEUE Joint soll so orientiert sein, wie der ALTE es mal WAR!

			// revert non moving mesh
			if(NonMovingMeshTransform)
				NonMovingMeshTransform.rotation = Joint.transform.rotation * rotationJointToNonMoving;


			return;













		//	Joint = part.attachJoint.Joint;

	//		Joint.xMotion = ConfigurableJointMotion.Locked;
	//		Joint.yMotion = ConfigurableJointMotion.Locked;
	//		Joint.zMotion = ConfigurableJointMotion.Locked;
	//		Joint.angularXMotion = ConfigurableJointMotion.Limited;
	//		Joint.angularYMotion = ConfigurableJointMotion.Locked;
	//		Joint.angularZMotion = ConfigurableJointMotion.Locked;

	//		Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
	//		Joint.angularXDrive = new JointDrive
	//		{
	//			maximumForce = 0.0f, //0.2
	//			positionSpring = 0.0f,
	//			positionDamper = 20.0f // 0.5 weil ich nicht ewiges Schwingen will beim Test
	//		};

	//		SoftJointLimit sjll, sjlh;
			
	//		sjll = new SoftJointLimit();
	//		sjll.limit = -10.0f;

	//		sjlh = new SoftJointLimit();
	//		sjlh.limit = 80.0f;

	//		Joint.lowAngularXLimit = sjll;
	//		Joint.highAngularXLimit = sjlh;
	//		Joint.lowAngularXLimit = sjll;
	//		Joint.highAngularXLimit = sjlh;

	//		Joint.enableCollision = false;
	//		Joint.enablePreprocessing = false;

	//		Joint.projectionMode = JointProjectionMode.None;

	//		Joint.anchor = Vector3.zero; // meistens -> wobei man das echt angeben können müsste - FEHLER

			// correct connectedAnchor
	//		Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(
	//			Joint.transform.TransformPoint(Joint.anchor));

			// rotateAxis is given, secondaryAxis must be perpendicular, that's what we create here
	//		Joint.axis = rotateAxis;
			Vector3 sa =
				Vector3.Dot(Vector3.right, axis) < Vector3.Dot(Vector3.up, axis)
				? Vector3.Cross(Vector3.right, axis) : Vector3.Cross(Vector3.up, axis);

			++iStep;
	//		for(int i = 0; i < iStep; i++)
	//			sa = Quaternion.AngleAxis((30 * iStep) % 360, Joint.axis) * sa;

			switch(iStep % 3)
			{
			case 0:
				Joint.transform.Rotate(Joint.axis, 30, Space.Self); break;

			case 1:
				Joint.transform.Rotate(Joint.axis, -30, Space.Self); break;

			case 2:
				break;
			}

			Joint.secondaryAxis = sa;
				// ok, wenn die achse gesetzt wird, dann ist das der 0-Punkt... aber es ist egal wo sich die Axe befindet
			// evtl. sollte man den Joint drehen, achse setzen, zurückdrehen... *hmm*
				// -> ja genau, kurz zurücksetzen, dann kommt's gut

			switch(iStep % 3)
			{
			case 0:
				Joint.transform.Rotate(Joint.axis, -30, Space.Self); break;

			case 1:
				Joint.transform.Rotate(Joint.axis, 30, Space.Self); break;

			case 2:
				break;
			}
		}
	}
}
