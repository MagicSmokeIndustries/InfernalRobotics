using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics_v3.Gui.IRBuildAid
{
	public class CircularInterval : BasicInterval
	{
		public float circleRadius = 1f;
		public int vertexCount = 45;

		protected override void Start()
		{
			base.Start();

			mainLine.SetVertexCount(vertexCount);
			mainLine.useWorldSpace = false;
			mainLine.SetColors(lineColor, lineColor);

			endPoint1.SetVertexCount(2);
			endPoint1.useWorldSpace = false;
			endPoint1.SetColors(lineColor, lineColor);

			endPoint2.SetVertexCount(2);
			endPoint2.useWorldSpace = false;
			endPoint2.SetColors(lineColor, lineColor);

			currentPosMarker.SetVertexCount(2);
			currentPosMarker.useWorldSpace = false;
			currentPosMarker.SetColors(lineColor, lineColor);

			defaultPosMarker.SetVertexCount(2);
			defaultPosMarker.useWorldSpace = false;
			defaultPosMarker.SetColors(lineColor, lineColor);

			for(int i = 0; i < presetsPosMarkers.Count; i++)
			{
				var posRenderer = presetsPosMarkers[i];
				posRenderer.useWorldSpace = false;
				posRenderer.SetVertexCount(2);
				posRenderer.SetColors(presetPositionsColor, presetPositionsColor);
				posRenderer.gameObject.layer = gameObject.layer;
			}

			mainLine.gameObject.layer = gameObject.layer;
			endPoint1.gameObject.layer = gameObject.layer;
			endPoint2.gameObject.layer = gameObject.layer;
			currentPosMarker.gameObject.layer = gameObject.layer;
			defaultPosMarker.gameObject.layer = gameObject.layer;
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();

			if(mainLine.enabled) 
			{
				float angleDelta = Mathf.Deg2Rad * length / vertexCount;

				float x, y, z = 0;
				float a = Mathf.Deg2Rad * offset;

				Vector3 v = Vector3.zero;

				for(int i = 0; i < vertexCount-1; i++) 
				{
					x = Mathf.Sin(a) * circleRadius;
					y = Mathf.Cos(a) * circleRadius;
					v = new Vector3(x, y, z);
					mainLine.SetPosition(i, v);

					a += angleDelta;
				}

				a = Mathf.Deg2Rad * (offset + length);
				x = Mathf.Sin(a) * circleRadius;
				y = Mathf.Cos(a) * circleRadius;
				v = new Vector3(x, y, z);
				mainLine.SetPosition(vertexCount - 1, v);
				
				if(length < 360)
				{
					endPoint1.SetWidth(width, width);
					endPoint2.SetWidth(width, width);

					endPoint1.SetColors(endPoint1Color, endPoint1Color);
					endPoint2.SetColors(endPoint2Color, endPoint2Color);

					a = Mathf.Deg2Rad * offset;
					x = Mathf.Sin(a) * (circleRadius - width * 2);
					y = Mathf.Cos(a) * (circleRadius - width * 2);
					v = new Vector3(x, y, z);
					endPoint1.SetPosition(0, v);

					x = Mathf.Sin(a) * (circleRadius + width * 2);
					y = Mathf.Cos(a) * (circleRadius + width * 2);
					v = new Vector3(x, y, z);
					endPoint1.SetPosition(1, v);

					a = Mathf.Deg2Rad * (offset + length);
					x = Mathf.Sin(a) * (circleRadius - width * 2);
					y = Mathf.Cos(a) * (circleRadius - width * 2);
					v = new Vector3(x, y, z);
					endPoint2.SetPosition(0, v);

					x = Mathf.Sin (a) * (circleRadius + width*2);
					y = Mathf.Cos (a) * (circleRadius + width*2);
					v = new Vector3(x, y, z);
					endPoint2.SetPosition(1, v);
				}

				// now draw the currentPosition marker

				currentPosMarker.SetColors (currentPositionColor, currentPositionColor);
				currentPosMarker.SetWidth (width*2, 0.01f);

				a = Mathf.Deg2Rad * currentPosition;
				x = Mathf.Sin(a) * (circleRadius - width * 2);
				y = Mathf.Cos(a) * (circleRadius - width * 2);
				v = new Vector3(x, y, z);
				currentPosMarker.SetPosition(0, v);

				x = Mathf.Sin(a) * circleRadius;
				y = Mathf.Cos(a) * circleRadius;
				v = new Vector3(x, y, z);
				currentPosMarker.SetPosition(1, v);

				defaultPosMarker.SetWidth(width * 2, 0.01f);
				defaultPosMarker.SetColors(endPoint1Color, endPoint1Color);

				a = Mathf.Deg2Rad * defaultPosition;
				x = Mathf.Sin(a) * (circleRadius + width * 2);
				y = Mathf.Cos(a) * (circleRadius + width * 2);
				v = new Vector3(x, y, z);
				defaultPosMarker.SetPosition(0, v);

				x = Mathf.Sin(a) * circleRadius;
				y = Mathf.Cos(a) * circleRadius;
				v = new Vector3(x, y, z);
				defaultPosMarker.SetPosition(1, v);

				for(int i = 0; i < presetsPosMarkers.Count; i++)
				{
					var posMarker = presetsPosMarkers[i];
					posMarker.useWorldSpace = false;
					var pos = presetPositions[i];

					posMarker.SetColors(presetPositionsColor, presetPositionsColor);
					posMarker.SetWidth(width * 0.5f, width * 0.5f);

					a = Mathf.Deg2Rad * pos;
					x = Mathf.Sin(a) * (circleRadius + width * 2.5f);
					y = Mathf.Cos(a) * (circleRadius + width * 2.5f);
					v = new Vector3(x, y, z);
					posMarker.SetPosition(0, v);

					x = Mathf.Sin(a) * circleRadius;
					y = Mathf.Cos(a) * circleRadius;
					v = new Vector3(x, y, z);
					posMarker.SetPosition(1, v);
				}
			}
		}
	}
}

