using System;
using InfernalRobotics.Control;
using InfernalRobotics.Control.Servo;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace InfernalRobotics.Command
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ServoControllerFlight : ServoController
    {
        public override string AddonName { get { return this.name; } }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ServoControllerEditor : ServoController
    {
        public override string AddonName { get { return this.name; } }
    }

    public class ServoController : MonoBehaviour
    {
        public virtual String AddonName { get; set; }

        protected static bool UseElectricCharge = true;
        protected static ServoController ControllerInstance;
        
        public List<ControlGroup> ServoGroups; 
        private int partCounter;
        private int loadedVesselCounter = 0;

        public static ServoController Instance { get { return ControllerInstance; } }

        public static bool APIReady { get { return ControllerInstance != null && ControllerInstance.ServoGroups != null && ControllerInstance.ServoGroups.Count > 0; } }
        
        static ServoController()
        {
        }

        public static void MoveServo(ControlGroup from, ControlGroup to, IServo servo)
        {
            to.AddControl(servo);
            from.RemoveControl(servo);
        }

        public static void AddServo(IServo servo)
        {
            if (!Instance)
                return;
            
            if (Instance.ServoGroups == null)
                Instance.ServoGroups = new List<ControlGroup>();

            ControlGroup controlGroup = null;

            if (!string.IsNullOrEmpty(servo.Group.Name))
            {
                foreach (ControlGroup cg in Instance.ServoGroups)
                {
                    if (servo.Group.Name == cg.Name)
                    {
                        controlGroup = cg;
                        break;
                    }
                }
                if (controlGroup == null)
                {
                    var newGroup = new ControlGroup(servo);
                    Instance.ServoGroups.Add(newGroup);
                    Logger.Log("[ServoController] AddServo adding new ControlGroup", Logger.Level.Debug);
                    return;
                }
            }
            if (controlGroup == null)
            {
                if (Instance.ServoGroups.Count < 1)
                {
                    Instance.ServoGroups.Add(new ControlGroup());
                }
                controlGroup = Instance.ServoGroups[Instance.ServoGroups.Count - 1];
            }

            controlGroup.AddControl(servo);

            Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);
        }

        public static void RemoveServo(IServo servo)
        {
            if (!Instance)
                return;

            if (Instance.ServoGroups == null)
                return;

            int num = 0;
            foreach (ControlGroup group in Instance.ServoGroups)
            {
                if (group.Name == servo.Group.Name)
                {
                    group.RemoveControl(servo);
                }
                num += group.Servos.Count;
            }

            if (Gui.WindowManager.Instance)
            {
                //disable GUI when last servo removed
                Gui.WindowManager.Instance.GUIEnabled &= num > 0;
            }
            Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> hostTarget)
        {
            Part part = hostTarget.host;

            if ((EditorLogic.fetch.ship.parts.Count >= partCounter) &&
                (EditorLogic.fetch.ship.parts.Count != partCounter))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (var p in part.GetChildServos())
                    {
                        AddServo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }
            if ((EditorLogic.fetch.ship.parts.Count == 0) && (partCounter == 0))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (var p in part.GetChildServos())
                    {
                        AddServo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }

            Logger.Log("[ServoController] OnPartAttach finished successfully", Logger.Level.Debug);
        }

        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> hostTarget)
        {
            Part part = hostTarget.target;
            try
            {
                var servos = part.ToServos();
                foreach (var temp in servos)
                {
                    temp.Mechanism.Reset();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("[ServoController] OnPartRemove Error: " + ex, Logger.Level.Debug);
            }

            foreach (var p in part.GetChildServos())
            {
                RemoveServo(p);
            }
            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;

            Logger.Log("[ServoController] OnPartRemove finished successfully", Logger.Level.Debug);
        }

        private void RebuildServoGroupsEditor(ShipConstruct ship = null)
        {
            if(ship==null)
                ship = EditorLogic.fetch.ship;

            ServoGroups = null;

            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in ship.Parts)
            {
                foreach (var servo in p.ToServos())
                {
                    if (!groupMap.ContainsKey(servo.Group.Name))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.Group.Name] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.Group.Name]];
                        g.AddControl(servo);
                    }
                }
            }

            if (groups.Count > 0)
                ServoGroups = groups;
        }
       
        private void OnEditorShipModified(ShipConstruct ship)
        {
            RebuildServoGroupsEditor(ship);

            Gui.WindowManager.guiRebuildPending = true; //this should force an UI rebuild on first update

            if(Gui.IRBuildAid.IRBuildAidManager.Instance)
                Gui.IRBuildAid.IRBuildAidManager.Reset();
            
            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;
            Logger.Log("[ServoController] OnEditorShipModified finished successfully", Logger.Level.Debug);
        }

        private void OnEditorRestart()
        {
            ServoGroups = null;

            Gui.WindowManager.guiRebuildPending = true; //this should force an UI rebuild on first update

            if (Gui.IRBuildAid.IRBuildAidManager.Instance)
                Gui.IRBuildAid.IRBuildAidManager.Reset();

            Logger.Log ("OnEditorRestart called", Logger.Level.Debug);
        }

        private void OnEditorLoad(ShipConstruct s, KSP.UI.Screens.CraftBrowserDialog.LoadType t)
        {
            OnEditorShipModified (s);
            
            Logger.Log ("OnEditorLoad called", Logger.Level.Debug);
        }
        /// <summary>
        /// Rebuilds the servo groups. Only works in flight.
        /// </summary>
        private void RebuildServoGroupsFlight()
        {
            ServoGroups = new List<ControlGroup>();

            for(int i=0; i<FlightGlobals.Vessels.Count; i++)
            {
                var vessel = FlightGlobals.Vessels [i];

                if (!vessel.loaded)
                    continue;
                
                var groups = new List<ControlGroup>();
                var groupMap = new Dictionary<string, int>();

                foreach(var servo in vessel.ToServos())
                {
                    if (!groupMap.ContainsKey(servo.Group.Name))
                    {
                        groups.Add(new ControlGroup(servo, vessel));
                        groupMap[servo.Group.Name] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.Group.Name]];
                        g.AddControl(servo);
                    }
                }

                ServoGroups.AddRange (groups);
            }

            if (ServoGroups.Count == 0)
                ServoGroups = null;

            Gui.WindowManager.guiRebuildPending = true; //this should force an UI rebuild on the next update

        }

        private void OnVesselChange(Vessel v)
        {
            Logger.Log(string.Format("[ServoController] vessel {0}", v.name));

            RebuildServoGroupsFlight ();

            foreach (var servo in v.ToServos())
            {
                servo.RawServo.SetupJoints();
            }

            Logger.Log("[ServoController] OnVesselChange finished successfully", Logger.Level.Debug);
        }

        private void OnVesselPartCountModified(Vessel v)
        {
            RebuildServoGroupsFlight ();
        }

        private void OnVesselLoaded (Vessel v)
        {
            Logger.Log("[ServoController] OnVesselLoaded, v=" + v.GetName(), Logger.Level.Debug);
            RebuildServoGroupsFlight ();
        }

        private void OnVesselUnloaded (Vessel v)
        {
            Logger.Log("[ServoController] OnVesselUnloaded, v=" + v.GetName(), Logger.Level.Debug);
            RebuildServoGroupsFlight ();
        }

        private void Awake()
        {
            Logger.Log("[ServoController] awake, AddonName = " + this.AddonName);

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT)
            {
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountModified);
                GameEvents.onVesselLoaded.Add (OnVesselLoaded);
                GameEvents.onVesselDestroy.Add (OnVesselUnloaded);
                GameEvents.onVesselGoOnRails.Add (OnVesselUnloaded);
                ControllerInstance = this;
            }
            else if (scene == GameScenes.EDITOR)
            {
                GameEvents.onPartAttach.Add(OnPartAttach);
                GameEvents.onPartRemove.Add(OnPartRemove);
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                GameEvents.onEditorLoad.Add(OnEditorLoad);
                GameEvents.onEditorRestart.Add(OnEditorRestart);
                ControllerInstance = this;
            }
            else
            {
                ControllerInstance = null;
            }

            Logger.Log("[ServoController] awake finished successfully, AddonName = " + this.AddonName, Logger.Level.Debug);
        }

        /// <summary>
        /// Sets the wheel auto-struting for the Vessel v. 
        /// In flight mode we need to set to false before moving 
        /// the joint and to true aferwards
        /// </summary>
        public static void SetWheelAutoStruts(bool value, Vessel v)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            /*foreach(var p in v.Parts)
            {
                if(!value)
                {
                    p.autoStrutMode = Part.AutoStrutMode.Off;
                    p.UpdateAutoStrut ();
                }
            }
*/
            /*var activeVesselWheels = v.FindPartModulesImplementing<ModuleWheelBase>();
            foreach(var mwb in activeVesselWheels)
            {
                if (value)
                {
                    if(!mwb.autoStrut) //we only need to Cycle once
                        mwb.CycleWheelStrut();
                }
                else
                    mwb.ReleaseWheelStrut();

                mwb.autoStrut = value;

            }*/
        }


        private void FixedUpdate()
        {
            //because OnVesselDestroy and OnVesselGoOnRails seem to only work for active vessel I had to build this stupid workaround
            if(HighLogic.LoadedSceneIsFlight)
            {
                if(FlightGlobals.Vessels.Count(v => v.loaded) != loadedVesselCounter)
                {
                    RebuildServoGroupsFlight ();
                    loadedVesselCounter = FlightGlobals.Vessels.Count(v => v.loaded);
                }

                if (ServoGroups == null)
                    return;

                //check if all servos stopped running and enable the struts, otherwise disable wheel autostruts
                var anyActive = new Dictionary<Vessel, bool>();

                foreach(var g in ServoGroups)
                {
                    if (!anyActive.ContainsKey(g.Vessel))
                        anyActive.Add(g.Vessel, false);
                    
                    foreach(var s in g.Servos)
                    {
                        if (s.RawServo.Interpolator.Active)
                        {
                            anyActive[g.Vessel] = true;
                            break;
                        }
                    }
                }
                foreach(var pair in anyActive)
                {
                    SetWheelAutoStruts(!pair.Value, pair.Key);
                }
            }
        }

        private void OnDestroy()
        {
            Logger.Log("[ServoController] destroy", Logger.Level.Debug);

            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onVesselWasModified.Remove(OnVesselPartCountModified);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            GameEvents.onEditorLoad.Remove(OnEditorLoad);
            GameEvents.onEditorRestart.Remove(OnEditorRestart);

            GameEvents.onVesselLoaded.Remove (OnVesselLoaded);
            GameEvents.onVesselDestroy.Remove (OnVesselUnloaded);
            GameEvents.onVesselGoOnRails.Remove (OnVesselUnloaded);
            Logger.Log("[ServoController] OnDestroy finished successfully", Logger.Level.Debug);
        }

        //TODO: move this to a separate file and extend if necessary
        //this will require changes in API
        public class ControlGroup
        {
            private bool stale;
            private float totalElectricChargeRequirement;
            private string speed;
            private string forwardKey;
            private string reverseKey;
            private readonly List<IServo> servos;
            private readonly Vessel vessel;

            public ControlGroup(IServo servo, Vessel v)
                : this(servo)
            {
                vessel = v;
            }

            public ControlGroup(IServo servo)
                : this()
            {
                Name = servo.Group.Name;
                ForwardKey = servo.Input.Forward;
                ReverseKey = servo.Input.Reverse;
                Speed = servo.RawServo.customSpeed.ToString("g");
                servos.Add(servo);
            }

            public ControlGroup()
            {
                servos = new List<IServo>();
                Expanded = false;
                Name = "New Group";
                ForwardKey = string.Empty;
                ReverseKey = string.Empty;
                Speed = "1";
                MovingNegative = false;
                MovingPositive = false;
                ButtonDown = false;
                stale = true;
            }

            public bool ButtonDown { get; set; }

            public bool Expanded { get; set; }

            private string name = "New Group";
            public string Name 
            { 
                get { return this.name; } 
                set { 
                    this.name = value;
                    if (this.servos != null && this.servos.Count > 0)
                        this.servos.ForEach(s => s.Group.Name = this.name);
                } 
            }

            public bool MovingNegative { get; set; }

            public bool MovingPositive { get; set; }

            public IList<IServo> Servos
            {
                get { return servos; }
            }

            public Vessel Vessel
            {
                get { return vessel; }
            }

            public string ForwardKey
            {
                get { return forwardKey; }
                set
                {
                    forwardKey = value;
                    PropogateForward();
                }
            }

            public string ReverseKey
            {
                get { return reverseKey; }
                set
                {
                    reverseKey = value;
                    PropogateReverse();
                }
            }

            public string Speed
            {
                get { return speed; }
                set
                {
                    speed = value;
                    PropogateSpeed();
                }
            }

            public float TotalElectricChargeRequirement
            {
                get
                {
                    if (stale) Freshen();
                    return totalElectricChargeRequirement;
                }
            }

            public void AddControl(IServo control)
            {
                servos.Add(control);
                control.Group.Name = Name;
                control.Input.Forward = ForwardKey;
                control.Input.Reverse = ReverseKey;
                stale = true;
            }

            public void RemoveControl(IServo control)
            {
                servos.Remove(control);
                stale = true;
            }

            public void MoveRight()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Motor.MoveRight();
                    }
                }
            }

            public void MoveLeft()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Motor.MoveLeft();
                    }
                }
            }

            public void MoveCenter()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Motor.MoveCenter();
                    }
                }
            }

            public void MoveNextPreset()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Preset.MoveNext();
                    }
                }
            }

            public void MovePrevPreset()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Preset.MovePrev();
                    }
                }
            }

            public void Stop()
            {
                MovingNegative = false;
                MovingPositive = false;

                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Motor.Stop();
                    }
                }
            }

            private void Freshen()
            {
                if (Servos == null) return;

                if (UseElectricCharge)
                {
                    float chargeRequired = Servos.Where (s => s.Mechanism.IsFreeMoving == false).Sum (s => s.ElectricChargeRequired);
                    foreach (var servo in Servos)
                    {
                        servo.Group.ElectricChargeRequired = chargeRequired;
                    }
                    totalElectricChargeRequirement = chargeRequired;
                }

                stale = false;
            }

            private void PropogateForward()
            {
                if (Servos == null) return;

                foreach (var servo in Servos)
                {
                    servo.Input.Forward = ForwardKey;
                }
            }

            private void PropogateReverse()
            {
                if (Servos == null) return;

                foreach (var servo in Servos)
                {
                    servo.Input.Reverse = ReverseKey;
                }
            }

            private void PropogateSpeed()
            {
                if (Servos == null) return;

                float parsedSpeed;
                var isFloat = float.TryParse(speed, out parsedSpeed);
                if (!isFloat) return;

                foreach (var servo in Servos)
                {
                    servo.RawServo.customSpeed = parsedSpeed;
                }
            }

            public void RefreshKeys()
            {
                foreach (var servo in Servos)
                {
                    servo.Input.Reverse = ReverseKey;
                    servo.Input.Forward = ForwardKey;
                }
            }
        }
    }
}
