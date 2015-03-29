using System.Collections.Generic;
using InfernalRobotics.Extension;
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
            const string iconFile = "R&D_node_icon_robotics";
            const string filterCategory = "Filter by Function";
            const string customCategoryName = "Robotic Parts";

            PartCategorizer.Icon icon = PartCategorizer.Instance.GetIcon(iconFile);

            //Adding our own subcategory to main filter
            PartCategorizer.Category filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == filterCategory);
            PartCategorizer.AddCustomSubcategoryFilter(filter, customCategoryName, icon, p => availableParts.Contains(p));

            RUIToggleButtonTyped button = filter.button.activeButton;

            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
