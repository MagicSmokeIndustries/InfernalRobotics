using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;

namespace InfernalRobotics_v3.Utility
{
	public class PartSelector : MonoBehaviour
	{
		private const string INPUT_LOCK_ID = "9A954F54BCEF4A53B98479187E5EB735";

		private const string SELECT_HELP_TEXT = "Left-Click on a part to select. Right-Click or press 'ESC' to abort.";


		private List<Part> selectableParts = new List<Part>();
		private bool isSelecting = false;


		public delegate void SelectPart(Part p);
		public SelectPart onSelectedCallback;


		public void Awake()
		{}

		public void Update()
		{
			if(isSelecting)
			{
				for(int i = 0; i < selectableParts.Count; i++)
				{
					selectableParts[i].SetHighlightColor(Color.cyan);
					selectableParts[i].SetHighlight(true, false);
					selectableParts[i].SetHighlightType(Part.HighlightType.AlwaysOn);
				}

				if(Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Escape))
					EndSelection();

				if(Input.GetKeyDown(KeyCode.Mouse0))
				{
					if((Mouse.HoveredPart != null)
					&& selectableParts.Contains(Mouse.HoveredPart))
						onSelectedCallback(Mouse.HoveredPart);

					EndSelection();
				}
			}
		}


		public void AddPart(Part p)
		{
			selectableParts.Add(p);
		}

		public void AddAllParts(Vessel v)
		{
			selectableParts.AddRange(v.parts);
		}

		public void AddAllParts(ShipConstruct s)
		{
			selectableParts.AddRange(s.parts);
		}

		public void AddAllPartsOfType<T>(Vessel v)
		{
			foreach(Part p in v.parts)
			{
				if(p.GetComponent<T>() != null)
					selectableParts.Add(p);
			}
		}

		public void AddAllPartsOfType<T>(ShipConstruct s)
		{
			foreach(Part p in s.parts)
			{
				if(p.GetComponent<T>() != null)
					selectableParts.Add(p);
			}
		}

		public void StartSelection()
		{
			InputLockManager.SetControlLock(INPUT_LOCK_ID);

			ScreenMessages.PostScreenMessage(SELECT_HELP_TEXT, 5, ScreenMessageStyle.UPPER_CENTER);

			isSelecting = true;
		}

		public void EndSelection()
		{
			InputLockManager.RemoveControlLock(INPUT_LOCK_ID);

			for(int i = 0; i < selectableParts.Count; i++)
				selectableParts[i].SetHighlightDefault();

			isSelecting = false;

			Mouse.Left.ClearMouseState();
			Mouse.Right.ClearMouseState();
		}
	}
}
