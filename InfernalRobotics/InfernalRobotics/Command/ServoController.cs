using InfernalRobotics.Module;
using KSP.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace InfernalRobotics.Command
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ServoController : MonoBehaviour
    {
        protected static bool UseElectricCharge = true;
        protected static ServoController ControllerInstance;
        
        internal List<ControlGroup> ServoGroups; 
        private int partCounter;

        public static ServoController Instance { get { return ControllerInstance; } }
        
        static ServoController()
        {
        }

        public static void MoveServo(ControlGroup from, ControlGroup to, MuMechToggle servo)
        {
            to.AddControl(servo);
            from.RemoveControl(servo);
        }

        public static void AddServo(MuMechToggle servo)
        {
            if (!Instance)
                return;
            
            if (Instance.ServoGroups == null)
                Instance.ServoGroups = new List<ControlGroup>();

            if (InfernalRobotics.Gui.ControlsGUI.IRGUI)
            {
                InfernalRobotics.Gui.ControlsGUI.IRGUI.enabled = true;
            }

            ControlGroup controlGroup = null;

            if (!string.IsNullOrEmpty(servo.groupName))
            {
                foreach (ControlGroup cg in Instance.ServoGroups)
                {
                    if (servo.groupName == cg.Name)
                    {
                        controlGroup = cg;
                        break;
                    }
                }
                if (controlGroup == null)
                {
                    var newGroup = new ControlGroup(servo);
                    Instance.ServoGroups.Add(newGroup);
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
        }

        public static void RemoveServo(MuMechToggle servo)
        {
            if (!Instance)
                return;

            if (Instance.ServoGroups == null)
                return;

            int num = 0;
            foreach (ControlGroup group in Instance.ServoGroups)
            {
                if (group.Name == servo.groupName)
                {
                    group.RemoveControl(servo);
                }
                num += group.Servos.Count;
            }

            if (InfernalRobotics.Gui.ControlsGUI.IRGUI)
            {
                //disable gui when last servo removed
                InfernalRobotics.Gui.ControlsGUI.IRGUI.enabled = num > 0;
            }
        }

        private void OnVesselChange(Vessel v)
        {
            Logger.Log(string.Format("[ServoController] vessel {0}", v.name));
            ServoGroups = null;
            
            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!groupMap.ContainsKey(servo.groupName))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.groupName]];
                        g.AddControl(servo);
                    }
                }
            }
            Logger.Log(string.Format("[ServoController] {0} groups", groups.Count));

            if (groups.Count > 0)
                ServoGroups = groups;

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    servo.SetupJoints();
                }
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
                    foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
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
                    foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
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
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    MuMechToggle temp = part.Modules.OfType<MuMechToggle>().First();

                    if (temp.rotateJoint)
                    {
                        temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation);
                        temp.rotation = 0;
                    }
                    else
                    {
                        temp.FixedMeshTransform.position = temp.part.transform.position;
                        temp.translation = 0;
                    }
                }
            }
            catch
            {
            }

            foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
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
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!groupMap.ContainsKey(servo.groupName))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.groupName]];
                        g.AddControl(servo);
                    }
                }
            }

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

            Logger.Log("[ServoController] OnDestroy finished sucessfully", Logger.Level.Debug);
        }

        public class ControlGroup
        {
            private bool stale;
            private float totalElectricChargeRequirement;
            private string speed;
            private string forwardKey;
            private string reverseKey;
            private readonly List<MuMechToggle> servos;

            public ControlGroup(MuMechToggle servo)
                : this()
            {
                Name = servo.groupName;
                ForwardKey = servo.forwardKey;
                ReverseKey = servo.reverseKey;
                Speed = servo.customSpeed.ToString("g");
                ShowGUI = servo.showGUI;
                servos.Add(servo);
            }

            public ControlGroup()
            {
                servos = new List<MuMechToggle>();
                Expanded = false;
                Name = "New Group";
                ForwardKey = string.Empty;
                ReverseKey = string.Empty;
                Speed = "1";
                ShowGUI = true;
                MovingNegative = false;
                MovingPositive = false;
                ButtonDown = false;
                stale = true;
            }

            public bool ButtonDown { get; set; }

            public bool Expanded { get; set; }

            public string Name { get; set; }

            public bool ShowGUI { get; private set; }

            public bool MovingNegative { get; set; }

            public bool MovingPositive { get; set; }

            public IList<MuMechToggle> Servos
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

            public void AddControl(MuMechToggle control)
            {
                servos.Add(control);
                control.groupName = Name;
                control.forwardKey = ForwardKey;
                control.reverseKey = ReverseKey;
                stale = true;
            }

            public void RemoveControl(MuMechToggle control)
            {
                servos.Remove(control);
                stale = true;
            }

            public void MovePositive()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move(float.PositiveInfinity, servo.customSpeed * servo.speedTweak);
                    }
                }
            }

            public void MoveNegative()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move(float.NegativeInfinity, servo.customSpeed * servo.speedTweak);
                    }
                }
            }

            public void MoveCenter()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move(servo.Translator.ToExternalPos(0f), servo.customSpeed * servo.speedTweak); //TODO: to be precise this should be not Zero but a default rotation/translation as set in VAB/SPH
                    }
                }
            }

            public void MoveNextPreset()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.MoveNextPreset();
                    }
                }
            }

            public void MovePrevPreset()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.MovePrevPreset();
                    }
                }
            }

            public void Stop()
            {
                MovingNegative = false;
                MovingPositive = false;

                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Stop();
                    }
                }
            }

            private void Freshen()
            {
                if (Servos == null) return;

                if (UseElectricCharge)
                {
                    float chargeRequired = Servos.Where(s => s.freeMoving == false).Select(s => s.electricChargeRequired).Sum();
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.GroupElectricChargeRequired = chargeRequired;
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
                    servo.forwardKey = ForwardKey;
                }
            }

            private void PropogateReverse()
            {
                if (Servos == null) return;

                foreach (var servo in Servos)
                {
                    servo.reverseKey = ReverseKey;
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
                    servo.customSpeed = parsedSpeed;
                }
            }
        }
    }
}
