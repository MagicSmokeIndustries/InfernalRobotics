using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

// TODO: Change this namespace to something specific to your plugin here.
namespace IRWrapper
{

    public class IRWrapper
    {
        protected static System.Type IRServoControllerType;
        protected static System.Type IRControlGroupType;
        protected static System.Type IRServoType;

        protected static Object actualServoController = null;

        public static IRAPI IRController = null;
        public static Boolean AssemblyExists { get { return (IRServoControllerType != null); } }
        public static Boolean InstanceExists { get { return (IRController != null); } }
        private static Boolean isWrapped = false;
        public static Boolean APIReady { get { return isWrapped && IRController.APIReady; } }

        public static Boolean InitWrapper()
        {
            isWrapped = false;
            actualServoController = null;
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
           
            IRControlGroupType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Command.ServoController.ControlGroup");

            if (IRControlGroupType == null)
            {
                return false;
            }

            IRServoType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.Servo.Servo");

            if (IRServoType == null)
            {
                return false;
            }

            LogFormatted("Got Assembly Types, grabbing Instance");

            try
            {
                actualServoController = IRServoControllerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
            catch (Exception)
            {
                LogFormatted("No Instance found");
            }

            if (actualServoController == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            LogFormatted("Got Instance, Creating Wrapper Objects");
            IRController = new IRAPI(actualServoController);
            isWrapped = true;
            return true;
        }

        public class IRAPI
        {
            internal IRAPI(Object IRServoController)
            {
                //store the actual object
                actualServoController = IRServoController;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                LogFormatted("Getting APIReady Object");
                APIReadyField = IRServoControllerType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (APIReadyField != null).ToString());

                LogFormatted("Getting ServoGroups Object");
                ServoGroupsField = IRServoControllerType.GetField("ServoGroups", BindingFlags.Public | BindingFlags.Static);
                actualServoGroups = ServoGroupsField.GetValue(actualServoController);
                LogFormatted("Success: " + (actualServoGroups != null).ToString());
                
            }

            private Object actualServoController;

            private FieldInfo APIReadyField;
            public Boolean APIReady
            {
                get
                {
                    if (APIReadyField == null)
                        return false;

                    return (Boolean)APIReadyField.GetValue(null);
                }
            }

            private Object actualServoGroups;
            private FieldInfo ServoGroupsField;

            internal IRServoGroupsList ServoGroups
            {
                get
                {
                    return ExtractServoGroups(actualServoGroups);
                }
            }

            private IRServoGroupsList ExtractServoGroups(Object actualServoGroups)
            {
                IRServoGroupsList ListToReturn = new IRServoGroupsList();
                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)actualServoGroups)
                    {
                        IRControlGroup r1 = new IRControlGroup(item);
                        ListToReturn.Add(r1);
                    }
                }
                catch (Exception)
                {
                    //LogFormatted("Arrggg: {0}", ex.Message);
                    //throw ex;
                    //
                }
                return ListToReturn;
            }

            public class IRControlGroup
            {
                internal IRControlGroup(Object cg)
                {
                    actualControlGroup = cg;
                    NameField = IRControlGroupType.GetField("Name");
                    ForwardKeyField = IRControlGroupType.GetField("ForwardKey");
                    ReverseKeyField = IRControlGroupType.GetField("ReverseKey");
                    SpeedField = IRControlGroupType.GetField("Speed");

                    ServosField = IRControlGroupType.GetField("Servos");
                    actualServos = ServosField.GetValue(actualControlGroup);

                    MovePositiveMethod = IRControlGroupType.GetMethod("MovePositive", BindingFlags.Public | BindingFlags.Instance);
                    MoveNegativeMethod = IRControlGroupType.GetMethod("MoveNegative", BindingFlags.Public | BindingFlags.Instance);
                    MoveCenterMethod = IRControlGroupType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                    MoveNextPresetMethod = IRControlGroupType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                    MovePrevPresetMethod = IRControlGroupType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                    StopMethod = IRControlGroupType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                }
                private Object actualControlGroup;

                private FieldInfo NameField;
                public String Name
                {
                    get { return (String)NameField.GetValue(actualControlGroup); }
                    set { NameField.SetValue(actualControlGroup, value); }
                }

                private FieldInfo ForwardKeyField;
                public String ForwardKey
                {
                    get { return (String)ForwardKeyField.GetValue(actualControlGroup); }
                    set { ForwardKeyField.SetValue(actualControlGroup, value); }
                }

                private FieldInfo ReverseKeyField;
                public String ReverseKey
                {
                    get { return (String)ReverseKeyField.GetValue(actualControlGroup); }
                    set { ReverseKeyField.SetValue(actualControlGroup, value); }
                }

                private FieldInfo SpeedField;
                public String Speed
                {
                    get { return (String)SpeedField.GetValue(actualControlGroup); }
                    set { SpeedField.SetValue(actualControlGroup, value); }
                }

                private Object actualServos;
                private FieldInfo ServosField;

                internal IRServosList Servos
                {
                    get
                    {
                        return ExtractServos(actualServos);
                    }
                }

                private IRServosList ExtractServos(Object actualServos)
                {
                    IRServosList ListToReturn = new IRServosList();
                    try
                    {
                        //iterate each "value" in the dictionary
                        foreach (var item in (IList)actualServos)
                        {
                            IRServo r1 = new IRServo(item);
                            ListToReturn.Add(r1);
                        }
                    }
                    catch (Exception)
                    {
                        //LogFormatted("Arrggg: {0}", ex.Message);
                        //throw ex;
                        //
                    }
                    return ListToReturn;
                }

                private MethodInfo MovePositiveMethod;
                internal void MovePositive()
                {
                    MovePositiveMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MoveNegativeMethod;
                internal void MoveNegative()
                {
                    MoveNegativeMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MoveCenterMethod;
                internal void MoveCenter()
                {
                    MoveCenterMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MoveNextPresetMethod;
                internal void MoveNextPreset()
                {
                    MoveNextPresetMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MovePrevPresetMethod;
                internal void MovePrevPreset()
                {
                    MovePrevPresetMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo StopMethod;
                internal void Stop()
                {
                    StopMethod.Invoke(actualControlGroup, new System.Object[] { });
                }
            }

            public class IRServo
            {
                internal IRServo(Object s)
                {
                    actualServo = s;
                    NameField = IRServoType.GetField("Name");

                }
                private Object actualServo;


                private FieldInfo NameField;
                public String Name
                {
                    get { return (String)NameField.GetValue(actualServo); }
                    set { NameField.SetValue(actualServo, value); }
                }
            }

            public class IRServoGroupsList : List<IRControlGroup>
            {

            }

            public class IRServosList : List<IRServo>
            {

            }
        }

        #region Logging Stuff
        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }
        #endregion
    }
}