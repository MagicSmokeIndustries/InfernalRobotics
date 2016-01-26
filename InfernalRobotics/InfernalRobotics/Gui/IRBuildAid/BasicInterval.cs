using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics.Gui.IRBuildAid
{
    [RequireComponent(typeof(LineRenderer))]
    public class BasicInterval : LinePrimitive
    {
        // a basic interval like so |------|
        bool holdUpdate = true;

        protected Vector3 lineVector = Vector3.zero;

        protected LineRenderer mainLine;
        protected LineRenderer endPoint1, endPoint2;

        public float offset;
        public Vector3 mainStartPoint { get; private set; }
        public Vector3 mainEndPoint { get; private set; }

        protected float length;
        protected float width = 4;

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
                endPoint1 = GetComponent<LineRenderer> ();
                endPoint1.material = material;
                lineRenderers.Add (endPoint1);

                endPoint2 = GetComponent<LineRenderer> ();
                endPoint2.material = material;
                lineRenderers.Add (endPoint2);
            } 
            else 
            {
                mainLine = lineRenderers [0];
                endPoint1 = lineRenderers [1];
                endPoint2 = lineRenderers [2];
            }


            mainLine.SetVertexCount (2);
            endPoint1.SetVertexCount(2);
            endPoint2.SetVertexCount(2);
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
                Vector3 cross = Vector3.Cross (lineVector, transform.up);

                mainStartPoint = transform.position + norm * offset;
                mainEndPoint = mainStartPoint + norm * length;

                mainLine.SetPosition (0, mainStartPoint);
                mainLine.SetPosition (1, mainEndPoint);

                endPoint1.SetPosition (0, mainStartPoint + cross * width * 4);
                endPoint1.SetPosition (1, mainStartPoint - cross * width * 4);

                endPoint2.SetPosition (0, mainEndPoint + cross * width * 4);
                endPoint2.SetPosition (1, mainEndPoint - cross * width * 4);

            }
        }
    }
}

