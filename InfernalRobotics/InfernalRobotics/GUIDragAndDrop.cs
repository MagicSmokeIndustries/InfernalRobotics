using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MuMech;
using KSP;
using UnityEngine;

namespace InfernalRobotics
{
    internal static class GUIDragAndDrop
    {

        /// <summary>
        /// Use this to enable/disable functionality
        /// </summary>
        internal static Boolean Enabled = true;
        internal static Boolean Disabled { get { return !Enabled; } }

        internal static Boolean GUISetupDone=false;


        internal static void OnGUIOnceOnly()
        {
            if (Disabled) return;

            if (!GUISetupDone)
            {
                InitTextures();
                InitStyles();
                GUISetupDone = true;
            }
        }


        /// <summary>
        /// Called at Start of DrawWindow routine
        /// </summary>
        /// <param name="editorScroll">Scroll Position of the scrol window</param>
        internal static void WindowBegin(Vector2 editorScroll)
        {
            if (Disabled) return;

            MousePosition = Event.current.mousePosition;
            ScrollPosition = editorScroll;
            
            if (Event.current.type == EventType.Repaint)
            {
                lstGroups = new GroupDetailsList();
                lstServos = new ServoDetailsList();
            }
        }

        /// <summary>
        /// Add a 20pox pad for when a handle is not visible
        /// </summary>
        internal static void PadText()
        {
            if (Enabled)
                GUILayout.Space(20);
        }

        /// <summary>
        /// Draw the Group Handle and do any maths
        /// </summary>
        /// <param name="GroupID">Index of the Group</param>
        internal static void DrawGroupHandle(String Name,Int32 GroupID,Rect windowRect)
        {
            if (Disabled) return;

            GUILayout.Label(imgDragHandle);

            if (Event.current.type == EventType.Repaint)
            {
                lstGroups.Add(Name,GroupID, GUILayoutUtility.GetLastRect(), windowRect.width);
            }
        }

        internal static void EndDrawGroup(Int32 GroupID)
        {
            Rect newRect = new Rect(lstGroups[GroupID].GroupRect);
            newRect.height =
                lstServos.Last(x => x.groupID == GroupID).ServoRect.y +
                lstServos.Last(x => x.groupID == GroupID).ServoRect.height -
                lstGroups[GroupID].GroupRect.y;
            lstGroups[GroupID].GroupRect = newRect;
        }


        /// <summary>
        /// Draw the Group Handle and do any maths
        /// </summary>
        /// <param name="GroupID">Index of the Group</param>
        /// <param name="ServoID">Index of the Servo</param>
        internal static void DrawServoHandle(String Name,Int32 GroupID, Int32 ServoID, Rect windowRect)
        {
            if (Disabled) return;

            GUILayout.Label(imgDragHandle);

            if (Event.current.type == EventType.Repaint)
            {
                lstServos.Add(Name, GroupID, ServoID, GUILayoutUtility.GetLastRect(), windowRect.width);
            }
        }

        internal static void WindowEnd(Rect windowRect)
        {
            if (Disabled) return;

            // Draw the Yellow insertion strip
            Rect InsertRect;
            if (draggingItem && ServoDragging != null) {
                Int32 InsertIndex = ServoOver.ID + (ServoOverUpper?0:1);
                if((ServoDragging.groupID!=ServoOver.groupID)&&
                    ( ServoDragging.ID!=InsertIndex && ServoDragging.ID!=InsertIndex+1))
                Single rectResMoveY;
                if (InsertIndex < lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList().Count)
                    rectResMoveY = lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList()[InsertIndex].ServoRect.y;
                else
                    rectResMoveY = lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList().Last().ServoRect.y + lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList().Last().ServoRect.height;
                InsertRect = new Rect(4,
                    rectResMoveY + 26 - ScrollPosition.y,
                    378, 9);
                GUI.Box(InsertRect, "", styleDragInsert);
            }
            else if (draggingItem && GroupDragging != null) {
                Int32 InsertIndex = GroupOver.ID + (GroupOverUpper?0:1);
                if(GroupDragging.ID!=InsertIndex && GroupDragging.ID!=InsertIndex+1)
                Single rectResMoveY;
                if (InsertIndex < lstGroups.Count)
                    rectResMoveY = lstGroups[InsertIndex].GroupRect.y;
                else
                    rectResMoveY = lstGroups.Last().GroupRect.y + lstGroups.Last().GroupRect.height;
                InsertRect = new Rect(4,
                    rectResMoveY + 26 - ScrollPosition.y,
                    378, 9);
                GUI.Box(InsertRect, "", styleDragInsert);
            }


            //What is the mouse over
            if (MousePosition.y > zTriggerTweaks.intTest1 && MousePosition.y < windowRect.height - zTriggerTweaks.intTest2)
            {
                GroupOver = lstGroups.FirstOrDefault(x => x.GroupRect.Contains(MousePosition + ScrollPosition - new Vector2(8,29)));
                GroupIconOver = lstGroups.FirstOrDefault(x => x.IconRect.Contains(MousePosition + ScrollPosition - new Vector2(8,29)));
                if(GroupOver!=null)
                    GroupOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 29)).y - GroupOver.GroupRect.y) < GroupOver.GroupRect.height / 2;

                ServoOver = lstServos.FirstOrDefault(x => x.ServoRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                ServoIconOver = lstServos.FirstOrDefault(x => x.IconRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                if (ServoOver != null)
                    ServoOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 29)).y - ServoOver.ServoRect.y) < ServoOver.ServoRect.height / 2;

                //Will the drop actually change the list
                //DropWillReorderList = (resourceInsertIndex != resourceDragIndex) && (resourceInsertIndex != resourceDragIndex + 1);
            }
            else { GroupOver = null; ServoOver = null; GroupIconOver = null; ServoIconOver = null; }

            //MouseDown
            if (Event.current.type == EventType.mouseDown &&
                Event.current.button == 0)
            {
                if (GroupIconOver != null)
                {
                    Debug.Log("draggrouop");
                    GroupDragging = GroupOver;
                    //resourceDragIndex = lstResPositions.FindIndex(x => x.id == resourceDrag.id);
                    draggingItem = true;
                    DropWillReorderList = false;
                }
                else if (ServoIconOver != null)
                {
                    ServoDragging = ServoOver;
                    //resourceDragIndex = lstResPositions.FindIndex(x => x.id == resourceDrag.id);
                    draggingItem = true;
                    DropWillReorderList = false;
                } 
            }
                       //did we release the mouse
            if (Event.current.type == EventType.mouseUp &&
                Event.current.button == 0)
            {
                Debug.Log("Drop");
                if (GroupOver != null)
                {
                    Debug.Log(GroupOver.ID);
                }
                if (ServoOver != null)
                {
                    Debug.Log(string.Format("{0}-{1}",ServoOver.groupID,ServoOver.ID));
                }
                draggingItem = false;
                GroupDragging = null;
                ServoDragging = null;
            }
        }


        internal static void OnGUIEvery(Rect windowRect)
        {
            if (Disabled) return;

            //disable resource dragging if we mouseup outside the window
            if (Event.current.type == EventType.mouseUp &&
                Event.current.button == 0 &&
                !windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {

                draggingItem = false;
                GroupDragging = null;
                ServoDragging = null;
            }

            //If we are dragging, show what we are dragging
            if (draggingItem && (ServoDragging != null || GroupDragging!=null) {
                //set the Style
                GUIStyle styleResMove = imgBackground;
                styleResMove.alignment = TextAnchor.MiddleLeft;

                //set and draw the text like a tooltip
                String Message = "  Moving";
                if(GroupDragging!=null) Message += GroupDragging.Name;
                if(ServoDragging!=null) Message += ServoDragging.Name;
                Rect LabelPos = new Rect(Input.mousePosition.x - 5, Screen.height - Input.mousePosition.y - 9, 120, 22);
                GUI.Label(LabelPos, Message, SkinsLibrary.CurrentTooltip);

                //If its a resourcethen draw the icon too
                GUIContent contIcon = new GUIContent(imgDrag);
                Rect ResPos = new Rect(Input.mousePosition.x + 55, Screen.height - Input.mousePosition.y - 6, 32, 16);
                GUI.Box(ResPos, contIcon, new GUIStyle());

                //On top of everything
                GUI.depth = 0;
            }

        }



        internal static Vector2 MousePosition;
        internal static Vector2 ScrollPosition;

        internal static Boolean draggingItem = false;
        internal static Boolean DropWillReorderList = false;


        internal static GroupDetails GroupOver;
        internal static Boolean GroupOverUpper;
        internal static GroupDetails GroupIconOver;
        internal static GroupDetails GroupDragging;

        internal static GroupDetailsList lstGroups = new GroupDetailsList();

        internal class GroupDetails
        {
            public String Name { get; set; }
            public Int32 ID { get; set; }
            public Rect IconRect { get; set; }
            public Rect GroupRect { get; set; }
        }
        internal class GroupDetailsList : List<GroupDetails>
        {

            internal void Add(String Name,Int32 GroupID, Rect iconRect,Single windowWidth)
            {
                GroupDetails newG = new GroupDetails();
                newG.Name = Name; 
                newG.ID = GroupID;
                newG.IconRect = iconRect;
                newG.GroupRect = new Rect(iconRect) { width = windowWidth - 50 };
                this.Add(newG);
            }
        }


        internal static ServoDetails ServoOver;
        internal static ServoDetails ServoOverUpper;
        internal static ServoDetails ServoIconOver;
        internal static ServoDetails ServoDragging;

        internal static ServoDetailsList lstServos = new ServoDetailsList();

        internal class ServoDetails
        {
            public String Name { get; set; }
            public Int32 ID { get; set; }
            public Int32 groupID { get; set; }
            public Rect IconRect { get; set; }
            public Rect ServoRect { get; set; }

        }
        internal class ServoDetailsList : List<ServoDetails>
        {
            internal void Add(String Name,Int32 GroupID, Int32 ServoID, Rect iconRect, Single windowWidth)
            {
                ServoDetails newS = new ServoDetails();
                newS.Name = Name;
                newS.ID = ServoID;
                newS.groupID = GroupID;
                newS.IconRect = iconRect;
                newS.ServoRect = new Rect(iconRect) { width = windowWidth - 80 };
                this.Add(newS);
            }

        }




        #region Texture Stuff
        internal static Texture2D imgDrag = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D imgDragHandle = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D imgDragInsert = new Texture2D(18, 9, TextureFormat.ARGB32, false);

        internal static Texture2D imgBackground = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        private static void InitTextures()
        {
            LoadImageFromFile(ref imgDrag,"icon_drag.png");
            LoadImageFromFile(ref imgDragHandle,"icon_dragHandle.png");
            LoadImageFromFile(ref imgDragInsert, "icon_dragInsert.png");
            LoadImageFromFile(ref imgBackground, "icon_backgroundpng");
        }

        internal static GUIStyle styleDragInsert;
        private static void InitStyles()
        {

            styleDragInsert = new GUIStyle();
            styleDragInsert.active.background = imgDragInsert;
            styleDragInsert.border = new RectOffset(8, 8, 3, 3);
        }

        private static Boolean LoadImageFromFile(ref Texture2D tex, String FileName)
        {
            //Set the Path variables
            String PluginPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            String PathPluginTextures = String.Format("{0}/../Textures", PluginPath);
            Boolean blnReturn = false;
            try
            {
                //File Exists check
                if (System.IO.File.Exists(String.Format("{0}/{1}", PathPluginTextures, FileName)))
                {
                    try
                    {
                        Debug.Log(String.Format("[IR GUI] Loading: {0}", String.Format("{0}/{1}", PathPluginTextures, FileName)));
                        tex.LoadImage(System.IO.File.ReadAllBytes(String.Format("{0}/{1}", PathPluginTextures, FileName)));
                        blnReturn = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(String.Format("Failed to load the texture:{0} ({1})", String.Format("{0}/{1}", PathPluginTextures, FileName), ex.Message));
                    }
                }
                else
                {
                    Debug.Log(String.Format("Cannot find texture to load:{0}", String.Format("{0}/{1}", PathPluginTextures, FileName)));
                }


            }
            catch (Exception ex)
            {
                Debug.Log(String.Format("Failed to load (are you missing a file):{0} ({1})", String.Format("{0}/{1}", PathPluginTextures, FileName), ex.Message));
            }
            return blnReturn;
        }
        #endregion
    }
}
