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

			mainLine.positionCount = vertexCount;
			mainLine.useWorldSpace = false;
			mainLine.startColor = lineColor; mainLine.endColor = lineColor;

			endPoint1.positionCount = 2;
			endPoint1.useWorldSpace = false;
			endPoint1.startColor = lineColor; endPoint1.endColor = lineColor;

			endPoint2.positionCount = 2;
			endPoint2.useWorldSpace = false;
			endPoint2.startColor = lineColor; endPoint2.endColor = lineColor;

			currentPosMarker.positionCount = 2;
			currentPosMarker.useWorldSpace = false;
			currentPosMarker.startColor = lineColor; currentPosMarker.endColor = lineColor;

			defaultPosMarker.positionCount = 2;
			defaultPosMarker.useWorldSpace = false;
			defaultPosMarker.startColor = lineColor; defaultPosMarker.endColor = lineColor;

			for(int i = 0; i < presetsPosMarkers.Count; i++)
			{
				var posRenderer = presetsPosMarkers[i];
				posRenderer.useWorldSpace = false;
				posRenderer.positionCount = 2;
				posRenderer.startColor = presetPositionsColor; posRenderer.endColor = presetPositionsColor;
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
					endPoint1.startWidth = width; endPoint1.endWidth = width;
					endPoint2.startWidth = width; endPoint2.endWidth = width;

					endPoint1.startColor = endPoint1Color; endPoint1.endColor = endPoint1Color;
					endPoint2.startColor = endPoint2Color; endPoint2.endColor = endPoint2Color;

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

				currentPosMarker.startWidth = width * 2; currentPosMarker.endWidth = 0.01f;
				currentPosMarker.startColor = currentPositionColor; currentPosMarker.endColor = currentPositionColor;

				a = Mathf.Deg2Rad * currentPosition;
				x = Mathf.Sin(a) * (circleRadius - width * 2);
				y = Mathf.Cos(a) * (circleRadius - width * 2);
				v = new Vector3(x, y, z);
				currentPosMarker.SetPosition(0, v);

				x = Mathf.Sin(a) * circleRadius;
				y = Mathf.Cos(a) * circleRadius;
				v = new Vector3(x, y, z);
				currentPosMarker.SetPosition(1, v);

				defaultPosMarker.startWidth = width * 2; defaultPosMarker.endWidth = 0.01f;
				defaultPosMarker.startColor = endPoint1Color; defaultPosMarker.endColor = endPoint1Color;

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

					posMarker.startWidth = width * 0.5f; posMarker.endWidth = width * 0.5f;
					posMarker.startColor = presetPositionsColor; posMarker.endColor = presetPositionsColor;

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

