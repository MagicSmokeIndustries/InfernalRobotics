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
		protected static EditorEventLogic Instance;
		
		private void Awake()
		{
			if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
				Instance = this;
			}
			else
				Instance = null;
		}

		private void OnDestroy()
		{
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

		private void OnEditorPartEvent(ConstructionEventType evt, Part p)
		{
			switch(evt)
			{
			case ConstructionEventType.PartAttached:
				{
					Part _p = p;
					int i = 0;

				next:
					ModuleIRServo_v3 s = _p.GetComponent<ModuleIRServo_v3>();
					if(s)
						s.OnEditorAttached(true);

					OnEditorAttached(_p);

					if(i < p.symmetryCounterparts.Count)
					{
						_p = p.symmetryCounterparts[i++];
						goto next;
					}
				}
				break;

			case ConstructionEventType.PartDetached:
				{
					ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
					if(s)
						s.OnEditorDetached(true);

					OnEditorDetached(p);
				}
				break;

			case ConstructionEventType.PartCopied:
				{
					ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
					if(s)
						s.MoveChildren(s.commandedPosition);
				}
				break;

			case ConstructionEventType.PartRootSelected:
				OnEditorRootSelected(p);
				break;
			}
		}
	}
}
