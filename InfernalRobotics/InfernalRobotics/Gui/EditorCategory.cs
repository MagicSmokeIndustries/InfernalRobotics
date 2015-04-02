using System.Collections.Generic;
using InfernalRobotics.Control.Servo;
using UnityEngine;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class IREditorCategory : MonoBehaviour
    {
        private static readonly List<AvailablePart> availableParts = new List<AvailablePart>();

        void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(IRCustomFilter);

            //create list of parts that have MuMechToggle module in them
            availableParts.Clear();
            availableParts.AddRange(PartLoader.LoadedPartsList.InfernalParts());
        }

        private void IRCustomFilter()
        {
            const string ICON_FILE = "R&D_node_icon_robotics";
            const string FILTER_CATEGORY = "Filter by Function";
            const string CUSTOM_CATEGORY_NAME = "Robotic Parts";

            PartCategorizer.Icon icon = PartCategorizer.Instance.GetIcon(ICON_FILE);

            //Adding our own subcategory to main filter
            PartCategorizer.Category filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == FILTER_CATEGORY);
            PartCategorizer.AddCustomSubcategoryFilter(filter, CUSTOM_CATEGORY_NAME, icon, p => availableParts.Contains(p));

            RUIToggleButtonTyped button = filter.button.activeButton;

            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
