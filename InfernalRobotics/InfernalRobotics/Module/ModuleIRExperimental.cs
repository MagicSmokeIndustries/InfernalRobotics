using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using KSP.IO;
using UnityEngine;

using InfernalRobotics_v3.Utility;

namespace InfernalRobotics_v3.Module
{
	public class ModuleIRExperimental : PartModule
	{
		public ModuleIRExperimental()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			DebugInit();

	//		GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
	//		GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(state == StartState.Editor) // FEHLER, müsste ich None abfangen?? wieso sollte das je aufgerufen werden???
				return;
		}

		public void OnDestroy()
		{
//			GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
//			GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
		}

/*		public void OnVesselGoOnRails(Vessel v)
		{
			if(part.vessel != v)
				return;
		}

		public void OnVesselGoOffRails(Vessel v)
		{
			if(part.vessel != v)
				return;
		}
*/
		////////////////////////////////////////
		// Functions

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
		}

		////////////////////////////////////////
		// Properties

		////////////////////////////////////////
		// Status

// FEHLER, den Status irgendwo anzeigen? -> ja, das tun... echt jetzt

		////////////////////////////////////////
		// Settings

		////////////////////////////////////////
		// Characteristics - values 'by design'

		////////////////////////////////////////
		// Scaling

		////////////////////////////////////////
		// Input

		////////////////////////////////////////
		// Editor

		////////////////////////////////////////
		// Context Menu

		private void UpdateUI()
		{
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Scale Rail", active = true)]
		public void ScaleRail()
		{
			Transform t = KSPUtil.FindInPartModel(part.transform, "Rail");

			t.localScale = new Vector3(3f * t.localScale.x, t.localScale.y, t.localScale.z);
		}

		////////////////////////////////////////
		// IRescalable
/*
		// Tweakscale support
		[KSPEvent(guiActive = false, active = true)]
		void OnPartScaleChanged(BaseEventDetails data)
		{
			OnRescale(new ScalingFactor(data.Get<float>("factorAbsolute"), data.Get<float>("factorRelative")));
		}

		public void OnRescale(ScalingFactor factor)
		{
			ModuleIRLEE prefab = part.partInfo.partPrefab.GetComponent<ModuleIRLEE>();

			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);

			UpdateUI();
		}
*/
		////////////////////////////////////////
		// IModuleInfo
/*
		string IModuleInfo.GetModuleTitle()
		{
			return "Latching End Effector";
		}

		string IModuleInfo.GetInfo()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Attach strength (catched): {0:F0}\n", catchedBreakForce);
			sb.AppendFormat("Attach strength (latched): {0:F0}\n", latchedBreakForce);

			if(electricChargeRequiredLatching != 0f)
			{
				sb.Append("\n\n");
				sb.Append("<b><color=orange>Requires:</color></b>\n");
				
				if(electricChargeRequiredLatching != 0f)
					sb.AppendFormat("- <b>Electric Charge:</b> {0:F0}\n  (for latching)", electricChargeRequiredLatching);
			}

			return sb.ToString();
		}

		Callback<Rect> IModuleInfo.GetDrawModulePanelCallback()
		{
			return null;
		}

		string IModuleInfo.GetPrimaryField()
		{
			return null;
		}
*/
		////////////////////////////////////////
		// Debug

/* FEHLER, vielleicht später...
  public void OnKISAction(Dictionary<string, object> eventData) {
    var action = eventData["action"].ToString();
    var tgtPart = eventData["targetPart"] as Part;

    if(action == "Store" || action == "AttachStart" || action == "DropEnd") {
      DetachGrapple();
    }
    if(action == "AttachEnd") {
      DetachGrapple();
      if(tgtPart == null) {
        AttachStaticGrapple();
      }
    }
  }
*/

		////////////////////////////////////////
		// Debug

		private MultiLineDrawer ld;

		private void DebugInit()
		{
			ld = new MultiLineDrawer();
			ld.Create(null);
		}

		public void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			ld.Draw(idx, p_from, p_from + p_vector);
		}
	}

//// Zeugs zum umarbeiten

/*
	[KSPField(isPersistant = true)]
	public bool crossfeed = true;

	public Transform nodeTransform;
	public Transform controlTransform;

	public bool IsDisabled
	{
		get
		{
			int result;
			if(fsm != null)
			{
				if(fsm.Started)
				{
					result = ((fsm.CurrentState == st_disabled) ? 1 : 0);
					goto IL_004c;
				}
			}
			result = 0;
			goto IL_004c;
			IL_004c:
			return (byte)result != 0;
		}
	}

		nodeTransform = base.part.FindModelTransform(nodeTransformName);



	public override void OnActive()
	{
		if(!staged)
		{
			return;
		}
		if(!stagingEnabled)
		{
			return;
		}
		if(base.Events["Decouple"].active)
		{
			Decouple();
			return;
		}
		return;
	}
/*
	[KSPAction("#autoLOC_6001447")]
	public void MakeReferenceToggle(KSPActionParam act)
	{
		MakeReferenceTransform();
	}

	[KSPEvent(guiActive = true, guiName = "#autoLOC_6001447")]
	public void MakeReferenceTransform()
	{
		base.part.SetReferenceTransform(controlTransform);
		base.vessel.SetReferenceTransform(base.part);
	}
*/


}
