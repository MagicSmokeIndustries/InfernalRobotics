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

		public void OnEditorLoad(ShipConstruct s, KSP.UI.Screens.CraftBrowserDialog.LoadType t)
		{
			foreach(Part p in s.parts)
			{
				ModuleIRMovedPartEditor2.InitializePart(p);
			}
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
				ModuleIRMovedPartEditor2.InitializePart(part);
				}
				break;

			case ConstructionEventType.PartOffsetting:
			case ConstructionEventType.PartRotating:
				{
				if((part.symmetryCounterparts.Count > 0) && (part.symMethod == SymmetryMethod.Mirror))
					ResetToNeutral(part); // FEHLER, doofer Name, ist neu etwas "anderes" :-)
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

						// FEHLER, Test -> hatte funktioniert
						//part.potentialParent.symmetryCounterparts[0].transform.position = symmetricPosition;
						//part.potentialParent.symmetryCounterparts[0].transform.rotation = symmetricRotation;
					}

Vector3 posOfParent = symmetricPosition; Quaternion rotOfParent = symmetricRotation;

					// einige Werte errechnen, die es nicht gibt, weil wir den Parent nicht gesetzt haben

					Vector3 localPosition = Quaternion.Inverse(part.potentialParent.transform.rotation) * (part.transform.position - part.potentialParent.transform.position);
					Quaternion localRotation = Quaternion.Inverse(part.potentialParent.transform.rotation) * part.transform.rotation;

					// FEHLER, neue Idee, damit wir's haben um später runter- und hoch-rechnen zu können für partDragging
				//	Set(part, part.transform.localPosition, part.transform.localRotation);
					// -> ergibt hier keinen Sinn

					// calculate unrotated rotation -> RestoreRotationVirtual -> können wir abkürzen
					Quaternion neutralLocalRot = localRotation; AttachNode a2 = null;

					// update values from part.parent to part
					position += rotation * localPosition;
					rotation = rotation * neutralLocalRot;

					// mirror the part
					Vector3 mirroredPosition; Quaternion mirroredRotation;
					RestoreMirroringVirtual(part, mirrorRoot, position, rotation, a2, out mirroredPosition, out mirroredRotation);

// FEHLER, hier müsste man noch die rotationen vom mirrorRoot her bis zum potentialParent anwenden !!! -> weil, wir hier plötzlich keinen Parent mehr haben in der Kette... daher
Vector3 mp2 = mirroredPosition; Quaternion mr2 = mirroredRotation;
if(part.potentialParent != mirrorRoot)
{
	mp2 = Quaternion.Inverse(rotOfParent) * mp2 - posOfParent;
	mr2 = Quaternion.Inverse(rotOfParent) * mr2;

	mp2 = part.symmetryCounterparts[0].potentialParent.transform.position + part.symmetryCounterparts[0].potentialParent.transform.rotation * mp2;
	mr2 = part.symmetryCounterparts[0].potentialParent.transform.rotation * mr2;
}

					Set(part.symmetryCounterparts[0], mp2, mr2);

					// update values from part.parent to part
					symmetricPosition = mirroredPosition;
					symmetricRotation = mirroredRotation;

ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();
if(s)
	rotation = rotation * s.CalculateNeutralRotation(localRotation);

ModuleIRServo_v3 sm = part.symmetryCounterparts[0].GetComponent<ModuleIRServo_v3>();
if(sm)
	symmetricRotation = symmetricRotation * sm.CalculateNeutralRotation(localRotation);

					// continue with all children
					for(int i = 0; i < part.children.Count; i++)
						RestoreFullMirroringVirtualReversed(part.children[i], mirrorRoot, position, rotation, symmetricPosition, symmetricRotation);

					RestoreTurnVirtualIntegratedReversed(part);

					SetTheValuesReversed(part);
				}
				break;
			}
		}

		// if the part is a ModuleIRServo_v3 with a parent, then we calculate
		// the rotation in the 0-position
		// (unattached parts are rotated already due to the needs of the editor
		// -> see OnEditorAttached/OnEditorDetached)

		void RestoreRotationVirtual(Part part, out Quaternion q, out AttachNode a)
		{
			if(part.parent)
			{
				a = AttachNode.Clone(part.FindAttachNodeByPart(part.parent));

				ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();

				if(s)
				{
					q = s.CalculateNeutralRotation(part.transform.localRotation);
					s._RotateBack(a);

					return;
				}
			}

			q = part.transform.localRotation;
			a = null;
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

		Part FindMirrorRoot(Part part)
		{
			while(part && (part.symmetryCounterparts.Count > 0) && (part.symMethod == SymmetryMethod.Mirror))
				part = part.parent;

			return part;						
		}

		void Set(Part part, Vector3 localPosition, Quaternion localRotation)
		{
			ModuleIRMovedPartEditor2 m = part.gameObject.GetComponent<ModuleIRMovedPartEditor2>();
			if(!m)
				m = part.gameObject.AddComponent<ModuleIRMovedPartEditor2>();

			m.localPosition = localPosition;
			m.localRotation = localRotation;
		}

		void RestoreMirroringVirtual(Part part, Part mirrorRoot, Vector3 v, Quaternion q, AttachNode a, out Vector3 position, out Quaternion rotation)
		{
			Vector3 vector = v - mirrorRoot.transform.position;
			Vector3 vector2 = Vector3.ProjectOnPlane(vector, EditorLogic.RootPart.transform.up);
			/*Vector3*/ position = mirrorRoot.transform.position + (vector - vector2) + Quaternion.AngleAxis(180f, -EditorLogic.RootPart.transform.forward) * vector2;
			Vector3 direction = EditorLogic.RootPart.transform.InverseTransformDirection(q * Vector3.up);
			Vector3 direction2 = EditorLogic.RootPart.transform.InverseTransformDirection(q * Vector3.forward);
			direction.x *= -1f;
			direction2.x *= -1f;
			direction = EditorLogic.RootPart.transform.TransformDirection(direction);
			direction2 = EditorLogic.RootPart.transform.TransformDirection(direction2);
			/*Quaternion*/ rotation = Quaternion.LookRotation(direction2, direction);
			AttachNode attachNode = a;
			bool flag = part.OnWillBeMirrored(ref rotation, attachNode, part.parent);
			if(attachNode != null)
			{
				if(!flag)
				{
					if(Mathf.Abs(attachNode.orientation.x) > 0.00001f)
					{
						rotation = Quaternion.AngleAxis(180f, direction) * rotation;
					}
				}
			}
		}

		// input: position, symmetricPosition, rotation, symmetricRotation -> values of part.parent
		void RestoreFullMirroringVirtualReversed(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, Vector3 symmetricPosition, Quaternion symmetricRotation)
		{
			// FEHLER, neue Idee, damit wir's haben um später runter- und hoch-rechnen zu können für partDragging
			Set(part, part.transform.localPosition, part.transform.localRotation);

			// calculate unrotated rotation
			Quaternion neutralLocalRot; AttachNode a2;
			RestoreRotationVirtual(part, out neutralLocalRot, out a2);

			// update values from part.parent to part
			position += rotation * part.transform.localPosition;
			rotation = rotation * neutralLocalRot;

			// mirror the part
			Vector3 mirroredPosition; Quaternion mirroredRotation;
			RestoreMirroringVirtual(part, mirrorRoot, position, rotation, a2, out mirroredPosition, out mirroredRotation);

			Set(part.symmetryCounterparts[0],
				Quaternion.Inverse(symmetricRotation) * (mirroredPosition - symmetricPosition),
				Quaternion.Inverse(symmetricRotation) * mirroredRotation);

			// update values from part.parent to part
			symmetricPosition = mirroredPosition;
			symmetricRotation = mirroredRotation;

if(!part.parent) // FEHLER, etwas unschön, aber bei abgehängtem Teil muss statt dem rotation oben rechnen das hier passieren -> RestoreRotationVirtual tut dann auch nichts... bzw. gibt Identity zurück... also... man könnte sich was "schöneres" überlegen noch
{
ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();
if(s)
	rotation = rotation * s.CalculateNeutralRotation(part.transform.localRotation);
}

			// continue with all children
			for(int i = 0; i < part.children.Count; i++)
				RestoreFullMirroringVirtualReversed(part.children[i], mirrorRoot, position, rotation, symmetricPosition, symmetricRotation);
		}

		void SetTheValuesReversed(Part part)
		{
			ModuleIRMovedPartEditor2 m = part.symmetryCounterparts[0].gameObject.GetComponent<ModuleIRMovedPartEditor2>();

			part.symmetryCounterparts[0].transform.localPosition = m.localPosition;
			part.symmetryCounterparts[0].transform.localRotation = m.localRotation;

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

		void DeepAnalyze(Part part, Part mirrorRoot, ref Vector3 position, ref Quaternion rotation, out AttachNode a2)
		{
			// calculate the parent first
			if(part.parent != mirrorRoot)
				DeepAnalyze(part.parent, mirrorRoot, ref position, ref rotation, out a2);

			// calculate unrotated rotation
			Quaternion neutralLocalRot;
			RestoreRotationVirtual(part, out neutralLocalRot, out a2);

			// update values from part.parent to part
			position += rotation * part.transform.localPosition;
			rotation = rotation * neutralLocalRot;
		}

		void DeepMirror(Part part, Part mirrorRoot, ref Vector3 position, ref Quaternion rotation, ref Vector3 symmetricPosition, ref Quaternion symmetricRotation)
		{
			AttachNode a2;

			DeepAnalyze(part, mirrorRoot, ref position, ref rotation, out a2);

			// mirror the part
			Vector3 mirroredPosition; Quaternion mirroredRotation;
			RestoreMirroringVirtual(part, mirrorRoot, position, rotation, a2, out mirroredPosition, out mirroredRotation);

			// update values from part.parent to part
			symmetricPosition = mirroredPosition;
			symmetricRotation = mirroredRotation;
		}

		public void ResetToNeutral(Part part)
		{
			// find root
			while(part.parent && (part.parent.symmetryCounterparts.Count > 0) && (part.parent.symMethod == SymmetryMethod.Mirror))
				part = part.parent;

			Part mirrorRoot = part.parent;

			RestoreFullMirroringVirtualReversed(part, mirrorRoot,
				mirrorRoot.transform.position, mirrorRoot.transform.rotation,
				mirrorRoot.transform.position, mirrorRoot.transform.rotation);

			RestoreTurnVirtualIntegratedReversed(part);

			SetTheValuesReversed(part);
		}
	}
}
