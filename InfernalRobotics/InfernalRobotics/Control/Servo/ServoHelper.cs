using System.Collections.Generic;
using System.Linq;
using InfernalRobotics_v3.Module;

namespace InfernalRobotics_v3.Control.Servo
{
	public static class ServoHelper
	{
		public static IEnumerable<AvailablePart> InfernalParts(this List<AvailablePart> parts)
		{
			return (from avPart in parts.Where(p => p.partPrefab) 
					let moduleItem = avPart.partPrefab.GetComponent<ModuleIRServo_v3>()
					where moduleItem
					select avPart).ToList();
		}

		public static IList<IServo> ToServos(this Vessel vessel)
		{
			return BuildServos(vessel.FindPartModulesImplementing<ModuleIRServo_v3>());
		}

		public static IList<IServo> ToServos(this Part part)
		{
			return BuildServos(part.Modules.OfType<ModuleIRServo_v3>());
		}

		public static IList<IServo> GetChildServos(this Part part)
		{
			return BuildServos(part.GetComponentsInChildren<ModuleIRServo_v3>());
		}

		private static IList<IServo> BuildServos(IEnumerable<ModuleIRServo_v3> toggles)
		{
			System.Collections.Generic.List<IServo> l = new List<IServo>();
			l.AddRange(toggles.Select(toggle => (IServo)toggle).ToList());
			return l;
		}
	}
}
