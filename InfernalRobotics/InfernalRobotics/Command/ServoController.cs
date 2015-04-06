using System;
using InfernalRobotics.Control;
using InfernalRobotics.Control.Servo;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace InfernalRobotics.Command
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ServoController : MonoBehaviour
    {
        protected static bool UseElectricCharge = true;
        protected static ServoController ControllerInstance;
        
        public List<ControlGroup> ServoGroups; 
        private int partCounter;

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

            if (Gui.ControlsGUI.IRGUI)
            {
                Gui.ControlsGUI.IRGUI.enabled = true;
            }

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

            if (Gui.ControlsGUI.IRGUI)
            {
                //disable GUI when last servo removed
                Gui.ControlsGUI.IRGUI.enabled = num > 0;
            }
            Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);
        }

        private void OnVesselChange(Vessel v)
        {
            Logger.Log(string.Format("[ServoController] vessel {0}", v.name));
            ServoGroups = null;
            
            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (var servo in v.ToServos())
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

            Logger.Log(string.Format("[ServoController] {0} groups", groups.Count));

            if (groups.Count > 0)
                ServoGroups = groups;

            foreach (var servo in v.ToServos())
            {
                servo.RawServo.SetupJoints();
            }
            Logger.Log("[ServoController] OnVesselChange finished successfully", Logger.Level.Debug);
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

        private void OnEditorShipModified(ShipConstruct ship)
        {
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
            
            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;
            Logger.Log("[ServoController] OnEditorShipModified finished successfully", Logger.Level.Debug);
        }

        private void Awake()
        {
            Logger.Log("[ServoController] awake");

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT)
            {
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onVesselWasModified.Add(OnVesselWasModified);
                ControllerInstance = this;
            }
            else if (scene == GameScenes.EDITOR)
            {
                GameEvents.onPartAttach.Add(OnPartAttach);
                GameEvents.onPartRemove.Add(OnPartRemove);
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                ControllerInstance = this;
            }
            else
            {
                ControllerInstance = null;
            }

            Logger.Log("[ServoController] awake finished successfully", Logger.Level.Debug);
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (v == FlightGlobals.ActiveVessel)
            {
                ServoGroups = null;

                OnVesselChange(v);
            }
        }

        private void OnDestroy()
        {
            Logger.Log("[ServoController] destroy");

            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);

            Logger.Log("[ServoController] OnDestroy finished successfully", Logger.Level.Debug);
        }

        public class ControlGroup
        {
            private bool stale;
            private float totalElectricChargeRequirement;
            private string speed;
            private string forwardKey;
            private string reverseKey;
            private readonly List<IServo> servos;

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

            public string Name { get; set; }

            public bool MovingNegative { get; set; }

            public bool MovingPositive { get; set; }

            public IList<IServo> Servos
            {
                get { return servos; }
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
                        servo.Mechanism.MoveRight();
                    }
                }
            }

            public void MoveLeft()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Mechanism.MoveLeft();
                    }
                }
            }

            public void MoveCenter()
            {
                if (Servos.Any())
                {
                    foreach (var servo in Servos)
                    {
                        servo.Mechanism.MoveCenter();
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
                        servo.Mechanism.Stop();
                    }
                }
            }

            private void Freshen()
            {
                if (Servos == null) return;

                if (UseElectricCharge)
                {
                    float chargeRequired = Servos.Where(s => s.Mechanism.IsFreeMoving == false).Select(s => s.ElectricChargeRequired).Sum();
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
