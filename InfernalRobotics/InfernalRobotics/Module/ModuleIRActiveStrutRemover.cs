using System;

namespace InfernalRobotics_v3.Module
{
	/*
	 * this class removes invisible debris that would exist after undocking a robotstrut
	 */

	public class ModuleIRActiveStrutRemover : PartModule
	{
		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			GameEvents.onPartDeCoupleComplete.Add(OnPartDeCoupleComplete);
		}

		public void OnDestroy()
		{
			GameEvents.onPartDeCoupleComplete.Remove(OnPartDeCoupleComplete);
		}

		public void OnPartDeCoupleComplete(Part p)
		{
			if((p == part) && (p == p.vessel.rootPart))
				p.vessel.Die();
		}
	}
}
