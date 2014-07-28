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

        internal static void WindowBegin()
        {
            if (Disabled) return;
            if (Event.current.type == EventType.Repaint) {
                MousePosition = Event.current.mousePosition;
                ScrollPosition = editorScroll;

                lstGroupPositions = new GroupPosList();
                lstServoPositions = new ServoPosList();
            }
        }

        internal static void PadText()
        {
            if (Enabled)
                GUILayout.Space(20);
        }

        internal static void DrawGroupHandle(Int32 GroupID)
        {
            if (Disabled) return;

            GUILayout.Label(new GUIContent(GameDatabase.Instance.GetTexture("MagicSmokeIndustries/Textures/icon_dragHandle", false)));
            

        }

        internal static void DrawServoHandle(Int32 GroupID,Int32 ServoID)
        {
            if (Disabled) return;

            GUILayout.Label(new GUIContent(GameDatabase.Instance.GetTexture("MagicSmokeIndustries/Textures/icon_dragHandle", false)));
            lstServos.Add(servo.servoName, i, servo == grp.servos.Last());
        }

        internal static void WindowEnd()
        {
            if (Disabled) return;
        }




        internal static Boolean Enabled = true;
        internal static Boolean Disabled { get { return !Enabled; } }

        internal static Vector2 MousePosition;
        internal static Vector2 ScrollPosition;

        internal static Boolean draggingItem = false;

        internal static ServoDetails servoOver;
        internal static ServoDetails servoIconOver;
        internal static ServoDetails servoDragging;

        internal static ServoDetailsList lstServos;

        internal class ServoDetails
        {

        }
        internal class ServoDetailsList : List<ServoDetails>
        {

        }
    }
}
