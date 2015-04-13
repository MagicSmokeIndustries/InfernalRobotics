using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// TODO: Change this namespace to something specific to your plugin here.
namespace InfernalRobotics.API
{
    public class IRWrapper
    {
        private static Boolean isWrapped;

        protected static Type IRServoControllerType { get; set; }
        protected static Type IRControlGroupType { get; set; }
        protected static Type IRServoType { get; set; }
        protected static Type IRServoPartType { get; set; }
        protected static Type IRServoMechanismType { get; set; }
        protected static object ActualServoController { get; set; }

        public static IRAPI IRController { get; set; }
        public static Boolean AssemblyExists { get { return (IRServoControllerType != null); } }
        public static Boolean InstanceExists { get { return (IRController != null); } }
        public static Boolean APIReady { get { return isWrapped && IRController.APIReady; } }

        public static Boolean InitWrapper()
        {
            isWrapped = false;
            ActualServoController = null;
            IRController = null;
            LogFormatted("Attempting to Grab IR Types...");

            IRServoControllerType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Command.ServoController");

            if (IRServoControllerType == null)
            {
                return false;
            }

            LogFormatted("IR Version:{0}", IRServoControllerType.Assembly.GetName().Version.ToString());

            IRServoMechanismType = AssemblyLoader.loadedAssemblies
               .Select(a => a.assembly.GetExportedTypes())
               .SelectMany(t => t)
               .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.IMechanism");

            if (IRServoMechanismType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab Mechanism Type");
                return false;
            }

            IRServoType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.IServo");

            if (IRServoType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab Servo Type");
                return false;
            }

            IRServoPartType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.IPart");

            if (IRServoType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab ServoPart Type");
                return false;
            }

            IRControlGroupType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Command.ServoController+ControlGroup");

            if (IRControlGroupType == null)
            {
                var irassembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.FullName.Contains("InfernalRobotics"));
                if (irassembly == null)
                {
                    LogFormatted("[IR Wrapper] cannot find InvernalRobotics.dll");
                    return false;
                }
                foreach (Type t in irassembly.assembly.GetExportedTypes())
                {
                    LogFormatted("[IR Wrapper] Exported type: " + t.FullName);
                }

                LogFormatted("[IR Wrapper] Failed to grab ControlGroup Type");
                return false;
            }

            LogFormatted("Got Assembly Types, grabbing Instance");

            try
            {
                var propertyInfo = IRServoControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                
                if (propertyInfo == null)
                    LogFormatted("[IR Wrapper] Cannot find Instance Property");
                else
                    ActualServoController = propertyInfo.GetValue(null, null);
            }
            catch (Exception e)
            {
                LogFormatted("No Instance found, " + e.Message);
            }

            if (ActualServoController == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            LogFormatted("Got Instance, Creating Wrapper Objects");
            IRController = new IRAPI(ActualServoController);
            isWrapped = true;
            return true;
        }

        public class IRAPI
        {
            public IRAPI(object irServoController)
            {
                LogFormatted("Getting APIReady Object");
                apiReady = IRServoControllerType.GetProperty("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (apiReady != null));

                LogFormatted("Getting ServoGroups Object");
                var servoGroupsField = IRServoControllerType.GetField("ServoGroups");
                if (servoGroupsField == null)
                    LogFormatted("Failed Getting ServoGroups fieldinfo");
                actualServoGroups = servoGroupsField.GetValue(irServoController);
                LogFormatted("Success: " + (actualServoGroups != null));
                
            }

            private readonly PropertyInfo apiReady;
            public Boolean APIReady
            {
                get
                {
                    if (apiReady == null)
                        return false;

                    return (Boolean)apiReady.GetValue(null, null);
                }
            }

            private readonly object actualServoGroups;

            internal IList<IControlGroup> ServoGroups
            {
                get
                {
                    return ExtractServoGroups(actualServoGroups);
                }
            }

            private IList<IControlGroup> ExtractServoGroups(object servoGroups)
            {
                var listToReturn = new List<IControlGroup>();
                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)servoGroups)
                    {
                        listToReturn.Add(new IRControlGroup(item));
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Arrggg: {0}", ex.Message);
                }
                return listToReturn;
            }


            private class IRControlGroup : IControlGroup
            {
                public IRControlGroup(object cg)
                {
                    actualControlGroup = cg;
                    nameProperty = IRControlGroupType.GetProperty("Name");
                    forwardKeyProperty = IRControlGroupType.GetProperty("ForwardKey");
                    reverseKeyProperty = IRControlGroupType.GetProperty("ReverseKey");
                    speedProperty = IRControlGroupType.GetProperty("Speed");
                    expandedProperty = IRControlGroupType.GetProperty("Expanded");

                    var servosProperty = IRControlGroupType.GetProperty("Servos");
                    ActualServos = servosProperty.GetValue(actualControlGroup, null);

                    moveRightMethod = IRControlGroupType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                    moveLeftMethod = IRControlGroupType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                    moveCenterMethod = IRControlGroupType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                    moveNextPresetMethod = IRControlGroupType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                    movePrevPresetMethod = IRControlGroupType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                    stopMethod = IRControlGroupType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                }
                private readonly object actualControlGroup;

                private readonly PropertyInfo nameProperty;
                public string Name
                {
                    get { return (string)nameProperty.GetValue(actualControlGroup, null); }
                    set { nameProperty.SetValue(actualControlGroup, value, null); }
                }

                private readonly PropertyInfo forwardKeyProperty;
                public string ForwardKey
                {
                    get { return (string)forwardKeyProperty.GetValue(actualControlGroup, null); }
                    set { forwardKeyProperty.SetValue(actualControlGroup, value, null); }
                }

                private readonly PropertyInfo reverseKeyProperty;
                public string ReverseKey
                {
                    get { return (string)reverseKeyProperty.GetValue(actualControlGroup, null); }
                    set { reverseKeyProperty.SetValue(actualControlGroup, value, null); }
                }

                private readonly PropertyInfo speedProperty;
                public float Speed
                {
                    get { return (float)speedProperty.GetValue(actualControlGroup, null); }
                    set { speedProperty.SetValue(actualControlGroup, value, null); }
                }

                private readonly PropertyInfo expandedProperty;
                public bool Expanded
                {
                    get { return (bool)expandedProperty.GetValue(actualControlGroup, null); }
                    set { expandedProperty.SetValue(actualControlGroup, value, null); }
                }

                public object ActualServos { get; set; }

                public IList<IServo> Servos
                {
                    get
                    {
                        return ExtractServos(ActualServos);
                    }
                }

                private IList<IServo> ExtractServos(object actualServos)
                {
                    var listToReturn = new List<IServo>();
                    try
                    {
                        //iterate each "value" in the dictionary
                        foreach (var item in (IList)actualServos)
                        {
                            listToReturn.Add(new IRServo(item));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogFormatted("Arrggg: {0}", ex.Message);
                    }
                    return listToReturn;
                }

                private readonly MethodInfo moveRightMethod;
                public void MoveRight()
                {
                    moveRightMethod.Invoke(actualControlGroup, new object[] { });
                }

                private readonly MethodInfo moveLeftMethod;
                public void MoveLeft()
                {
                    moveLeftMethod.Invoke(actualControlGroup, new object[] { });
                }

                private readonly MethodInfo moveCenterMethod;
                public void MoveCenter()
                {
                    moveCenterMethod.Invoke(actualControlGroup, new object[] { });
                }

                private readonly MethodInfo moveNextPresetMethod;
                public void MoveNextPreset()
                {
                    moveNextPresetMethod.Invoke(actualControlGroup, new object[] { });
                }

                private readonly MethodInfo movePrevPresetMethod;
                public void MovePrevPreset()
                {
                    movePrevPresetMethod.Invoke(actualControlGroup, new object[] { });
                }

                private readonly MethodInfo stopMethod;
                public void Stop()
                {
                    stopMethod.Invoke(actualControlGroup, new object[] { });
                }
            }

            public class IRServo : IServo
            {

                public IRServo(object s)
                {
                    actualServo = s;

                    nameProperty = IRServoPartType.GetProperty("Name");
                    highlightProperty = IRServoPartType.GetProperty("Highlight");
                    
                    var mechanismProperty = IRServoType.GetProperty("Mechanism");
                    actualServoMechanism = mechanismProperty.GetValue(actualServo, null);

                    positionProperty = IRServoMechanismType.GetProperty("Position");
                    minPositionProperty = IRServoMechanismType.GetProperty("MinPositionLimit");
                    maxPositionProperty = IRServoMechanismType.GetProperty("MaxPositionLimit");

                    minConfigPositionProperty = IRServoMechanismType.GetProperty("MinPosition");
                    maxConfigPositionProperty = IRServoMechanismType.GetProperty("MaxPosition");

                    speedProperty = IRServoMechanismType.GetProperty("SpeedLimit");
                    configSpeedProperty = IRServoMechanismType.GetProperty("DefaultSpeed");
                    currentSpeedProperty = IRServoMechanismType.GetProperty("CurrentSpeed");
                    accelerationProperty = IRServoMechanismType.GetProperty("AccelerationLimit");
                    isMovingProperty = IRServoMechanismType.GetProperty("IsMoving");
                    isFreeMovingProperty = IRServoMechanismType.GetProperty("IsFreeMoving");
                    isLockedProperty = IRServoMechanismType.GetProperty("IsLocked");
                    isAxisInvertedProperty = IRServoMechanismType.GetProperty("IsAxisInverted");
                    
                    moveRightMethod = IRServoMechanismType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                    moveLeftMethod = IRServoMechanismType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                    moveCenterMethod = IRServoMechanismType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                    moveNextPresetMethod = IRServoMechanismType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                    movePrevPresetMethod = IRServoMechanismType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                    stopMethod = IRServoMechanismType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);

                    moveToMethod = IRServoMechanismType.GetMethod("MoveTo", new[] { typeof(float), typeof(float) });
                }
                private readonly object actualServo;

                private readonly object actualServoMechanism;

                private readonly PropertyInfo nameProperty;
                public string Name
                {
                    get { return (string)nameProperty.GetValue(actualServo, null); }
                    set { nameProperty.SetValue(actualServo, value, null); }
                }

                private readonly PropertyInfo highlightProperty;
                public bool Highlight
                {
                    //get { return (bool)HighlightProperty.GetValue(actualServo, null); }
                    set { highlightProperty.SetValue(actualServo, value, null); }
                }

                private readonly PropertyInfo positionProperty;
                public float Position
                {
                    get { return (float)positionProperty.GetValue(actualServoMechanism, null); }
                }

                private readonly PropertyInfo minConfigPositionProperty;
                public float MinConfigPosition
                {
                    get { return (float)minConfigPositionProperty.GetValue(actualServoMechanism, null); }
                }

                private readonly PropertyInfo maxConfigPositionProperty;
                public float MaxConfigPosition
                {
                    get { return (float)maxConfigPositionProperty.GetValue(actualServoMechanism, null); }
                }

                private readonly PropertyInfo minPositionProperty;
                public float MinPosition
                {
                    get { return (float)minPositionProperty.GetValue(actualServoMechanism, null); }
                    set { minPositionProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly PropertyInfo maxPositionProperty;
                public float MaxPosition
                {
                    get { return (float)maxPositionProperty.GetValue(actualServoMechanism, null); }
                    set { maxPositionProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly PropertyInfo configSpeedProperty;
                public float ConfigSpeed
                {
                    get { return (float)configSpeedProperty.GetValue(actualServoMechanism, null); }
                }

                private readonly PropertyInfo speedProperty;
                public float Speed
                {
                    get { return (float)speedProperty.GetValue(actualServoMechanism, null); }
                    set { speedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly PropertyInfo currentSpeedProperty;
                public float CurrentSpeed
                {
                    get { return (float)currentSpeedProperty.GetValue(actualServoMechanism, null); }
                    set { currentSpeedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly PropertyInfo accelerationProperty;
                public float Acceleration
                {
                    get { return (float)accelerationProperty.GetValue(actualServoMechanism, null); }
                    set { accelerationProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly PropertyInfo isMovingProperty;
                public bool IsMoving
                {
                    get { return (bool)isMovingProperty.GetValue(actualServoMechanism, null); }
                }

                private readonly PropertyInfo isFreeMovingProperty;
                public bool IsFreeMoving
                {
                    get { return (bool)isFreeMovingProperty.GetValue(actualServoMechanism, null); }
                }

                private readonly PropertyInfo isLockedProperty;
                public bool IsLocked
                {
                    get { return (bool)isLockedProperty.GetValue(actualServoMechanism, null); }
                    set { isLockedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly PropertyInfo isAxisInvertedProperty;
                public bool IsAxisInverted
                {
                    get { return (bool)isAxisInvertedProperty.GetValue(actualServoMechanism, null); }
                    set { isAxisInvertedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private readonly MethodInfo moveRightMethod;
                public void MoveRight()
                {
                    moveRightMethod.Invoke(actualServoMechanism, new object[] { });
                }

                private readonly MethodInfo moveLeftMethod;
                public void MoveLeft()
                {
                    moveLeftMethod.Invoke(actualServoMechanism, new object[] { });
                }

                private readonly MethodInfo moveCenterMethod;
                public void MoveCenter()
                {
                    moveCenterMethod.Invoke(actualServoMechanism, new object[] { });
                }

                private readonly MethodInfo moveNextPresetMethod;
                public void MoveNextPreset()
                {
                    moveNextPresetMethod.Invoke(actualServoMechanism, new object[] { });
                }

                private readonly MethodInfo movePrevPresetMethod;
                public void MovePrevPreset()
                {
                    movePrevPresetMethod.Invoke(actualServoMechanism, new object[] { });
                }

                private readonly MethodInfo moveToMethod;
                public void MoveTo(float position, float speed)
                {
                    moveToMethod.Invoke(actualServoMechanism, new object[] {position, speed });
                }

                private readonly MethodInfo stopMethod;
                public void Stop()
                {
                    stopMethod.Invoke(actualServoMechanism, new object[] { });
                }

                public override bool Equals(object o)
                {
                    var servo = o as IRServo;
                    return servo != null && actualServo.Equals(servo.actualServo);
                }

                public override int GetHashCode()
                {
                    return (actualServo != null ? actualServo.GetHashCode() : 0);
                }

                public static bool operator ==(IRServo left, IRServo right)
                {
                    return Equals(left, right);
                }

                public static bool operator !=(IRServo left, IRServo right)
                {
                    return !Equals(left, right);
                }

                protected bool Equals(IRServo other)
                {
                    return Equals(actualServo, other.actualServo);
                }
            }
        }


        internal interface IControlGroup
        {
            string Name { get; set; }
            string ForwardKey { get; set; }
            string ReverseKey { get; set; }
            float Speed { get; set; }
            bool Expanded { get; set; }
            object ActualServos { get; set; }
            IList<IServo> Servos { get; }
            void MoveRight();
            void MoveLeft();
            void MoveCenter();
            void MoveNextPreset();
            void MovePrevPreset();
            void Stop();
        }

        internal interface IServo
        {
            string Name { get; set; }

            bool Highlight { set; }

            float Position { get; }
            float MinConfigPosition { get; }
            float MaxConfigPosition { get; }
            float MinPosition { get; set; }
            float MaxPosition { get; set; }
            float ConfigSpeed { get; }
            float Speed { get; set; }
            float CurrentSpeed { get; set; }
            float Acceleration { get; set; }
            bool IsMoving { get; }
            bool IsFreeMoving { get; }
            bool IsLocked { get; set; }
            bool IsAxisInverted { get; set; }
            void MoveRight();
            void MoveLeft();
            void MoveCenter();
            void MoveNextPreset();
            void MovePrevPreset();
            void MoveTo(float position, float speed);
            void Stop();
            bool Equals(object o);
            int GetHashCode();
        }

        #region Logging Stuff
        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(string message, params object[] strParams)
        {
            LogFormatted(message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        internal static void LogFormatted(string message, params object[] strParams)
        {
            message = string.Format(message, strParams);
            string strMessageLine = string.Format("{0},{2}-{3},{1}",
                DateTime.Now, message, Assembly.GetExecutingAssembly().GetName().Name,
                MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }
        #endregion
    }
}