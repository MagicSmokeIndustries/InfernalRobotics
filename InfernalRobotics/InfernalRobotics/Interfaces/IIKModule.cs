using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfernalRobotics_v3.Interfaces
{
	public interface IIKModule
	{
		void SelectGroup(Interfaces.IServoGroup g);

		void SetLimiter(bool active);
		void SetDirectMode(bool active);
		void SelectEndEffector();
		void Action1();
		void Action2();
	}
}
