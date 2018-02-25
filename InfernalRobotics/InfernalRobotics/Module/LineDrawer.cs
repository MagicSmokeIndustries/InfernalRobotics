using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace InfernalRobotics_v3.Module
{
	public struct LineDrawer
	{
		private LineRenderer lineRenderer;
		private float lineSize;

		public LineDrawer(float lineSize = 0.02f)
		{
			GameObject lineObj = new GameObject("LineObj");
			lineRenderer = lineObj.AddComponent<LineRenderer>();
			//Particles/Additive
			lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

			this.lineSize = lineSize;
		}

		private void init(float lineSize = 0.02f)
		{
			if(lineRenderer == null)
			{
				GameObject lineObj = new GameObject("LineObj");
				lineRenderer = lineObj.AddComponent<LineRenderer>();
				//Particles/Additive
				lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

				this.lineSize = lineSize;
			}
		}

		//Draws lines through the provided vertices
		public void DrawLineInGameView(Vector3 start, Vector3 end, Color color)
		{
			if(lineRenderer == null)
			{
				init(0.02f);
			}

			//Set color
			lineRenderer.SetColors(color, color);

			//Set width
			lineRenderer.SetWidth(lineSize, lineSize);

			//Set line count which is 2
			lineRenderer.SetPosition(0, start);
			lineRenderer.SetPosition(1, end);
		}

		public void Destroy()
		{
			if(lineRenderer != null)
			{
				UnityEngine.Object.Destroy(lineRenderer.gameObject);
			}
		}
	}
}
