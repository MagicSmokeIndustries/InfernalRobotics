using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace InfernalRobotics_v3.Module
{
	class MyEndEffector : PartModule
	{
		// soll dann einen Pointer nach unten zeichnen können und mit dem steuern wir dann alles...
		// nicht mehr das blöde Verfahren, dass alle Teile auf's gleiche zeigen

		private LineDrawer[] al = new LineDrawer[13];
		private Color[] alColor = new Color[13];

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(Vector3.zero, p_vector, alColor[idx]);
		}

		private void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(p_from, p_vector, alColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			al[idx].DrawLineInGameView(p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformVector(p_vector) : p_vector), alColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }

		// FEHLER, spezielle und evtl. temporäre Hilfsfunktionen
		
		public MyEndEffector()
		{
			for(int i = 0; i < 13; i++)
				al[i] = new LineDrawer();

			alColor[0] = Color.red;
			alColor[1] = Color.green;
			alColor[2] = Color.yellow;
			alColor[3] = Color.magenta;	// axis
			alColor[4] = Color.blue;		// secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			alColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			alColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			alColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
			alColor[11] = new Color(209.0f / 255.0f, 247.0f / 255.0f, 74.0f / 255.0f);
			alColor[12] = new Color(247.0f / 255.0f, 186.0f / 255.0f, 74.0f / 255.0f);
		}

		public Vector3 GetPointer()
		{
			return transform.up.normalized;
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "draw Pointer")]
		public void drawPointer()
		{
			DrawAxis(0, transform, GetPointer(), false, transform.right.normalized * 0.4f);
		}
	}
}
