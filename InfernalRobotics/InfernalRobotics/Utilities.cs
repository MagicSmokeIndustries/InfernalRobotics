/**
 * Utilities.cs
 * 
 * Thunder Aerospace Corporation's library for the Kerbal Space Program, by Taranis Elsu
 * 
 * (C) Copyright 2013, Taranis Elsu
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InfernalRobotics
{
    public class Utilities
    {
        public static Rect EnsureVisible(Rect pos, float min = 16.0f)
        {
            float xMin = min - pos.width;
            float xMax = Screen.width - min;
            float yMin = min - pos.height;
            float yMax = Screen.height - min;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect EnsureCompletelyVisible(Rect pos)
        {
            float xMin = 0;
            float xMax = Screen.width - pos.width;
            float yMin = 0;
            float yMax = Screen.height - pos.height;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect ClampToScreenEdge(Rect pos)
        {
            float topSeparation = Math.Abs(pos.y);
            float bottomSeparation = Math.Abs(Screen.height - pos.y - pos.height);
            float leftSeparation = Math.Abs(pos.x);
            float rightSeparation = Math.Abs(Screen.width - pos.x - pos.width);

            if (topSeparation <= bottomSeparation && topSeparation <= leftSeparation && topSeparation <= rightSeparation)
            {
                pos.y = 0;
            }
            else if (leftSeparation <= topSeparation && leftSeparation <= bottomSeparation && leftSeparation <= rightSeparation)
            {
                pos.x = 0;
            }
            else if (bottomSeparation <= topSeparation && bottomSeparation <= leftSeparation && bottomSeparation <= rightSeparation)
            {
                pos.y = Screen.height - pos.height;
            }
            else if (rightSeparation <= topSeparation && rightSeparation <= bottomSeparation && rightSeparation <= leftSeparation)
            {
                pos.x = Screen.width - pos.width;
            }

            return pos;
        }

        public static Texture2D LoadImage<T>(string filename)
        {
            if (File.Exists<T>(filename))
            {
                var bytes = File.ReadAllBytes<T>(filename);
                Texture2D texture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                texture.LoadImage(bytes);
                return texture;
            }
            else
            {
                return null;
            }
        }

        public static bool GetValue(ConfigNode config, string name, bool currentValue)
        {
            bool newValue;
            if (config.HasValue(name) && bool.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static float GetValue(ConfigNode config, string name, float currentValue)
        {
            float newValue;
            if (config.HasValue(name) && float.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static double GetValue(ConfigNode config, string name, double currentValue)
        {
            double newValue;
            if (config.HasValue(name) && double.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static double ShowTextField(double currentValue, int maxLength, GUIStyle style, params GUILayoutOption[] options)
        {
            double newDouble;
            string result = GUILayout.TextField(currentValue.ToString(), maxLength, style, options);
            if (double.TryParse(result, out newDouble))
            {
                return newDouble;
            }
            else
            {
                return currentValue;
            }
        }

        // Gets connected resources to a part. Note fuel lines are NOT reversible! Add flow going TO the constructing part!
        // Safe to pass any string for name - if specified resource does not exist, a Partresource with amount 0 will be returned
        public static List<PartResource> GetConnectedResources(Part part, String resourceName)
        {
            var resources = new List<PartResource>();
            // Only check for connected resources if a Resource Definition for that resource exists
            if (PartResourceLibrary.Instance.resourceDefinitions.Contains(resourceName) == true)
            {
                PartResourceDefinition res = PartResourceLibrary.Instance.GetDefinition(resourceName);
                part.GetConnectedResources(res.id,res.resourceFlowMode, resources);
            }
            // Do not return an empty list - if none of the resource found, create resource item and set amount to 0
            if (resources.Count < 1)
            {
                PartResource p = new PartResource();
                p.resourceName = resourceName;
                p.amount = (double)0;
                resources.Add(p);
            }
            return resources;
        }

    }
}