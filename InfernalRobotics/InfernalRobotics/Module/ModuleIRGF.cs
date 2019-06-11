using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using KSP.IO;
using UnityEngine;
using TweakScale;

using InfernalRobotics_v3.Effects;
using InfernalRobotics_v3.Utility;

namespace InfernalRobotics_v3.Module
{
	public class ModuleIRGF : PartModule, ITargetable, IRescalable, IModuleInfo
	{
		public BaseEvent evtSetAsTarget;
		public BaseEvent evtUnsetTarget;
		// FEHLER, ist nur das Gegenstück (findbar) zum LEE... tut eigentlich gar nichts... ok, ein Menü anzeigen um das undock auszulösen und evtl. für's deactivate oder so... aber... na ja


		[KSPField(isPersistant = false)] public string dockingNodeTransformName = "dockingNode";
		public Transform nodeTransform;

		[KSPField(isPersistant = false)] public string dockingNodeName = "top";

		[KSPField(isPersistant = false)] public bool gendered = true;
		[KSPField(isPersistant = false)] public bool genderFemale = false;

// Status noch führen und sowas wie ein enable-disable und undock vom port her erlauben oder so... ja gut, undock evtl. nicht...

public bool isReady
{
	get { return true; }
}


		public ModuleIRGF()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			nodeTransform = part.FindModelTransform(dockingNodeTransformName);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(state == StartState.Editor) // FEHLER, müsste ich None abfangen?? wieso sollte das je aufgerufen werden???
				return;

			nodeTransform = part.FindModelTransform(dockingNodeTransformName);
		}

// FEHLER, hier fehlt sicher noch etwas

		////////////////////////////////////////
		// ITargetable

		public Transform GetTransform()
		{
			return nodeTransform;
		}

		public Vector3 GetObtVelocity()
		{
			return vessel.obt_velocity;
		}

		public Vector3 GetSrfVelocity()
		{
			return vessel.srf_velocity;
		}

		public Vector3 GetFwdVector()
		{
			return nodeTransform.forward;
		}

		public Vessel GetVessel()
		{
			return vessel;
		}

		public string GetName()
		{
			return "GF on " + vessel.vesselName;
				// FEHLER, schlaueren Namen wählen, sonst kann man die Teils ja nie unterscheiden -> bzw. unterstützen, dass man sie benennen kann
		}

		public string GetDisplayName()
		{
			return GetName();
		}

		public Orbit GetOrbit()
		{
			return vessel.orbit;
		}

		public OrbitDriver GetOrbitDriver()
		{
			return vessel.orbitDriver;
		}

		public VesselTargetModes GetTargetingMode()
		{
			return VesselTargetModes.DirectionVelocityAndOrientation;
		}

		public bool GetActiveTargetable()
		{
			return false;
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f, guiName = "Set As Target")]
		public void SetAsTarget()
		{
			FlightGlobals.fetch.SetVesselTarget(this);
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f, guiName = "Unset As Target")]
		public void UnsetTarget()
		{
			FlightGlobals.fetch.SetVesselTarget(null);
		}

		////////////////////////////////////////
		// IRescalable

		public void OnRescale(ScalingFactor factor)
		{
			ModuleIRGF prefab = part.partInfo.partPrefab.GetComponent<ModuleIRGF>();

/*			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);

			forceNeeded = prefab.forceNeeded * factor.absolute.linear;
 			partBreakForce = partBreakForce * factor.relative.linear;
 			groundBreakForce = groundBreakForce * factor.relative.linear;

			electricChargeRequiredIdle = prefab.electricChargeRequiredIdle * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);
			electricChargeRequiredConnected = prefab.electricChargeRequiredConnected * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);

			UpdateUI();*/
		}

		////////////////////////////////////////
		// IModuleInfo

		string IModuleInfo.GetModuleTitle()
		{
			return "Grapple Fixture";
		}

		string IModuleInfo.GetInfo()
		{
			return "";
		}

		Callback<Rect> IModuleInfo.GetDrawModulePanelCallback()
		{
			return null;
		}

		string IModuleInfo.GetPrimaryField()
		{
			return null;
		}


	}
}
