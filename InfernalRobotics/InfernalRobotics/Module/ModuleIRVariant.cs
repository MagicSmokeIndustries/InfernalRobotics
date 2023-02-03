using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;

namespace InfernalRobotics_v3.Module
{
	public struct ScalingFactor
	{
		public struct FactorSet
		{
			private float _linear;

			public FactorSet(float factor)
			{
				_linear = factor;
			}

			public float linear { get { return _linear; } }
			public float quadratic { get { return _linear * _linear; } }
			public float cubic { get { return _linear * _linear * _linear; } }
		}

		FactorSet _absolute;
	//	FactorSet _relative;

		public ScalingFactor(float abs/*, float rel*/)
		{
			_absolute = new FactorSet(abs);
	//		_relative = new FactorSet(rel);
		}

		public FactorSet absolute { get { return _absolute; } }
	//	public FactorSet relative { get { return _relative; } }
	}

	public class ModuleIRVariant : PartModule
	{
		private struct IRVariant
		{
			public string name;
			public string displayName;
			public float factor;
		};

		private List<IRVariant> variantList;

		public float currentFactor = 1f;

		////////////////////////////////////////
		// Callbacks

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(HighLogic.LoadedSceneIsEditor)
			{
				if(variantList == null)
				{
					ModuleIRVariant prefabModule = (ModuleIRVariant)part.partInfo.partPrefab.Modules["ModuleIRVariant"];
					variantList = prefabModule.variantList;

					if(variantIndex == -1)
						variantIndex = prefabModule.variantIndex;
				}

				AttachContextMenu();

				UpdateUI();
			}
		}

		public void OnDestroy()
		{
			DetachContextMenu();
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if(variantList == null)
			{
				if((part.partInfo != null) && (part.partInfo.partPrefab != null))
				{
					ModuleIRVariant prefabModule = (ModuleIRVariant)part.partInfo.partPrefab.Modules["ModuleIRVariant"];
					if(prefabModule != null)
					{
						variantList = prefabModule.variantList;
						if(variantIndex == -1)
							variantIndex = prefabModule.variantIndex;
					}
				}
				else // I assume, that I'm the prefab then
				{
					string defaultVariant = node.GetValue("defaultVariant");

					variantList = new List<IRVariant>();

					ConfigNode[] variantnodes = node.GetNodes("VARIANT");
					for(int i = 0; i < variantnodes.Length; i++)
					{
						ConfigNode variantnode = variantnodes[i];

						IRVariant variant = new IRVariant();

						variantnode.TryGetValue("name", ref variant.name);
						variantnode.TryGetValue("displayName", ref variant.displayName);
						variantnode.TryGetValue("scale", ref variant.factor);

						if(variant.name.Equals(defaultVariant))
							variantIndex = variantList.Count;

						variantList.Add(variant);
					}
				}
			}

			float factor = new float();
			if(node.TryGetValue("currentFactor", ref factor))
			{
				RefreshVariant(factor);
			}
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("currentFactor", currentFactor);
		}

		////////////////////////////////////////
		// Functions

		private bool HasTweakScale()
		{
	//		for(int i = 0; i < part.Modules.Count; i++)
	//		{
	//			if(part.Modules[i].name == "TweakScale")
	//				return true;
	//		}
			return false;
		}

		private void MoveNode(AttachNode node, AttachNode baseNode, float factor)
        {
			Vector3 deltaPos = node.position;

			node.radius = baseNode.radius * factor;

			node.position = baseNode.position * factor;
			node.originalPosition = baseNode.originalPosition * factor;

			deltaPos = node.position - deltaPos;

			if(node.attachedPart != null)
			{
				if(node.attachedPart == part.parent)
					part.transform.Translate(-deltaPos, part.transform);
				else
				{
					Vector3 offset = node.attachedPart.attPos * (factor - 1);
					node.attachedPart.transform.Translate(deltaPos + offset, part.transform);
					node.attachedPart.attPos *= factor;
				}
			}
        }

		private void ScaleDragCubes(float factor)
		{
			for(int ic = 0; ic < part.DragCubes.Cubes.Count; ic++)
			{
				DragCube dragCube = part.DragCubes.Cubes[ic];
				dragCube.Size *= factor;

				for(int i = 0; i < dragCube.Area.Length; i++)
					dragCube.Area[i] *= factor * factor;

				for (int i = 0; i < dragCube.Depth.Length; i++)
					dragCube.Depth[i] *= factor;
			}
			part.DragCubes.ForceUpdate(true, true);
		}

		public void RefreshVariant(float factor)
		{
			if(HasTweakScale())
				return; // if someone is using TweakScale, we don't do anything
// FEHLER, mit TweakScale ginge sowieso nix -> andere Lösung finden um die AttachNodes und Kinder zu drehen -> direkt im OnRescale vom Servo oder so

			part.rescaleFactor = part.partInfo.partPrefab.rescaleFactor * factor;

			Transform modelTransform = part.transform.Find("model");
            if(modelTransform != null)
				modelTransform.localScale = part.partInfo.partPrefab.transform.Find("model").transform.localScale * factor;

			ModuleIRServo_v3 servo_ = part.GetComponent<ModuleIRServo_v3>();

			float cmdp = (servo_ != null) ? servo_.CommandedPosition : 0f;

			if(HighLogic.LoadedSceneIsEditor)
			{
				if(servo_ != null)
				{
					servo_.EditorMiniInit();

					if(!servo_.bDetachedByEditor)
						servo_.EditorSetTo(servo_.DefaultPosition);
					else
						servo_.EditorSetSpecial(); // FEHLER, ist das noch nötig?
				}

				for(int i = 0; i < part.attachNodes.Count; i++)
				{
					AttachNode node = part.attachNodes[i];

					AttachNode prefabNode = null;
					for(int j = 0; j < part.partInfo.partPrefab.attachNodes.Count; j++)
					{
						if(part.partInfo.partPrefab.attachNodes[j].id == node.id)
						{ prefabNode = part.partInfo.partPrefab.attachNodes[j]; break; }
					}

					if(node.icon != null)
						node.icon.transform.localScale = Vector3.one * prefabNode.radius * ((prefabNode.size == 0) ? ((float)prefabNode.size + 0.5f) : prefabNode.size);

                    MoveNode(node, prefabNode, factor);
				}

				if(part.srfAttachNode != null)
					MoveNode(part.srfAttachNode, part.partInfo.partPrefab.srfAttachNode, factor);

				for(int i = 0; i < part.children.Count; i++)
				{
					Part child = part.children[i];

					if(child.srfAttachNode == null || child.srfAttachNode.attachedPart != part)
						continue;

					Vector3 attachedPosition = child.transform.localPosition + child.transform.localRotation * child.srfAttachNode.position;
					Vector3 targetPosition = attachedPosition * (factor / currentFactor);
					child.transform.Translate(targetPosition - attachedPosition, part.transform);
				}
			}

			ScaleDragCubes(factor / currentFactor);

			ModuleIRServo_v3 servo = part.GetComponent<ModuleIRServo_v3>();

			if(servo != null)
				servo.OnRescale(new ScalingFactor(factor));

			currentFactor = factor;

			if(HighLogic.LoadedSceneIsEditor && (servo_ != null))
			{
				if(!servo_.bDetachedByEditor)
					servo_.EditorSetTo(cmdp);
				else
					servo_.EditorResetSpecial(cmdp);
			}
		}

		////////////////////////////////////////
		// Settings

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Size"),
			UI_ChooseOption(scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.None)]
		public int variantIndex = -1;

		private void onChanged_variantIndex(object o)
		{
			float factor = variantList[variantIndex].factor;

			RefreshVariant(factor);
			UpdateUI();

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
			{
				ModuleIRVariant variant = part.symmetryCounterparts[i].GetComponent<ModuleIRVariant>();
				
				variant.variantIndex = variantIndex;

				variant.RefreshVariant(factor);
				variant.UpdateUI();
			}

			EditorLogic.fetch.SetBackup();

			GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}

		////////////////////////////////////////
		// Context Menu

		private void AttachContextMenu()
		{
			Fields["variantIndex"].OnValueModified += onChanged_variantIndex;

			List<string> m = new List<string>(variantList.Count);
			for(int i = 0; i < variantList.Count; i++)
				m.Add(variantList[i].displayName);

			((UI_ChooseOption)Fields["variantIndex"].uiControlEditor).options = m.ToArray();

			if(HasTweakScale())
				Fields["variantIndex"].guiActiveEditor = false; // if someone is using TweakScale, we don't show up
		}

		private void DetachContextMenu()
		{
			Fields["variantIndex"].OnValueModified -= onChanged_variantIndex;
		}

		private void UpdateUI()
		{}
	}
}
