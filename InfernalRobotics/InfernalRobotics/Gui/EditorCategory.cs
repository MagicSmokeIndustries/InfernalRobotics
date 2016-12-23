using System.Collections.Generic;
using InfernalRobotics.Control.Servo;
using UnityEngine;
using KSP.UI.Screens;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class IREditorCategory : MonoBehaviour
    {
        private static readonly List<AvailablePart> availableParts = new List<AvailablePart>();

        void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(IRCustomFilter);

            //create list of parts that have ModuleIRServo module in them
            availableParts.Clear();
            availableParts.AddRange(PartLoader.LoadedPartsList.InfernalParts());
        }

        private void IRCustomFilter()
        {
            const string FILTER_CATEGORY = "Filter by Function";
            const string CUSTOM_CATEGORY_NAME = "Robotic";

            //var texture_on = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            //var texture_off = new Texture2D(36, 36, TextureFormat.RGBA32, false);

            //InfernalRobotics.Utility.TextureLoader.LoadImageFromFile(texture_on, "icon_filter_on.png");
            //InfernalRobotics.Utility.TextureLoader.LoadImageFromFile(texture_off, "icon_filter_off.png");

            var texture_off = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_filter_off");
            var texture_on = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_filter_on");

            RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("Infernal Robotics", texture_off, texture_on);

            //Adding our own subcategory to main filter
            PartCategorizer.Category filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == FILTER_CATEGORY);
            PartCategorizer.AddCustomSubcategoryFilter(filter, CUSTOM_CATEGORY_NAME, icon, p => availableParts.Contains(p));

            //KSP.UI.UIRadioButton button = filter.button.activeButton;

            //button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            //button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}
