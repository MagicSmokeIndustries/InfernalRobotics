using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MuMech;
using UnityEngine;

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
                //pm.resized(factor.absolute.linear);
                pm.resized();
            }
        }
    }
}
