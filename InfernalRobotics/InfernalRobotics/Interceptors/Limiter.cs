using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfernalRobotics_v3.Interceptors
{
	class Limiter : Interfaces.ILimiter
	{
		public bool SetCommand(ref float p_TargetPosition, ref float p_TargetSpeed, ref float p_Acceleration)
		{
			return false;
		}
	}
}
