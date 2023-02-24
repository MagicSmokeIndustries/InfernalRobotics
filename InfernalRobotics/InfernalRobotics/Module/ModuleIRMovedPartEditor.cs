using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using InfernalRobotics_v3.Utility;

namespace InfernalRobotics_v3.Module
{
	public class ModuleIRMovedPartEditor2 : PartModule
	{
		public static void InitializePart(Part part)
		{
			ModuleIRMovedPartEditor2 m = part.GetComponent<ModuleIRMovedPartEditor2>();
			if(!m)
				m = (ModuleIRMovedPartEditor2)part.AddModule("ModuleIRMovedPartEditor2");

			m.localPosition = part.transform.localPosition;
			m.localRotation = part.transform.localRotation;
		}

		public Vector3 localPosition;
		public Quaternion localRotation;
	}
}
