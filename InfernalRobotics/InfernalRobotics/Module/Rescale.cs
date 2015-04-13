using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using InfernalRobotics.Module;

namespace InfernalRobotics
{
    //18.3
    class IRRescale
    {
        class MyUpdater : TweakScale.IRescalable<MuMechToggle>
        {
            MuMechToggle pm;

            public MyUpdater(MuMechToggle pm)
            {
                this.pm = pm;
            }

            public void OnRescale(TweakScale.ScalingFactor factor)
            {
                pm.OnRescale(factor.relative.linear);
            }
        }
    }
}