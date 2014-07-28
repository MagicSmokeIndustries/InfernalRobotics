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
#region UITweaks
        internal static Int32 EditorWidth = 330;
        internal static Int32 EditorButtonHeights = 25;
#endregion



        //internal static GroupPosList lstGroupPositions = new GroupPosList();
        //internal static ServoPosList lstServoPositions = new ServoPosList();

        //internal static Vector2 MousePosition;
        //internal static Vector2 ScrollPosition;

        //internal static Boolean DragOn = false;

        //internal static void InitPositionLists()
        //{
        //    if (Event.current.type == EventType.Repaint)
        //    {
        //        lstGroupPositions = new GroupPosList();
        //        lstServoPositions = new ServoPosList();
        //    }
        //}

        //internal class GroupPosList : List<GroupPosition>
        //{
        //    void InitPositionLists() { if (Event.current.type == EventType.Repaint) this.Clear(); }

        //    internal void Add(String name) {
        //        if (Event.current.type == EventType.Repaint)
        //        {
        //            this.Add(new GroupPosition(name, 
        //                GUILayoutUtility.GetLastRect()));
        //        }
        //    }
        //    internal void SetHeight(Int32 ID, Rect LastServoRect)
        //    {
        //        Rect newRect = new Rect(this[ID].groupRect) {height = LastServoRect.y - this[ID].groupRect.y + LastServoRect.height};
        //        this[ID].groupRect = newRect;
        //    }
        //}

        //internal class ServoPosList : List<ServoPosition>
        //{
        //    void InitPositionLists() { if (Event.current.type == EventType.Repaint) this.Clear(); }

        //    internal void Add(String name, Int32 groupID,Boolean Last=false)
        //    {
        //        if (Event.current.type == EventType.Repaint)
        //        {
        //            this.Add(new ServoPosition(name,groupID,
        //                GUILayoutUtility.GetLastRect()));
        //            if (Last)
        //                lstGroupPositions.SetHeight(groupID, GUILayoutUtility.GetLastRect());
        //        }
        //    }
        //}

        //internal class GroupPosition
        //{
        //    internal GroupPosition(String Name,Rect rectName)
        //    {
        //        this.Name = Name;
        //        this.groupRect = rectName;
        //    }
        //    public String Name { get; set; }
        //    public Rect groupRect { get; set; }
        //}

        //internal class ServoPosition
        //{
        //    internal ServoPosition(String Name, Int32 Group,Rect rectName )
        //    {
        //        this.Name = Name;
        //        this.groupID = Group;

        //        this.servoRect = new Rect(rectName) { x = rectName.x - 30, width = 200 };

        //        this.iconRect = new Rect(servoRect) { x = servoRect.x - 20, width = 20 };
        //    }
        //    public String Name { get; set; }
        //    public int groupID { get; set; }
        //    public Rect servoRect { get; set; }
        //    public Rect iconRect { get; set; }
        //}


        //internal static GroupPosition GroupOver=null;

        //internal static ServoPosition ServoDrag=null;
        //internal static ServoPosition ServoOver=null;
        //internal static ServoPosition iconServoOver=null;

        //internal static Boolean DraggingItem = false;
        //internal static Boolean InScroll = false;

        //internal static void DraggingMouseChecks()
        //{
        //                //If the Mouse is inside the scroll window
        //    if (MousePosition.y > intTest3 && MousePosition.y < intTest4)
        //    {
        //        InScroll=true;
        //        //check what we are over
        //        ServoOver = lstServoPositions.FirstOrDefault(x => x.servoRect.Contains(MousePosition + ScrollPosition - new Vector2(intTest1, intTest2)));
        //        //////////if (ServoOver != null)
        //        //////////{
        //        //////////    resourceOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 54)).y - ServoOver.resourceRect.y) < ServoOver.resourceRect.height / 2;
        //        //////////    resourceInsertIndex = lstServoPositions.FindIndex(x => x.id == resourceOver.id);
        //        //////////    if (!resourceOverUpper) resourceInsertIndex += 1;
        //        //////////}
        //        //////////else
        //        //////////    resourceInsertIndex = -1;
        //        iconServoOver = lstServoPositions.FirstOrDefault(x => x.iconRect.Contains(MousePosition + ScrollPosition - new Vector2(intTest1, intTest2)));

        //        //Will the drop actually change the list
        //        //////////DropWillReorderList = (resourceInsertIndex != resourceDragIndex) && (resourceInsertIndex != resourceDragIndex + 1);
        //    }
        //    else {InScroll=false; ServoOver = null; iconServoOver = null; }

        //    //did we click on an Icon with mouse button 0
        //    if (Event.current.type == EventType.mouseDown && 
        //        Event.current.button==0 && iconServoOver!=null)
        //    {
        //        //////////LogFormatted_DebugOnly("Drag Start");
        //        //////////ServoDrag = iconServoOver;
        //        //////////resourceDragIndex = lstResPositions.FindIndex(x=>x.id==ServoDrag.id);
        //        //////////DraggingItem = true;
        //        //////////DropWillReorderList = false;
        //    }
        //    //did we release the mouse
        //    if (Event.current.type == EventType.mouseUp &&
        //        Event.current.button == 0)
        //    {
        //        if (ServoOver != null)
        //        {
        //            //And dropped on a resource - cater to the above below code in this new one
        //            //LogFormatted_DebugOnly("Drag Stop:{0}-{1}-{2}", resourceOver == null ? "None" : resourceDragIndex.ToString(), resourceOver == null ? "" : (resourceInsertIndex< lstResPositions.Count ? settings.Resources[lstResPositions[resourceInsertIndex].id].name:"Last"), resourceDrag.name);
        //            //////////MoveResource(resourceDragIndex, resourceInsertIndex);

        //            //LogFormatted_DebugOnly("Drag Stop:{0}-{1}-{2}", resourceOver == null ? "None" : lstResPositions.FindIndex(x => x.id == resourceOver.id).ToString(), resourceOver == null ? "" : settings.Resources[resourceOver.id].name, resourceDrag.name);
        //            //MoveResource(lstResPositions.FindIndex(x => x.id == resourceDrag.id), lstResPositions.FindIndex(x => x.id == resourceOver.id));
        //        }
        //        //disable dragging flag
        //        DraggingItem = false;
        //        ServoDrag = null;
        //    }

        //    //If we are dragging and in the bottom or top area then scrtoll the list
        //    //if(DraggingItem && rectScrollBottom.Contains(MousePosition))
        //    //    ScrollPosition.y += (Time.deltaTime * 40);
        //    //if(DraggingItem && rectScrollTop.Contains(MousePosition))
        //    //    ScrollPosition.y -= (Time.deltaTime * 40);
        ////}
        //}





















        public static Rect debugWinPos = new Rect(100,200,400,400);

        internal static Int32 intTest1 = 32;
        internal static Int32 intTest2 = 20;
        internal static Int32 intTest3 = 8;
        internal static Int32 intTest4 = 29;

        public static void DebugWindow(int windowID)
        {
            DrawTextBox(ref intTest1);
            DrawTextBox(ref intTest2);
            DrawTextBox(ref intTest3);
            DrawTextBox(ref intTest4);

            GUILayout.Label(String.Format("S:{0} - M:{1}", GUIDragAndDrop.ScrollPosition, GUIDragAndDrop.MousePosition));
            GUILayout.Label(String.Format("D:{0}", GUIDragAndDrop.draggingItem));

            GUILayout.Label(String.Format("groupOver:{0}", GUIDragAndDrop.GroupOver != null ? GUIDragAndDrop.GroupOver.ID.ToString() : ""));
            GUILayout.Label(String.Format("iconGroupOver:{0}", GUIDragAndDrop.GroupIconOver != null ? GUIDragAndDrop.GroupIconOver.ID.ToString() : ""));
            GUILayout.Label(String.Format("groupDrag:{0}", GUIDragAndDrop.GroupDragging != null ? GUIDragAndDrop.GroupDragging.ID.ToString() : ""));

            GUILayout.Label(String.Format("ServOver:{0}-{1}",GUIDragAndDrop.ServoOver != null ? GUIDragAndDrop.ServoOver.groupID.ToString() : "", GUIDragAndDrop.ServoOver != null ? GUIDragAndDrop.ServoOver.ID.ToString() : ""));
            GUILayout.Label(String.Format("iconServOver:{0}-{1}", GUIDragAndDrop.ServoIconOver != null ? GUIDragAndDrop.ServoIconOver.groupID.ToString() : "",GUIDragAndDrop.ServoIconOver != null ? GUIDragAndDrop.ServoIconOver.ID.ToString() : ""));
            GUILayout.Label(String.Format("ServoDrag:{0}", GUIDragAndDrop.ServoDragging != null ? GUIDragAndDrop.ServoDragging.ID.ToString() : ""));


            foreach (GUIDragAndDrop.GroupDetails item in GUIDragAndDrop.lstGroups)
            {
                GUILayout.Label(String.Format("{0}:{1:0}:{2:0}", item.ID, item.IconRect,item.GroupRect));

            }
            foreach (GUIDragAndDrop.ServoDetails item in GUIDragAndDrop.lstServos)
            {
                GUILayout.Label(String.Format("{0}:{1}:{2:0}:{3:0}", item.ID,item.groupID, item.IconRect, item.ServoRect));
            }



            //GUILayout.Label(String.Format("S:{0} - M:{1}",ScrollPosition,MousePosition));
            //GUILayout.Label(String.Format("DO:{0}", DragOn));
            //GUILayout.Label(String.Format("inScroll:{0}", InScroll));
            //GUILayout.Label(String.Format("ServOver:{0}", ServoOver!=null?ServoOver.Name:""));
            //GUILayout.Label(String.Format("iconServOver:{0}", iconServoOver != null ? iconServoOver.Name : ""));

            //GUILayout.Label(String.Format("S:{0} - M:{1}", MousePosition.y, intTest3));
            //GUILayout.Label(String.Format("S:{0} - M:{1}", MousePosition.y, intTest4));

            //foreach (GroupPosition item in lstGroupPositions)
            //{
            //    GUILayout.Label(String.Format("{0}:{1}", item.Name, item.groupRect));
            //}
            //foreach (ServoPosition item in lstServoPositions)
            //{
            //    GUILayout.Label(String.Format("{0}({1}):{2}", item.Name, item.groupID,item.servoRect));
            //}

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
