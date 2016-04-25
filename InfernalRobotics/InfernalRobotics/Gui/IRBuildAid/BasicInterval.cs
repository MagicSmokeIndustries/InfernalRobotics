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

        public Color endPoint1Color = new Color(1f, 1f, 0, 0.5f);
        public Color endPoint2Color = new Color(1f, 1f, 0, 0.5f);

        protected LineRenderer currentPosMarker, defaultPosMarker;

        protected List<LineRenderer> presetsPosMarkers = new List<LineRenderer>();

        public float offset;
        public Vector3 mainStartPoint;
        public Vector3 mainEndPoint;

        public float length;
        public float width = 0.25f;

        public float currentPosition = 0f;
        public Color currentPositionColor = new Color(0f, 1f, 0f, 0.5f);

        public float defaultPosition = 0f;

        public List<float> presetPositions = new List<float>();
        public Color presetPositionsColor = new Color(1f, 0.75f, 0f, 0.5f);
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

            presetsPosMarkers.Clear();

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

        public void SetMainLineColors(Color startColor, Color endColor)
        {
            mainLine.SetColors(startColor, endColor);
        }

        public void SetPresetPositions(List<float> newList)
        {
            for (int i = 0; i < presetsPosMarkers.Count; i++)
            {
                //remove outdated linerenders
                Destroy(presetsPosMarkers[i]);
            }
            presetsPosMarkers.Clear();

            presetPositions = newList;

            for (int i = 0; i < presetPositions.Count; i++)
            {
                var pos = presetPositions[i];
                var posRenderer = CreateNewRenderer();
                posRenderer.material = material;
                presetsPosMarkers.Add(posRenderer);
                posRenderer.SetVertexCount(2);
                posRenderer.SetColors(presetPositionsColor, presetPositionsColor);
                posRenderer.gameObject.layer = gameObject.layer;
            }
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

                endPoint1.SetColors(endPoint1Color, endPoint1Color);

                endPoint2.SetPosition (0, mainEndPoint + cross * width * 2);
                endPoint2.SetPosition (1, mainEndPoint - cross * width * 2);

                endPoint2.SetColors(endPoint2Color, endPoint2Color);

                currentPosMarker.SetWidth (width*2, 0.01f);

                currentPosMarker.SetColors(currentPositionColor, currentPositionColor);

                currentPosMarker.SetPosition (0, currentPosPoint - cross * width * 2);
                currentPosMarker.SetPosition (1, currentPosPoint);

                defaultPosMarker.SetWidth (width*2, 0.01f);

                defaultPosMarker.SetPosition (0, defaultPosPoint + cross * width * 2);
                defaultPosMarker.SetPosition (1, defaultPosPoint);

                defaultPosMarker.SetColors(endPoint1Color, endPoint1Color);

                for (int i = 0; i < presetsPosMarkers.Count; i++)
                {
                    var pos = presetPositions[i];
                    var posMarker = presetsPosMarkers[i];
                    var posPoint = transform.position + norm * pos;
                    posMarker.SetWidth(width * 0.5f, width * 0.5f);
                    posMarker.SetPosition(0, posPoint - cross * width * 2.5f);
                    posMarker.SetPosition(1, posPoint);
                    posMarker.SetColors(presetPositionsColor, presetPositionsColor);
                }
            }
        }
    }
}

