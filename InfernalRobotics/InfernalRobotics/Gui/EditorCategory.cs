using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using InfernalRobotics_v3.Servo;
using InfernalRobotics_v3.Utility;

namespace InfernalRobotics_v3.Gui
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class IREditorCategory : MonoBehaviour
	{
		private static readonly List<AvailablePart> availableParts = new List<AvailablePart>();

		void Awake()
		{
			GameEvents.onGUIEditorToolbarReady.Add(IRCustomFilter);

			// create list of parts that have ModuleIRServo module in them
			availableParts.Clear();
			availableParts.AddRange(PartLoader.LoadedPartsList.InfernalParts());

			for(int i = 0; i < availableParts.Count; i++)
				availableParts[i].category = (PartCategories)(-2); // otherwise parts cannot be found (not an 'official' way to do this)
		}

		private void IRCustomFilter()
		{
			const string FILTER_CATEGORY_BYFUNCTION = "Filter by Function";
			const string CUSTOM_CATEGORY_NAME_ROBOTIC = "Robotic";

			var texture_off = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_filter_off");
			var texture_on = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_filter_on");

			RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("Infernal Robotics", texture_off, texture_on);

			// Adding our own subcategory to main filter
			PartCategorizer.Category filterByFunction = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == FILTER_CATEGORY_BYFUNCTION);
			PartCategorizer.AddCustomSubcategoryFilter(filterByFunction, CUSTOM_CATEGORY_NAME_ROBOTIC, CUSTOM_CATEGORY_NAME_ROBOTIC, icon, p => availableParts.Contains(p));
		}
	}
}
