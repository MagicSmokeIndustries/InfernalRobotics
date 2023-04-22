using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;


namespace InfernalRobotics_v3.Utility
{
	public struct LineDrawer
	{
		private LineRenderer lineRenderer;
		private float lineSize;

		public LineDrawer(Transform parent = null, float lineSize = 0.02f)
		{
			GameObject lineObj = new GameObject("LineObj");
			lineRenderer = lineObj.AddComponent<LineRenderer>();

			lineRenderer.transform.parent = parent;

			lineRenderer.transform.localPosition = Vector3.zero;
			lineRenderer.transform.localRotation = Quaternion.identity;

			lineRenderer.useWorldSpace = false;

			// particles / additive
			lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

			this.lineSize = lineSize;

			lineRenderer.enabled = false;
		}

		public void Destroy()
		{
			if(lineRenderer != null)
				UnityEngine.Object.Destroy(lineRenderer.gameObject);
		}

		// draws the line through the provided vertices
		public void Draw(Vector3 start, Vector3 end, Color color)
		{
			if(lineRenderer == null)
				return;

			lineRenderer.startColor = color; lineRenderer.endColor = color;
			lineRenderer.startWidth = lineSize; lineRenderer.endWidth = lineSize;

			// set line count which is 2
			lineRenderer.SetPosition(0, lineRenderer.transform.InverseTransformPoint(start));
			lineRenderer.SetPosition(1, lineRenderer.transform.InverseTransformPoint(end));

			lineRenderer.enabled = true;
		}

		// hides the line
		public void Hide()
		{
			lineRenderer.enabled = false;
		}
	}

	public class MultiLineDrawer
	{
		public LineDrawer[] al = new LineDrawer[13];
		public Color[] alColor = new Color[13];

		public void Create(Transform t)
		{
			for(int i = 0; i < 13; i++)
				al[i] = new LineDrawer(t);

			alColor[0] = Color.red;
			alColor[1] = Color.green;
			alColor[2] = Color.yellow;
			alColor[3] = Color.magenta; // axis
			alColor[4] = Color.blue;    // secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(255.0f / 255.0f, 128.0f / 255.0f, 0f / 255.0f);
			alColor[7] = new Color(0f / 255.0f, 128.0f / 255.0f, 0f / 255.0f);
			alColor[8] = new Color(0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
			alColor[9] = new Color(128.0f / 255.0f, 0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(255.0f / 255.0f, 0f / 255.0f, 128.0f / 255.0f);
			alColor[11] = new Color(0f / 255.0f, 0f / 255.0f, 128.0f / 255.0f);
			alColor[12] = new Color(128.0f / 255.0f, 0f / 255.0f, 128.0f / 255.0f);
		}

		public void Destroy()
		{
			for(int i = 0; i < 13; i++)
				al[i].Destroy();
		}

		// draws the line through the provided vertices
		public void Draw(int color, Vector3 start, Vector3 end)
		{
			al[color].Draw(start, end, alColor[color]);
		}

		// hides the line
		public void Hide(int color)
		{
			al[color].Hide();
		}

		// hides all lines
		public void Hide()
		{
			for(int i = 0; i < al.Length; i++)
				al[i].Hide();
		}
	}
}
