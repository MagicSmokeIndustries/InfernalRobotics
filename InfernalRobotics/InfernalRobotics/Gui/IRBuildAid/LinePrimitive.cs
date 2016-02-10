using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics.Gui.IRBuildAid
{
    public class LinePrimitive : MonoBehaviour
    {
        protected Material material;
        public const string shaderName = "Particles/Alpha Blended";
        [SerializeField]
        protected List<LineRenderer> lineRenderers = new List<LineRenderer> ();
        protected Color lineColor = Color.blue;

        public new virtual bool enabled 
        {
            get { return base.enabled; }
            set 
            {
                base.enabled = value;
                EnableRenderers (value);
            }
        }

        public virtual void UpdateColor (Color newColor) 
        {
            lineColor = newColor;
            foreach(var lr in lineRenderers) 
            {
                lr.SetColors (newColor, newColor);
            }
        }

        public virtual void UpdateWidth(float v1, float v2) 
        {
            foreach(var lr in lineRenderers) 
            {
                lr.SetWidth (v1, v2);
            }
        }

        public virtual void UpdateWidth (float v2)
        {
            UpdateWidth (v2, v2);
        }

        protected virtual void EnableRenderers (bool value)
        {
            foreach(var lr in lineRenderers) 
            {
                lr.enabled = value;
            }
        }

        protected LineRenderer CreateNewRenderer ()
        {
            var obj = new GameObject("IR BuildAid LineRenderer");
            var lr = obj.AddComponent<LineRenderer>();
            obj.transform.parent = gameObject.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            lr.material = material;
            return lr;
        }

        protected virtual void Awake ()
        {
            material = new Material (Shader.Find (shaderName));
        }

        protected virtual void Start ()
        {
            UpdateColor (lineColor);
            UpdateLayer ();
        }

        protected virtual void LateUpdate ()
        {
            CheckLayer ();
        }

        void CheckLayer ()
        {
            /* the Editor clobbers the layer's value whenever you pick the part */
            if (gameObject.layer != 1) 
            {
                UpdateLayer ();
            }
        }

        public virtual void UpdateLayer ()
        {
            gameObject.layer = 1;
            foreach(var lr in lineRenderers) 
            {
                lr.gameObject.layer = 1;
            }
        }
    }
}

