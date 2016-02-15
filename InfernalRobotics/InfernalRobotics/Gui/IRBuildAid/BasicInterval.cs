using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics.Gui.IRBuildAid
{
    [RequireComponent(typeof(LineRenderer))]
    public class BasicInterval : LinePrimitive
    {
        // a basic interval like so |------|
        // constructed out of 3 lines
        bool holdUpdate = true;

        public Vector3 lineVector = Vector3.zero;

        protected LineRenderer mainLine;
        protected LineRenderer endPoint1, endPoint2;

        protected LineRenderer currentPosMarker, defaultPosMarker;

        public float offset;
        public Vector3 mainStartPoint;
        public Vector3 mainEndPoint;

        public float length;
        public float width = 0.25f;

        public float currentPosition = 0f;
        public float defaultPosition = 0f;

        public float[] presetPositions;

        public override bool enabled 
        {
            get { return base.enabled; }
            set {
                base.enabled = value;
                if (!holdUpdate || !value) 
                {
                    EnableRenderers (value);
                }
            }
        }
            
        protected override void Awake ()
        {
            base.Awake ();

            if (lineRenderers.Count == 0) 
            {
                //main line
                mainLine = GetComponent<LineRenderer> ();
                mainLine.material = material;
                lineRenderers.Add (mainLine);

                //two endpoint lines
                endPoint1 = CreateNewRenderer();
                endPoint1.material = material;
                lineRenderers.Add (endPoint1);

                endPoint2 = CreateNewRenderer();
                endPoint2.material = material;
                lineRenderers.Add (endPoint2);

                //two position markers
                currentPosMarker = CreateNewRenderer();
                currentPosMarker.material = material;
                lineRenderers.Add (currentPosMarker);

                defaultPosMarker = CreateNewRenderer();
                defaultPosMarker.material = material;
                lineRenderers.Add (defaultPosMarker);
            } 
            else 
            {
                mainLine = lineRenderers [0];
                endPoint1 = lineRenderers [1];
                endPoint2 = lineRenderers [2];

                currentPosMarker = lineRenderers [3];
                defaultPosMarker = lineRenderers [4];
            }


            mainLine.SetVertexCount (2);
            endPoint1.SetVertexCount(2);
            endPoint2.SetVertexCount(2);

            currentPosMarker.SetVertexCount(2);
            defaultPosMarker.SetVertexCount(2);
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();
            EnableRenderers (!holdUpdate);
            holdUpdate = false;

            if (mainLine.enabled) 
            {
                UpdateWidth (width);

                Vector3 norm = lineVector.normalized;
                Vector3 cross = Vector3.Cross (norm, transform.up);

                mainStartPoint = transform.position + norm * offset;
                mainEndPoint = mainStartPoint + norm * length;

                var currentPosPoint = transform.position + norm * currentPosition;
                var defaultPosPoint = transform.position + norm * defaultPosition;

                mainLine.SetPosition (0, mainStartPoint);
                mainLine.SetPosition (1, mainEndPoint);

                endPoint1.SetPosition (0, mainStartPoint + cross * width * 2);
                endPoint1.SetPosition (1, mainStartPoint - cross * width * 2);

                endPoint2.SetPosition (0, mainEndPoint + cross * width * 2);
                endPoint2.SetPosition (1, mainEndPoint - cross * width * 2);

                currentPosMarker.SetWidth (width*2, 0.01f);

                var c = new Color (0f, 1f, 0f, 0.5f);
                currentPosMarker.SetColors (c, c);

                currentPosMarker.SetPosition (0, currentPosPoint - cross * width * 2);
                currentPosMarker.SetPosition (1, currentPosPoint);

                defaultPosMarker.SetWidth (width*2, 0.01f);

                defaultPosMarker.SetPosition (0, defaultPosPoint + cross * width * 2);
                defaultPosMarker.SetPosition (1, defaultPosPoint);
            }
        }
    }
}

