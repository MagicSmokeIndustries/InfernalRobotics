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

        //Internal flag so we only set up textures, etc once
        internal static Boolean GUISetupDone=false;

        //This is called all the time, but only run once per scene
        internal static void OnGUIOnceOnly()
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back

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
        /// <param name="windowRect">Rect of the window</param>
        /// <param name="editorScroll">Scroll Position of the scroll window</param>
        internal static void WindowBegin(Rect windowRect,Vector2 editorScroll)
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back

            //store the Position of the mouse and window elements for use later
            MousePosition = new Vector2(Input.mousePosition.x - windowRect.x,Screen.height-Input.mousePosition.y - windowRect.y);
            ScrollPosition = editorScroll;
            WindowRect = windowRect;
            if (Event.current.type == EventType.Repaint)
            {
                lstGroups = new GroupDetailsList();
                lstServos = new ServoDetailsList();
            }

            rectScrollTop = new Rect(8, zTriggerTweaks.intTest1, windowRect.width - zTriggerTweaks.intTest3, 15);
            rectScrollBottom = new Rect(8, windowRect.height - zTriggerTweaks.intTest2, windowRect.width - zTriggerTweaks.intTest3, 15);
        }

        /// <summary>
        /// Add a 20pox pad for when a handle is not visible and Drag and Drop is enabled
        /// </summary>
        internal static void PadText()
        {
            if (Enabled)
                GUILayout.Space(20);
        }

        /// <summary>
        /// Draw the Group Handle and do any maths
        /// </summary>
        /// <param name="Name">String name of the Group</param>
        /// <param name="GroupID">Index of the Group</param>
        internal static void DrawGroupHandle(String Name,Int32 GroupID)
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back

            //Draw the drag handle
            GUILayout.Label(imgDragHandle);

            if (Event.current.type == EventType.Repaint)
            {
                //If its the repaint event then use GUILayoutUtility to get the location of the handle
                //And build the structure so we know where on the screen it is
                lstGroups.Add(Name,GroupID, GUILayoutUtility.GetLastRect(), WindowRect.width);
            }
        }

        /// <summary>
        /// Called after we draw the last servo in a group
        /// </summary>
        /// <param name="GroupID">What Group was it</param>
        internal static void EndDrawGroup(Int32 GroupID)
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back
            
            //Only do this if there is a group that contains Servos
            if (lstGroups.Count < 1 || lstServos.Count < 1 || !lstServos.Any(x=>x.groupID==GroupID)) return;

            try
            {
                //Set the height of the group to contain all the servos
                Rect newRect = new Rect(lstGroups[GroupID].GroupRect);
                newRect.height =
                    lstServos.Last(x => x.groupID == GroupID).ServoRect.y +
                    lstServos.Last(x => x.groupID == GroupID).ServoRect.height -
                    lstGroups[GroupID].GroupRect.y;
                lstGroups[GroupID].GroupRect = newRect;

            }
            catch (Exception)
            {

            }
        }


        /// <summary>
        /// Draw the Group Handle and do any maths
        /// </summary>
        /// <param name="Name">Text name of the Servo</param>
        /// <param name="GroupID">Index of the Group</param>
        /// <param name="ServoID">Index of the Servo</param>
        internal static void DrawServoHandle(String Name,Int32 GroupID, Int32 ServoID)
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back

            //Draw the drag handle
            GUILayout.Label(imgDragHandle);

            if (Event.current.type == EventType.Repaint) {
                //If its the repaint event then use GUILayoutUtility to get the location of the handle
                //And build the structure so we know where on the screen it is
                lstServos.Add(Name, GroupID, ServoID, GUILayoutUtility.GetLastRect(), WindowRect.width);
            }
        }

        internal static void WindowEnd()
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back

            // Draw the Yellow insertion strip
            Rect InsertRect;
            if (draggingItem && ServoDragging != null && ServoOver!=null) {
                //What is the insert position of the dragged item
                Int32 InsertIndex = ServoOver.ID + (ServoOverUpper?0:1);
                if((ServoDragging.groupID!=ServoOver.groupID)||
                    ( ServoDragging.ID!=InsertIndex && (ServoDragging.ID+1)!=InsertIndex)){
                    //Only in here if the drop will cause the list to change 
                    Single rectResMoveY;
                    //is it dropping in the list or at the end
                    if (InsertIndex < lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList().Count)
                        rectResMoveY = lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList()[InsertIndex].ServoRect.y;
                    else
                        rectResMoveY = lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList().Last().ServoRect.y + lstServos.Where(x=>x.groupID==ServoOver.groupID).ToList().Last().ServoRect.height;
                    
                    //calculate and draw the graphic
                    InsertRect = new Rect(12,
                        rectResMoveY + 26  - ScrollPosition.y,
                        WindowRect.width-34, 9);
                    GUI.Box(InsertRect, "", styleDragInsert);
                }
            }
            else if (draggingItem && GroupDragging != null && GroupOver!=null)
            {
                //What is the insert position of the dragged item
                Int32 InsertIndex = GroupOver.ID + (GroupOverUpper ? 0 : 1);
                if (GroupDragging.ID != InsertIndex && (GroupDragging.ID+ 1) != InsertIndex)
                {
                    //Only in here if the drop will cause the list to change 
                    Single rectResMoveY;
                    //is it dropping in the list or at the end
                    if (InsertIndex < lstGroups.Count)
                        rectResMoveY = lstGroups[InsertIndex].GroupRect.y;
                    else
                        rectResMoveY = lstGroups.Last().GroupRect.y + lstGroups.Last().GroupRect.height;

                    //calculate and draw the graphic
                    InsertRect = new Rect(12,
                        rectResMoveY + 26  - ScrollPosition.y,
                        WindowRect.width - 34, 9);
                    GUI.Box(InsertRect, "", styleDragInsert);
                }
            }


            //What is the mouse over
            //if (MousePosition.y > zTriggerTweaks.intTest1 && MousePosition.y < windowRect.height - zTriggerTweaks.intTest2)
            hghdgjhagdgahgdja
            if (MousePosition.y > zTriggerTweaks.intTest1 && MousePosition.y < WindowRect.height - zTriggerTweaks.intTest2)
            {
                //inside the scrollview
                //check what group
                GroupOver = lstGroups.FirstOrDefault(x => x.GroupRect.Contains(MousePosition + ScrollPosition - new Vector2(8,29)));
                GroupIconOver = lstGroups.FirstOrDefault(x => x.IconRect.Contains(MousePosition + ScrollPosition - new Vector2(8,29)));
                if(GroupOver!=null)
                    GroupOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 29)).y - GroupOver.GroupRect.y) < GroupOver.GroupRect.height / 2;

                //or servo
                ServoOver = lstServos.FirstOrDefault(x => x.ServoRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                ServoIconOver = lstServos.FirstOrDefault(x => x.IconRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                if (ServoOver != null)
                    ServoOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 29)).y - ServoOver.ServoRect.y) < ServoOver.ServoRect.height / 2;

            } else {
                //Otherwise empty the variables
                GroupOver = null; ServoOver = null; GroupIconOver = null; ServoIconOver = null; 
            }

            //MouseDown - left mouse button
            if (Event.current.type == EventType.mouseDown &&
                Event.current.button == 0)
            {
                if (GroupIconOver != null)
                {
                    //If we click on the drag icon then start the drag
                    GroupDragging = GroupOver;
                    draggingItem = true;
                    DropWillReorderList = false;
                }
                else if (ServoIconOver != null)
                {
                    //If we click on the drag icon then start the drag
                    ServoDragging = ServoOver;
                    draggingItem = true;
                    DropWillReorderList = false;
                } 
            }

            //did we release the mouse
            if (Event.current.type == EventType.mouseUp &&
                Event.current.button == 0)
            {
                if (GroupDragging!=null && GroupOver != null)
                {
                    //were we dragging a group
                    if (GroupDragging.ID != (GroupOver.ID + (GroupOverUpper ? 0 : 1)))
                    {
                        //And it will cause a reorder
                        Debug.Log(String.Format("Reordering:{0}-{1}", GroupDragging.ID, (GroupOver.ID - (GroupOverUpper ? 1 : 0))));

                        //where are we inserting the dragged item
                        Int32 InsertAt = (GroupOver.ID - (GroupOverUpper ? 1 : 0));
                        if (GroupOver.ID < GroupDragging.ID) InsertAt += 1;

                        //move em around
                        MuMechGUI.Group g = MuMechGUI.gui.servo_groups[GroupDragging.ID];
                        MuMechGUI.gui.servo_groups.RemoveAt(GroupDragging.ID);
                        MuMechGUI.gui.servo_groups.Insert(InsertAt, g);

                    }
                }
                if (ServoDragging!=null && ServoOver != null)
                {
                    //were we dragging a servo
                    Debug.Log(String.Format("Reordering:({0}-{1})->({2}-{3})", ServoDragging.groupID, ServoDragging.ID, ServoOver.groupID,ServoOver.ID));
sjhajdhkjajdhkad
    //do we need to do a reorder if tsatement
                    
                    //where are we inserting the dragged item
                    Int32 InsertAt = (ServoOver.ID + (ServoOverUpper ? 0 : 1));
                    if (ServoOver.groupID == ServoDragging.groupID && ServoDragging.ID < ServoOver.ID)
                        InsertAt -= 1;

                    Debug.Log(String.Format("Reordering:({0}-{1})->({2}-{3})", ServoDragging.groupID, ServoDragging.ID, ServoOver.groupID, InsertAt));

                    //move em around
                    MuMechToggle s = MuMechGUI.gui.servo_groups[ServoDragging.groupID].servos[ServoDragging.ID];
                    MuMechGUI.gui.servo_groups[ServoDragging.groupID].servos.RemoveAt(ServoDragging.ID);
                    MuMechGUI.gui.servo_groups[ServoOver.groupID].servos.Insert(InsertAt, s);
                }

                //reset the dragging stuff
                draggingItem = false;
                GroupDragging = null;
                ServoDragging = null;
            }

            //If we are dragging and in the bottom or top area then scrtoll the list
            hgahjgfhaghjfa - //do we need to set the actual variable
            if (draggingItem && rectScrollBottom.Contains(MousePosition))
                ScrollPosition.y += (Time.deltaTime * 40);
            if (draggingItem && rectScrollTop.Contains(MousePosition))
                ScrollPosition.y -= (Time.deltaTime * 40);
        }


        internal static void OnGUIEvery()
        {
            if (Disabled) return;       //If the Drag and Drop is Disabled then just go back

            //disable resource dragging if we mouseup outside the window
            if (Event.current.type == EventType.mouseUp &&
                Event.current.button == 0 &&
                !WindowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {

                draggingItem = false;
                GroupDragging = null;
                ServoDragging = null;
            }

            //If we are dragging, show what we are dragging
            if (draggingItem && (ServoDragging != null || GroupDragging!=null)) {
                //set the Style
                //set and draw the text like a tooltip
                String Message = "Moving ";
                if(GroupDragging!=null) Message += " group: " + GroupDragging.Name;
                if(ServoDragging!=null) Message += " servo: " + ServoDragging.Name;
                Rect LabelPos = new Rect(Input.mousePosition.x - 5, Screen.height - Input.mousePosition.y - 9, 200, 22);
                GUI.Label(LabelPos, Message, styleDragTooltip);

                //On top of everything
                GUI.depth = 0;
            }

        }


        //Variables to hold details of the drag stuff
        internal static Vector2 MousePosition;
        internal static Vector2 ScrollPosition;
        internal static Rect WindowRect;
        internal static Rect rectScrollTop;
        internal static Rect rectScrollBottom;

        internal static Boolean draggingItem = false;

        #region Group Objects
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
                newG.GroupRect = new Rect(iconRect) {y=iconRect.y-5, width = windowWidth - 50 };
                this.Add(newG);
            }
        }
        #endregion


        #region Servo Objects
        internal static ServoDetails ServoOver;
        internal static Boolean ServoOverUpper;
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
                newS.ServoRect = new Rect(iconRect) {y=iconRect.y - 5, width = windowWidth - 80,height=iconRect.height + 7 };
                this.Add(newS);
            }

        }
        #endregion


        #region Texture Stuff
        internal static Texture2D imgDrag = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D imgDragHandle = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D imgDragInsert = new Texture2D(18, 9, TextureFormat.ARGB32, false);

        internal static Texture2D imgBackground = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        /// <summary>
        /// Load the textures from file to memory
        /// </summary>
        private static void InitTextures()
        {
            LoadImageFromFile(ref imgDrag,"icon_drag.png");
            LoadImageFromFile(ref imgDragHandle,"icon_dragHandle.png");
            LoadImageFromFile(ref imgDragInsert, "icon_dragInsert.png");
            LoadImageFromFile(ref imgBackground, "icon_background.png");
        }

        internal static GUIStyle styleDragInsert;
        internal static GUIStyle styleDragTooltip;

        /// <summary>
        /// setup the styles that use textures
        /// </summary>
        private static void InitStyles()
        {
            //the border determins which bit doesnt repeat
            styleDragInsert = new GUIStyle();
            styleDragInsert.normal.background = imgDragInsert;
            styleDragInsert.border = new RectOffset(8, 8, 3, 3);

            styleDragTooltip = new GUIStyle();
            styleDragTooltip.fontSize = 12;
            styleDragTooltip.normal.textColor = new Color32(207, 207, 207, 255);
            styleDragTooltip.stretchHeight = true;
            styleDragTooltip.normal.background = imgBackground;
            styleDragTooltip.border = new RectOffset(3, 3, 3, 3);
            styleDragTooltip.padding = new RectOffset(4, 4, 6, 4);
            styleDragTooltip.alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>
        /// Use System.IO.File to read a file into a texture in RAM. Path is relative to the DLL
        /// 
        /// Do it this way so the images are not affected by compression artifacts or Texture quality settings
        /// </summary>
        /// <param name="tex">Texture to load</param>
        /// <param name="FileName">Filename of the image in side the Textures folder</param>
        /// <returns></returns>
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
                        Debug.Log(String.Format("[IR GUI] Failed to load the texture:{0} ({1})", String.Format("{0}/{1}", PathPluginTextures, FileName), ex.Message));
                    }
                }
                else
                {
                    Debug.Log(String.Format("[IR GUI] Cannot find texture to load:{0}", String.Format("{0}/{1}", PathPluginTextures, FileName)));
                }


            }
            catch (Exception ex)
            {
                Debug.Log(String.Format("[IR GUI] Failed to load (are you missing a file):{0} ({1})", String.Format("{0}/{1}", PathPluginTextures, FileName), ex.Message));
            }
            return blnReturn;
        }
        #endregion
    }
}
