using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InfernalRobotics_v3.Module;

namespace InfernalRobotics_v3.Utility
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class EditorEventLogic : MonoBehaviour
	{
		public static EditorEventLogic Instance;

		private void Awake()
		{
			if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onEditorLoad.Add(OnEditorLoad);
				GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
				Instance = this;
			}
			else
				Instance = null;
		}

		private void OnDestroy()
		{
			GameEvents.onEditorLoad.Remove(OnEditorLoad);
			GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
		}

		////////////////////
		// Callback Handlers

		private void OnEditorAttached(Part p)
		{
			for(int i = 0; i < p.children.Count; i++)
			{
				ModuleIRServo_v3 s = p.children[i].GetComponent<ModuleIRServo_v3>();
				if(s)
					s.OnEditorAttached(false);

				OnEditorAttached(p.children[i]);
			}
		}

		private void OnEditorDetached(Part p)
		{
			for(int i = 0; i < p.children.Count; i++)
			{
				ModuleIRServo_v3 s = p.children[i].GetComponent<ModuleIRServo_v3>();
				if(s)
					s.OnEditorDetached(false);

				OnEditorDetached(p.children[i]);
			}
		}

		private void OnEditorRootSelected(Part p)
		{
			ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
			if(s)
				s.OnEditorRootSelected();

			for(int i = 0; i < p.children.Count; i++)
				OnEditorRootSelected(p.children[i]);
		}

		////////////////////
		// Callback Functions
		
		public void OnEditorLoad(ShipConstruct s, KSP.UI.Screens.CraftBrowserDialog.LoadType t)
		{
			foreach(Part part in s.parts)
				Initialize(part);
		}

		public void OnEditorPartEvent(ConstructionEventType evt, Part part)
		{
			switch(evt)
			{
			case ConstructionEventType.PartAttached:
				{
					Part _p = part;
					int i = 0;

				next:
					ModuleIRServo_v3 s = _p.GetComponent<ModuleIRServo_v3>();
					if(s)
						s.OnEditorAttached(true);

					OnEditorAttached(_p);

					if(i < part.symmetryCounterparts.Count)
					{
						_p = part.symmetryCounterparts[i++];
						goto next;
					}

					EditorLogic.fetch.ResetBackup();
				}

Initialize(part); // FEHLER, nicht sicher, könnte aber richtig sein
foreach(Part p in part.symmetryCounterparts)
						Initialize(p);

foreach(Part c in part.children)
					{
Initialize(c);
						foreach(Part p in c.symmetryCounterparts)
							Initialize(p);
					}
				break;

			case ConstructionEventType.PartDetached:
				{
					ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();
					if(s)
						s.OnEditorDetached(true);

					OnEditorDetached(part);
				}
				break;

			case ConstructionEventType.PartCopied:
				{
					ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();
					if(s)
						s.OnEditorCopied();
				}
				break;

			case ConstructionEventType.PartRootSelected:
				OnEditorRootSelected(part);
				break;

			case ConstructionEventType.PartCreated:
				{
					Initialize(part);
				}
				break;

			case ConstructionEventType.PartOffsetting:
			case ConstructionEventType.PartRotating:
				{
				if((part.symmetryCounterparts.Count > 0) && (part.symMethod == SymmetryMethod.Mirror))
					RestoreRotation(part);
				}
				break;

			case ConstructionEventType.PartRotated:
				{
				}
				break;

			case ConstructionEventType.PartDragging:
				if((part.symmetryCounterparts.Count > 0) && (part.symMethod == SymmetryMethod.Mirror))
				{
					Part mirrorRoot = FindMirrorRoot(part.potentialParent);

					Vector3 position = mirrorRoot.transform.position; Quaternion rotation = mirrorRoot.transform.rotation;
					Vector3 symmetricPosition = mirrorRoot.transform.position; Quaternion symmetricRotation = mirrorRoot.transform.rotation;

					if(part.potentialParent != mirrorRoot)
					{
						DeepMirror(part.potentialParent, mirrorRoot,
							ref position, ref rotation, ref symmetricPosition, ref symmetricRotation);
					}

					// manueller Schritt von RestoreFullMirroringVirtualReversed

					Vector3 localPosition = Quaternion.Inverse(part.potentialParent.transform.rotation) * (part.transform.position - part.potentialParent.transform.position);
					Quaternion localRotation = Quaternion.Inverse(part.potentialParent.transform.rotation) * part.transform.rotation;

					// update values from part.parent to part
					position += rotation * localPosition;
					rotation = rotation * localRotation;

					Vector3 mirroredPosition; Quaternion mirroredRotation;

					if(!part.GetComponent<ModuleIRServo_v3>())
					{
			//			if(!IsMirrored(part, out mirroredPosition, out mirroredRotation)) -> FEHLER, gilt hier IMMER -> weil dragged Zeugs IMMER neu mirrored wird
						RestoreMirroringVirtual2(part, mirrorRoot, position, rotation, symmetricPosition, symmetricRotation, out mirroredPosition, out mirroredRotation);
					}
					else
					{
						RestoreMirroringVirtual(part, mirrorRoot, position, rotation, symmetricPosition, symmetricRotation, null, out mirroredPosition, out mirroredRotation);
// FEHLER, geht das, ohne attach-Point? denke nicht..
	// per Zufall geht's wohl -> FEHLER, später irgendwie anders machen ?? oder ist das ok bei Servos?
					}

					// update values from part.parent to part
					symmetricPosition += symmetricRotation * mirroredPosition;
					symmetricRotation = symmetricRotation * mirroredRotation;

//					Set(part.symmetryCounterparts[0], mirroredPosition, mirroredRotation);
// FEHLER, hier muss ich absolute Werte speichern (und das sind per Zufall gerade symmetricX), weil das Teil keinen parent hat im Moment -> deshalb mussten wir ja oben auch künstlich die localPostion und so berechnen
					Set(part.symmetryCounterparts[0], symmetricPosition, symmetricRotation);


/*
 * FEHLER, ok, was haben wir? wirhaben offenbar das Zeug gespiegelt und auf das nicht rotierte root
 * übertragen... eigentlich müsste ich es ja nur auf das gedrehte (aktuell vorhandene also) root übertragen
 * oder nicht? hmm... tja, könnte man probieren...
 * 
 * */

Set(part.symmetryCounterparts[0],
		part.symmetryCounterparts[0].potentialParent.transform.position
							+ part.symmetryCounterparts[0].potentialParent.transform.rotation * mirroredPosition,
		part.symmetryCounterparts[0].potentialParent.transform.rotation
							* mirroredRotation);



// FEHLER, könnte gehen, aber das mit dem restore von Parents von dem wo ich drauf häng, das fehlt natürlich

ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();
if(s)
	rotation = rotation * Quaternion.Inverse(localRotation) * s.CalculateNeutralRotation(localRotation);

ModuleIRServo_v3 sm = part.symmetryCounterparts[0].GetComponent<ModuleIRServo_v3>();
if(sm)
	symmetricRotation = symmetricRotation * Quaternion.Inverse(localRotation) * sm.CalculateNeutralRotation(localRotation);

// FEHLER, temp
ResetAllChildren(part);

					// continue with all children
					for(int i = 0; i < part.children.Count; i++)
						RestoreFullMirroringVirtualReversed(part.children[i], mirrorRoot, position, rotation, symmetricPosition, symmetricRotation);

					RestoreTurnVirtualIntegratedReversed(part);

					SetTheValuesReversed(part.symmetryCounterparts[0]);
				}
				break;
			}

check(part);
		}

void check(Part p)
{
	bool bScheisse = false;

	ModuleIRMovedPartEditor2 m = p.GetComponent<ModuleIRMovedPartEditor2>();
	if(m && !m.bMirrored)
		bScheisse = true;

	foreach(Part sp in p.symmetryCounterparts)
	{
		ModuleIRMovedPartEditor2 sm = sp.GetComponent<ModuleIRMovedPartEditor2>();
		if(sm && !sm.bMirrored)
			bScheisse = true;
	}
}

		////////////////////
		// Helper Functions

		Part FindMirrorRoot(Part part)
		{
			while(part && (part.symmetryCounterparts.Count > 0) && (part.symMethod == SymmetryMethod.Mirror))
				part = part.parent;

			return part;						
		}

		void MirrorQuaternion(Quaternion rotation, out Vector3 direction, out Vector3 direction2)
		{
			direction = EditorLogic.RootPart.transform.InverseTransformDirection(rotation * Vector3.up);
			direction2 = EditorLogic.RootPart.transform.InverseTransformDirection(rotation * Vector3.forward);
			direction.x *= -1f;
			direction2.x *= -1f;
			direction = EditorLogic.RootPart.transform.TransformDirection(direction);
			direction2 = EditorLogic.RootPart.transform.TransformDirection(direction2);
		}

		void Initialize(Part part)
		{
			ModuleIRMovedPartEditor2 m = part.GetComponent<ModuleIRMovedPartEditor2>();
			if(!m)
				m = (ModuleIRMovedPartEditor2)part.AddModule("ModuleIRMovedPartEditor2");

			m.bMirrored = true;
			m.localPosition = part.transform.localPosition;
			m.localRotation = part.transform.localRotation;
		}

		void Set(Part part, Vector3 localPosition, Quaternion localRotation)
		{
			ModuleIRMovedPartEditor2 m = part.GetComponent<ModuleIRMovedPartEditor2>();
			if(!m)
				m = (ModuleIRMovedPartEditor2)part.AddModule("ModuleIRMovedPartEditor2");

			m.bMirrored = true;
			m.localPosition = localPosition;
			m.localRotation = localRotation;
		}

		bool IsMirrored(Part part, out Vector3 mirroredPosition, out Quaternion mirroredRotation)
		{
			ModuleIRMovedPartEditor2 m = part.symmetryCounterparts[0].GetComponent<ModuleIRMovedPartEditor2>();
			if(m)
			{
				mirroredPosition = m.localPosition;
				mirroredRotation = m.localRotation;
				return m.bMirrored;
			}

			mirroredPosition = part.symmetryCounterparts[0].transform.localPosition;
			mirroredRotation = part.symmetryCounterparts[0].transform.localRotation;
			return false;
		}

		void ResetAllChildren(Part part) // FEHLER, temp, Fix
		{
			ModuleIRMovedPartEditor2 m = part.symmetryCounterparts[0].GetComponent<ModuleIRMovedPartEditor2>();
			if(m)
				m.bMirrored = false;

			for(int i = 0; i < part.children.Count; i++)
				ResetAllChildren(part.children[i]);
		}

		bool DetectMirroringDirection(Part part, Part mirrorRoot)
		{
			Vector3 direction, direction2;
			MirrorQuaternion(part.transform.rotation, out direction, out direction2);

			Quaternion rotation = Quaternion.LookRotation(direction2, direction);
			Quaternion rotation2 = Quaternion.AngleAxis(180f, direction) * rotation;
			Quaternion rotation3 = Quaternion.LookRotation(-direction2, direction); // FEHLER, ist das nicht das gleiche wie rotation2?

			return (Vector3.Angle(direction2, rotation * Vector3.forward) > Vector3.Angle(direction2, rotation2 * Vector3.forward));
		}

		////////////////////
		// Functions

		// if the part is a ModuleIRServo_v3 with a parent, then we calculate
		// the rotation in the 0-position
		// (unattached parts are rotated already due to the needs of the editor
		// -> see OnEditorAttached/OnEditorDetached)

		void RestoreRotationVirtual(Part part, out Quaternion localRotation, out AttachNode a)
		{
			if(part.parent)
			{
				a = AttachNode.Clone(part.FindAttachNodeByPart(part.parent));

				ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();

				if(s)
				{
					localRotation = s.CalculateNeutralRotation(part.transform.localRotation);
					s._RotateBack(a);

					return;
				}
			}
			else
				a = null;

			localRotation = part.transform.localRotation;
		}

		void CalculateNeutralPosition(Part part, ref Vector3 position, ref Quaternion rotation, out AttachNode a)
		{
			Quaternion localRotation;
			RestoreRotationVirtual(part, out localRotation, out a);

			// update values from part.parent to part
			position += rotation * part.transform.localPosition;
			rotation = rotation * localRotation;
		}

		void ReRestoreRotationVirtual(Part part, ref Quaternion q)
		{
			Part symmetricPart = part.symmetryCounterparts[0];

			ModuleIRMovedPartEditor2 m = symmetricPart.GetComponent<ModuleIRMovedPartEditor2>();

			if(symmetricPart.parent)
			{
				ModuleIRServo_v3 s = symmetricPart.GetComponent<ModuleIRServo_v3>();

				if(s)
				{
					q = s.CalculateFinalRotation(m.localRotation);
					return;
				}
			}

			q = m.localRotation;
		}

		void RestoreMirroringVirtual2(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, Vector3 symmetricPosition, Quaternion symmetricRotation, out Vector3 mirroredPosition, out Quaternion mirroredRotation)
		{
			Vector3 vector = position - mirrorRoot.transform.position;
			Vector3 vector2 = Vector3.ProjectOnPlane(vector, EditorLogic.RootPart.transform.up);
			mirroredPosition = mirrorRoot.transform.position + (vector - vector2) + Quaternion.AngleAxis(180f, -EditorLogic.RootPart.transform.forward) * vector2;

			Quaternion rotRel = Quaternion.Inverse(part.transform.rotation) * rotation; // das ist die relative Rotation vom neutalen Punkt (q) zu dem wie es heute wirklich ist

			Vector3 direction, direction2;
			MirrorQuaternion(rotRel, out direction, out direction2);

			Quaternion rotation_;
			if(!DetectMirroringDirection(part, mirrorRoot))
				rotation_ = Quaternion.LookRotation(direction2, direction);
			else
				rotation_ = Quaternion.LookRotation(-direction2, direction);

			mirroredRotation = part.symmetryCounterparts[0].transform.rotation * rotation_; // so, Gegenüber auf 0 rotation vom Parent zurückgedreht

			// absolute Werte in relative umrechnen
			mirroredPosition = Quaternion.Inverse(symmetricRotation) * (mirroredPosition - symmetricPosition);
			mirroredRotation = Quaternion.Inverse(symmetricRotation) * mirroredRotation;
		}

		void RestoreMirroringVirtualNew(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, ref Vector3 symmetricPosition, ref Quaternion symmetricRotation, AttachNode a)
		{
			Vector3 mirroredPosition; Quaternion mirroredRotation;

			CalculateMirroringVirtualNew(part, mirrorRoot, position, rotation, ref symmetricPosition, ref symmetricRotation, a, out mirroredPosition, out mirroredRotation);

			Set(part.symmetryCounterparts[0], mirroredPosition, mirroredRotation);
		}

		void CalculateMirroringVirtualNew(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, ref Vector3 symmetricPosition, ref Quaternion symmetricRotation, AttachNode a, out Vector3 mirroredPosition, out Quaternion mirroredRotation)
		{
			if(!part.GetComponent<ModuleIRServo_v3>())
			{
				if(!IsMirrored(part, out mirroredPosition, out mirroredRotation))
					RestoreMirroringVirtual2(part, mirrorRoot, position, rotation, symmetricPosition, symmetricRotation, out mirroredPosition, out mirroredRotation);
			}
			else
			{
				RestoreMirroringVirtual(part, mirrorRoot, position, rotation, symmetricPosition, symmetricRotation, a, out mirroredPosition, out mirroredRotation);
			}

			// update values from part.parent to part
			symmetricPosition += symmetricRotation * mirroredPosition;
			symmetricRotation = symmetricRotation * mirroredRotation;
		}

		void RestoreMirroringVirtual(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, Vector3 symmetricPosition, Quaternion symmetricRotation, AttachNode a, out Vector3 mirroredPosition, out Quaternion mirroredRotation)
		{
			Vector3 vector = position - mirrorRoot.transform.position;
			Vector3 vector2 = Vector3.ProjectOnPlane(vector, EditorLogic.RootPart.transform.up);
			mirroredPosition = mirrorRoot.transform.position + (vector - vector2) + Quaternion.AngleAxis(180f, -EditorLogic.RootPart.transform.forward) * vector2;

			Vector3 direction, direction2;
			MirrorQuaternion(rotation, out direction, out direction2);

			mirroredRotation = Quaternion.LookRotation(direction2, direction);
			AttachNode attachNode = a;
			bool flag = part.OnWillBeMirrored(ref mirroredRotation, attachNode, part.parent);
			if(attachNode != null)
			{
				if(!flag)
				{
					if(Mathf.Abs(attachNode.orientation.x) > 0.0001f)
					{
						mirroredRotation = Quaternion.AngleAxis(180f, direction) * mirroredRotation;
					}
				}
			}

			// absolute Werte in relative umrechnen
			mirroredPosition = Quaternion.Inverse(symmetricRotation) * (mirroredPosition - symmetricPosition);
			mirroredRotation = Quaternion.Inverse(symmetricRotation) * mirroredRotation;
		}

		// input: position, symmetricPosition, rotation, symmetricRotation -> values of part.parent
		void RestoreFullMirroringVirtualReversed(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, Vector3 symmetricPosition, Quaternion symmetricRotation)
		{
			// calculate unrotated rotation
			AttachNode a2;
			CalculateNeutralPosition(part, ref position, ref rotation, out a2);

			// mirror the part unrotated
			RestoreMirroringVirtualNew(part, mirrorRoot, position, rotation, ref symmetricPosition, ref symmetricRotation, a2);

if(!part.parent) // FEHLER, etwas unschön, aber bei abgehängtem Teil muss statt dem rotation oben rechnen das hier passieren -> RestoreRotationVirtual tut dann auch nichts... bzw. gibt Identity zurück... also... man könnte sich was "schöneres" überlegen noch
{
ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();
if(s)
				{
	rotation = rotation * s.CalculateNeutralRotation(part.transform.localRotation);
				}
}

			// continue with all children
			for(int i = 0; i < part.children.Count; i++)
				RestoreFullMirroringVirtualReversed(part.children[i], mirrorRoot, position, rotation, symmetricPosition, symmetricRotation);
		}

		void SetTheValuesReversed(Part part)
		{
			ModuleIRMovedPartEditor2 m = part.GetComponent<ModuleIRMovedPartEditor2>();

			if(m)
			{
				part.transform.localPosition = m.localPosition;
				part.transform.localRotation = m.localRotation;
			}

			// jetzt alle Kinder
			for(int i = 0; i < part.children.Count; i++)
				SetTheValuesReversed(part.children[i]);
		}

		void RestoreTurnVirtualIntegratedReversed(Part part)
		{
			ModuleIRMovedPartEditor2 m = part.symmetryCounterparts[0].gameObject.GetComponent<ModuleIRMovedPartEditor2>();

			Quaternion q = Quaternion.identity;
			ReRestoreRotationVirtual(part, ref q);

			m.localRotation = q;

			// jetzt alle Kinder
			for(int i = 0; i < part.children.Count; i++)
				RestoreTurnVirtualIntegratedReversed(part.children[i]);
		}

		void DeepMirror(Part part, Part mirrorRoot, ref Vector3 position, ref Quaternion rotation, ref Vector3 symmetricPosition, ref Quaternion symmetricRotation)
		{
			// calculate the parent first
			if(part.parent != mirrorRoot)
				DeepMirror(part.parent, mirrorRoot, ref position, ref rotation, ref symmetricPosition, ref symmetricRotation);

			// calculate unrotated values
			AttachNode a;
			CalculateNeutralPosition(part, ref position, ref rotation, out a);

			// calculate mirrored unrotated values
			Vector3 mirroredPosition; Quaternion mirroredRotation;
			CalculateMirroringVirtualNew(part, mirrorRoot, position, rotation, ref symmetricPosition, ref symmetricRotation, a, out mirroredPosition, out mirroredRotation);
		}

		public void RestoreRotation(Part part)
		{
			// find root
			while(part.parent && (part.parent.symmetryCounterparts.Count > 0) && (part.parent.symMethod == SymmetryMethod.Mirror))
				part = part.parent;

			Part mirrorRoot = part.parent;

			RestoreFullMirroringVirtualReversed(part, mirrorRoot,
				mirrorRoot.transform.position, mirrorRoot.transform.rotation,
				mirrorRoot.transform.position, mirrorRoot.transform.rotation);

			RestoreTurnVirtualIntegratedReversed(part);

			SetTheValuesReversed(part.symmetryCounterparts[0]);
		}
	}

	public class ModuleIRMovedPartEditor2 : PartModule
	{
		public bool bMirrored;
		public Vector3 localPosition;
		public Quaternion localRotation;

		public override bool OnWillBeMirrored(ref Quaternion rotation, AttachNode selPartNode, Part partParent)
		{
			bMirrored = false;

			return false;
		}
	}
}
