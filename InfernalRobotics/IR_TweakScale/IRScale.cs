using MuMech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace IR_TweakScale
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    internal class MyEditorRegistrationAddon : TweakScale.RescalableRegistratorAddon
    {
        public override void OnStart()
        {
            MyUpdater.Register();
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class MyFlightRegistrationAddon : TweakScale.RescalableRegistratorAddon
    {
        public override void OnStart()
        {
            MyUpdater.Register();
        }
    }

    class MyUpdater : TweakScale.IRescalable
    {
        internal static void Register()
        {
            MonoBehaviour.print("Attempting to register");
            TweakScale.TweakScaleUpdater.RegisterUpdater((MuMechToggle a) => new MyUpdater(a));
        }

        MuMechToggle pm;

        public MyUpdater(MuMechToggle pm)
        {
            this.pm = pm;
        }

        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            pm.resized();
        }
    }
}
