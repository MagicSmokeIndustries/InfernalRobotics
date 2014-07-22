using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace InfernalRobotics
{
    public class zTriggerTweaks 
    {

        internal static GroupPosList lstGroupPositions = new GroupPosList();
        internal static ServoPosList lstServoPositions = new ServoPosList();

        internal static Vector2 mousePos;
        internal static Vector2 scrollPos;


        internal static void InitPositionLists()
        {
            if (Event.current.type == EventType.Repaint)
            {
                lstGroupPositions = new GroupPosList();
                lstServoPositions = new ServoPosList();
            }
        }


        internal class GroupPosList : List<GroupPosition>
        {
            void InitPositionLists() { if (Event.current.type == EventType.Repaint) this.Clear(); }

            internal void Add(String name) {
                if (Event.current.type == EventType.Repaint)
                {
                    this.Add(new GroupPosition(name, 
                        GUILayoutUtility.GetLastRect()));
                }
            }
        }

        internal class ServoPosList : List<ServoPosition>
        {
            void InitPositionLists() { if (Event.current.type == EventType.Repaint) this.Clear(); }

            internal void Add(String name, Int32 groupID)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    this.Add(new ServoPosition(name,groupID,
                        GUILayoutUtility.GetLastRect()));
                }
            }
        }



        internal class GroupPosition
        {
            internal GroupPosition(String Name,Rect rectName)
            {
                this.Name = Name;
                this.groupRect = rectName;
            }
            public String Name { get; set; }
            public Rect groupRect { get; set; }
        }

        internal class ServoPosition
        {
            internal ServoPosition(String Name, Int32 Group,Rect rectName )
            {
                this.Name = Name;
                this.groupID = Group;
                this.servoRect = rectName;
            }
            public String Name { get; set; }
            public int groupID { get; set; }
            public Rect servoRect { get; set; }
        }



        public static Rect debugWinPos = new Rect(100,200,400,400);

        internal static Int32 intTest1 = 310;
        internal static Int32 intTest2 = 0;
        internal static Int32 intTest3 = 0;
        internal static Int32 intTest4 = 0;

        public static void DebugWindow(int windowID)
        {
            DrawTextBox(ref intTest1);
            DrawTextBox(ref intTest2);
            DrawTextBox(ref intTest3);
            DrawTextBox(ref intTest4);

            GUILayout.Label(String.Format("S:{0} - M:{1}",scrollPos,mousePos));
            foreach (GroupPosition item in lstGroupPositions)
            {
                GUILayout.Label(String.Format("{0}:{1}", item.Name, item.groupRect));
            }
            foreach (ServoPosition item in lstServoPositions)
            {
                GUILayout.Label(String.Format("{0}({1}):{2}", item.Name, item.groupID,item.servoRect));
            }

            GUI.DragWindow();
        }


        


        internal static Boolean DrawTextBox(ref Int32 intVar)
        {
            String strRef = intVar.ToString();
            DrawTextBox(ref strRef);
            Int32 intOld = intVar;
            intVar = Convert.ToInt32(strRef);
            return (intOld!= intVar);
        }

        internal static Boolean DrawTextBox(ref String strVar)
        {
            String strOld = strVar;
            strVar = GUILayout.TextField(strVar);

            return (strOld != strVar);
        }
    }
//#if DEBUG
//    //This will kick us into the save called default and set the first vessel active
//    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
//    public class Debug_AutoLoadPersistentSaveOnStartup : MonoBehaviour
//    {
//        //use this variable for first run to avoid the issue with when this is true and multiple addons use it
//        public static bool first = true;
//        public void Start()
//        {
//            //only do it on the first entry to the menu
//            if (first)
//            {
//                first = false;
//                HighLogic.SaveFolder = "default";
//                Game game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);

//                if (game != null && game.flightState != null && game.compatible)
//                {
//                    Int32 FirstVessel;
//                    Boolean blnFoundVessel = false;
//                    for (FirstVessel = 0; FirstVessel < game.flightState.protoVessels.Count; FirstVessel++)
//                    {
//                        if (game.flightState.protoVessels[FirstVessel].vesselType != VesselType.SpaceObject &&
//                            game.flightState.protoVessels[FirstVessel].vesselType != VesselType.Unknown)
//                        {
//                            blnFoundVessel = true;
//                            break;
//                        }
//                    }
//                    if (!blnFoundVessel)
//                        FirstVessel = 0;
//                    FlightDriver.StartAndFocusVessel(game, FirstVessel);
//                }

//                //CheatOptions.InfiniteFuel = true;
//            }
//        }
//    }
//#endif
}
