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
				GameEvents.onEditorStarted.Add(OnEditorStarted);
				GameEvents.onEditorLoad.Add(OnEditorLoad);
				GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
				Instance = this;
			}
			else
				Instance = null;
		}

		private void OnDestroy()
		{
			GameEvents.onEditorStarted.Remove(OnEditorStarted);
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
		
		public void OnEditorStarted()
		{
			ShipConstruct s = EditorLogic.fetch.ship;

			if(s != null)
			{
				foreach(Part part in s.parts)
					SetCurrent(part);
			}
		}

		public void OnEditorLoad(ShipConstruct s, KSP.UI.Screens.CraftBrowserDialog.LoadType t)
		{
			foreach(Part part in s.parts)
				SetCurrent(part);
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

					SetCurrentRecursive(part);
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
				{
					OnEditorRootSelected(part);
				}
				break;

			case ConstructionEventType.PartCreated:
				{
					SetCurrent(part);
				}
				break;

			case ConstructionEventType.PartOffsetting:
			case ConstructionEventType.PartRotating:
				{
					if(IsMirrored(part))
						RestoreRotation(part);
				}
				break;

			case ConstructionEventType.PartDragging:
				if(IsMirrored(part))
				{
					Part mirrorRoot; bool hasIR;
					Vector3 parentPosition = Vector3.zero; Quaternion parentRotation = Quaternion.identity;
					Vector3 symmetryParentPosition = Vector3.zero; Quaternion symmetryParentRotation = Quaternion.identity;

					FindMirrorInfo(part.potentialParent, out mirrorRoot, out hasIR, ref parentPosition, ref parentRotation, ref symmetryParentPosition, ref symmetryParentRotation);

					hasIR = hasIR | IsIR(part);

					if(!hasIR)
					{
						RestoreMirroring(part, mirrorRoot);
					}
					else
					{
						part.transform.position = Quaternion.Inverse(part.potentialParent.transform.rotation) * (part.transform.position - part.potentialParent.transform.position);
						part.transform.rotation = Quaternion.Inverse(part.potentialParent.transform.rotation) * part.transform.rotation;

						RestoreMirroringIR(part, mirrorRoot,
							parentPosition, parentRotation,
							symmetryParentPosition, symmetryParentRotation);

						part.transform.position = part.potentialParent.transform.position + part.potentialParent.transform.rotation * part.transform.position;
						part.transform.rotation = part.potentialParent.transform.rotation * part.transform.rotation;

						part.symmetryCounterparts[0].transform.position = part.symmetryCounterparts[0].potentialParent.transform.position + part.symmetryCounterparts[0].potentialParent.transform.rotation * part.symmetryCounterparts[0].transform.position;
						part.symmetryCounterparts[0].transform.rotation = part.symmetryCounterparts[0].potentialParent.transform.rotation * part.symmetryCounterparts[0].transform.rotation;
					}
				}
				break;
			}

check(part); // FEHLER, raus, wenn's dann stimmt
		}

void check(Part p)
{
	bool bFailure = false;

	ModuleIREditorHelper m = p.GetComponent<ModuleIREditorHelper>();
	if(!m)
		bFailure = true;

	foreach(Part sp in p.symmetryCounterparts)
	{
		ModuleIREditorHelper sm = sp.GetComponent<ModuleIREditorHelper>();
		if(!sm)
			bFailure = true;
	}

	if(bFailure)
		Logger.Log("check failed!!!!!!!", Logger.Level.Error);
}

		////////////////////
		// Helper Functions

		bool IsMirrored(Part part)
		{
			return (part.symmetryCounterparts.Count > 0) && (part.symMethod == SymmetryMethod.Mirror);
		}

		ModuleIRServo_v3 IsIR(Part part)
		{
			return part.GetComponent<ModuleIRServo_v3>();
		}

		void UpdateWithNeutralPositionAndRotation(Part part, ref Vector3 position, ref Quaternion rotation)
		{
			Quaternion localRotation = part.transform.localRotation;

			ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();

			if(s)
				localRotation = s.CalculateNeutralRotation(localRotation);

			position = part.transform.localPosition + localRotation * position;
			rotation = localRotation * rotation;
		}

		void FindMirrorInfo(Part part, out Part mirrorRoot, out bool hasIR, ref Vector3 position, ref Quaternion rotation, ref Vector3 symmetryPosition, ref Quaternion symmetryRotation)
		{
			hasIR = false;

			while(IsMirrored(part))
			{
				UpdateWithNeutralPositionAndRotation(part, ref position, ref rotation);
				UpdateWithNeutralPositionAndRotation(part.symmetryCounterparts[0], ref symmetryPosition, ref symmetryRotation);

				hasIR = hasIR | IsIR(part);

				part = part.parent;
			}

			position = part.transform.position + part.transform.rotation * position;
			symmetryPosition = part.transform.position + part.transform.rotation * symmetryPosition;

			rotation = part.transform.rotation * rotation;
			symmetryPosition = part.transform.rotation * symmetryPosition;

			mirrorRoot = part;
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

		void Set(Part part, Vector3 localPosition, Quaternion localRotation)
		{
			ModuleIREditorHelper m = part.GetComponent<ModuleIREditorHelper>();
			if(!m)
				m = (ModuleIREditorHelper)part.AddModule("ModuleIREditorHelper");

			m.needsVerification = false;
			m.localPosition = localPosition;
			m.localRotation = localRotation;
		}

		void SetCurrent(Part part)
		{
			Set(part, part.transform.localPosition, part.transform.localRotation);
		}

		void SetCurrentRecursive(Part part)
		{
			SetCurrent(part);
			foreach(Part symmetryPart in part.symmetryCounterparts)
				SetCurrent(symmetryPart);

			foreach(Part child in part.children)
				SetCurrentRecursive(child);
		}

		// returns if attachNode.orientation.x was != 0f during the mirroring calculation
		bool DetectMirroringDirection(Part part, Part mirrorRoot)
		{
			Vector3 direction, direction2;
			MirrorQuaternion(part.transform.rotation, out direction, out direction2);

			Quaternion rotation = Quaternion.LookRotation(direction2, direction);
			Quaternion rotation2 = Quaternion.AngleAxis(180f, direction) * rotation;

			return Quaternion.Angle(part.symmetryCounterparts[0].transform.rotation, rotation)
				> Quaternion.Angle(part.symmetryCounterparts[0].transform.rotation, rotation2);
		}

		////////////////////
		// Functions

		// if the part is a ModuleIRServo_v3 with a parent, then we calculate
		// the rotation in the 0-position
		// (unattached parts are rotated already due to the needs of the editor
		// -> see OnEditorAttached/OnEditorDetached)

		void CalculateNeutralRotation(Part part, ref Quaternion localRotation, ref AttachNode attachNode)
		{
			if(part.parent)
			{
				ModuleIRServo_v3 s = part.GetComponent<ModuleIRServo_v3>();

				if(s)
				{
					localRotation = s.CalculateNeutralRotation(localRotation);
					s._RotateBack(attachNode);
				}
			}
		}

		void CalculateFinalRotation(Part symmetryPart, ref Quaternion localRotation)
		{
			if(symmetryPart.parent)
			{
				ModuleIRServo_v3 s = symmetryPart.GetComponent<ModuleIRServo_v3>();

				if(s)
				{
					localRotation = s.CalculateFinalRotation(localRotation);
				}
			}
		}

		void CalculateMirroredPositionAndRotation(Part part, Part mirrorRoot, Vector3 position, Quaternion rotation, AttachNode attachNode, out Vector3 symmetryPosition, out Quaternion symmetryRotation)
		{
			Vector3 vector = position - mirrorRoot.transform.position;
			Vector3 vector2 = Vector3.ProjectOnPlane(vector, EditorLogic.RootPart.transform.up);
			symmetryPosition = mirrorRoot.transform.position + (vector - vector2) + Quaternion.AngleAxis(180f, -EditorLogic.RootPart.transform.forward) * vector2;

			Vector3 direction, direction2;
			MirrorQuaternion(rotation, out direction, out direction2);

			symmetryRotation = Quaternion.LookRotation(direction2, direction);
			bool flag = part.OnWillBeMirrored(ref symmetryRotation, attachNode, part.parent);
			if(attachNode != null)
			{
				if(!flag)
				{
					if(attachNode.orientation.x != 0f)
					{
						symmetryRotation = Quaternion.AngleAxis(180f, direction) * symmetryRotation;
					}
				}
			}
		}

		public void RestoreMirroringIR(Part part, Part mirrorRoot,
			Vector3 parentPosition, Quaternion parentRotation,
			Vector3 symmetryParentPosition, Quaternion symmetryParentRotation)
		{
			bool bIsIR = IsIR(part);

			Vector3 _parentPosition; Quaternion _parentRotation;
			Vector3 _symmetryParentPosition; Quaternion _symmetryParentRotation;

			ModuleIREditorHelper m = part.GetComponent<ModuleIREditorHelper>();

			Quaternion localRotation = part.transform.localRotation;
			AttachNode attachNode = m.attachNodeUsed;

			// calculate unrotated position and rotation
			if(bIsIR)
				CalculateNeutralRotation(part, ref localRotation, ref attachNode);

			// mirror the part unrotated
			Vector3 position = parentPosition + parentRotation * part.transform.localPosition;
			Quaternion rotation = parentRotation * localRotation;

			_parentPosition = position; _parentRotation = rotation;

			Vector3 symmetryPosition; Quaternion symmetryRotation;

			CalculateMirroredPositionAndRotation(part, mirrorRoot, position, rotation, attachNode, out symmetryPosition, out symmetryRotation);

			_symmetryParentPosition = symmetryPosition; _symmetryParentRotation = symmetryRotation;

			symmetryPosition = Quaternion.Inverse(symmetryParentRotation) * (symmetryPosition - symmetryParentPosition);
			symmetryRotation = Quaternion.Inverse(symmetryParentRotation) * symmetryRotation;

			// calculate rotated position and rotation
			if(IsIR(part))
				CalculateFinalRotation(part.symmetryCounterparts[0], ref symmetryRotation);

			Set(part.symmetryCounterparts[0], symmetryPosition, symmetryRotation);

			part.symmetryCounterparts[0].transform.localPosition = symmetryPosition;
			part.symmetryCounterparts[0].transform.localRotation = symmetryRotation;

			foreach(Part child in part.children)
				RestoreMirroringIR(child, mirrorRoot,
					_parentPosition, _parentRotation,
					_symmetryParentPosition, _symmetryParentRotation);
		}

		public void RestoreMirroring(Part part, Part mirrorRoot)
		{
			if(!IsIR(part))
			{
				SetCurrent(part);
				SetCurrent(part.symmetryCounterparts[0]);

				foreach(Part child in part.children)
					RestoreMirroring(child, mirrorRoot);
			}
			else
			{
				RestoreMirroringIR(part, mirrorRoot,
					part.parent.transform.position, part.parent.transform.rotation,
					part.parent.symmetryCounterparts[0].transform.position, part.parent.symmetryCounterparts[0].transform.rotation);
			}
		}

		public void RestoreRotation(Part part)
		{
			Part mirrorRoot; bool hasIR;
			Vector3 parentPosition = Vector3.zero; Quaternion parentRotation = Quaternion.identity;
			Vector3 symmetryParentPosition = Vector3.zero; Quaternion symmetryParentRotation = Quaternion.identity;

			FindMirrorInfo(part.parent, out mirrorRoot, out hasIR, ref parentPosition, ref parentRotation, ref symmetryParentPosition, ref symmetryParentRotation);

			hasIR = hasIR | IsIR(part);

			if(!hasIR)
			{
				RestoreMirroring(part, mirrorRoot);
			}
			else
			{
				RestoreMirroringIR(part, mirrorRoot,
					parentPosition, parentRotation,
					symmetryParentPosition, symmetryParentRotation);
			}
		}
	}

	public class ModuleIREditorHelper : PartModule
	{
		public bool needsVerification;

		public AttachNode attachNodeUsed;

		public Vector3 localPosition;
		public Quaternion localRotation;

		public override bool OnWillBeMirrored(ref Quaternion rotation, AttachNode selPartNode, Part partParent)
		{
			bool isMine = (part.srfAttachNode == selPartNode);

			int i = 0;
			while(!isMine && (i < part.attachNodes.Count))
				isMine = (part.attachNodes[i++] == selPartNode);

			if(isMine)
			{
				needsVerification = true;
				attachNodeUsed = selPartNode;
			}
			else
			{
				ModuleIREditorHelper m = part.symmetryCounterparts[0].GetComponent<ModuleIREditorHelper>();

				if(!m)
					Logger.Log("part not initialized", Logger.Level.Error);

				m.needsVerification = true;
				m.attachNodeUsed = selPartNode;
			}

			return false;
		}
	}
}
