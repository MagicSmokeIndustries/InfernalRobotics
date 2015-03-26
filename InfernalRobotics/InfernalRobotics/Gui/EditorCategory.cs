using System;
using System.Collections.Generic;
using System.Linq;
using InfernalRobotics.Module;
using UnityEngine;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class IREditorCategory : MonoBehaviour
    {
        private static List<AvailablePart> availableParts = new List<AvailablePart>();

        void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(IRCustomFilter);

            //create list of parts that have MuMechToggle module in them
            availableParts.Clear();
            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (!avPart.partPrefab) continue;

                MuMechToggle moduleItem = avPart.partPrefab.GetComponent<MuMechToggle>();
                if (moduleItem)
                {
                    availableParts.Add(avPart);
                }
            }

        }

        private void IRCustomFilter()
        {
            const string iconFile = "R&D_node_icon_robotics";
            const string filterCategory = "Filter by Function";
            const string customCategoryName = "Robotic Parts";

            PartCategorizer.Icon icon = PartCategorizer.Instance.GetIcon(iconFile);

            //Adding our own subcategory to main filter
            PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == filterCategory);
            PartCategorizer.AddCustomSubcategoryFilter(Filter, customCategoryName, icon, p => availableParts.Contains(p));

            RUIToggleButtonTyped button = Filter.button.activeButton;

            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
