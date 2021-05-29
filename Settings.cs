using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RoadTextureTerrainEdgeRemover
{
    class Settings
    {
        public static string FileName => nameof(RoadTextureTerrainEdgeRemoverMod);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(FileName) == null)
            {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = FileName } });
            }
        }

        private static UISlider strengthSlider = null;
        private static UITextField strengthNumber = null;
        public static void OnSettingsUI(UIHelperBase helper)
        {
#if DEBUG
            Debug.Log("Make settings was called");
#endif
            helper.AddCheckbox("hide cliff texture", EraseClipping, (isChecked) => { EraseClipping.value = isChecked; SubstituteTextureManager.RegenerateCache(); });
            var modeDropdown = helper.AddDropdown("operating mode", Enum.GetNames(typeof(Modes)), Mode.value, (value) => { Mode.value = value; SubstituteTextureManager.RegenerateCache(); }) as UIDropDown;
            void OnStrengthChanged(int strength, bool apply)
            {
                strengthSlider.value = strength;
                strengthNumber.text = strength.ToString();
                if (apply && strength != Strength.value)
                {
                    Strength.value = strength;
                    if ((Modes)Mode.value != Modes.Erase) SubstituteTextureManager.RegenerateCache();
                }
            }
            var mainPanel = modeDropdown.parent.parent as UIScrollablePanel;
            var strengthPanel = mainPanel.AddUIComponent<UIPanel>();
            strengthPanel.autoLayout = true;
            strengthPanel.autoFitChildrenHorizontally = true;
            strengthPanel.autoFitChildrenVertically = true;
            strengthPanel.autoLayoutDirection = LayoutDirection.Vertical;
            var strengthLabel = strengthPanel.AddUIComponent<UILabel>();
            strengthLabel.text = "strength";
            strengthLabel.textScale = 1.125f;
            var strengthRow = strengthPanel.AddUIComponent<UIPanel>();
            strengthRow.autoLayout = true;
            strengthRow.autoFitChildrenHorizontally = true;
            strengthRow.autoFitChildrenVertically = true;
            strengthRow.autoLayoutDirection = LayoutDirection.Horizontal;
            strengthRow.autoLayoutPadding = new RectOffset(0, 8, 0, 0);
            strengthSlider = createSlider(helper, 0, MaxStrength, 1, Strength, (value) => { OnStrengthChanged(Mathf.RoundToInt(value), false); });
            strengthRow.AttachUIComponent(strengthSlider.gameObject);
            strengthNumber = createTextField(helper, Strength.value.ToString(), (_) => { }, (value) => { OnStrengthChanged(Util.LenientStringToInt(value, 0, MaxStrength, Strength.value), true); });
            strengthRow.AttachUIComponent(strengthNumber.gameObject);
            strengthNumber.numericalOnly = true;
            strengthNumber.allowFloats = false;
            strengthNumber.allowNegative = false;
            strengthNumber.maxLength = 4;
            strengthNumber.width /= 3;
            strengthSlider.height = strengthNumber.height;
            strengthSlider.eventMouseUp += (_, __) => OnStrengthChanged(Mathf.RoundToInt(strengthSlider.value), true);
            strengthSlider.eventMouseLeave += (_, __) => OnStrengthChanged(Mathf.RoundToInt(strengthSlider.value), true);
            strengthSlider.eventLeaveFocus += (_, __) => OnStrengthChanged(Mathf.RoundToInt(strengthSlider.value), true);
            strengthSlider.eventLostFocus += (_, __) => OnStrengthChanged(Mathf.RoundToInt(strengthSlider.value), true);
            helper.AddCheckbox("temporarily disable the mod (for quick comparison)", TempDisable, (isChecked) => { TempDisable = isChecked; SubstituteTextureManager.RegenerateCache(); });
        }

        public static UITextField createTextField(UIHelperBase helper, string defaultContent, OnTextChanged onTextChanged, OnTextSubmitted onTextSubmitted)
        {
            var textfield = helper.AddTextfield("..", defaultContent, onTextChanged, onTextSubmitted) as UITextField;
            var parent = textfield.parent;
            parent.RemoveUIComponent(textfield);
            var label = parent.Find<UILabel>("Label");
            parent.RemoveUIComponent(label);
            UnityEngine.Object.Destroy(label);
            parent.parent.RemoveUIComponent(parent);
            return textfield;
        }
        public static UISlider createSlider(UIHelperBase helper, float min, float max, float step, float defaultValue, OnValueChanged onValueChanged)
        {
            var slider = helper.AddSlider("..", min, max, step, defaultValue, onValueChanged) as UISlider;
            var parent = slider.parent;
            parent.RemoveUIComponent(slider);
            var label = parent.Find<UILabel>("Label");
            parent.RemoveUIComponent(label);
            UnityEngine.Object.Destroy(label);
            parent.parent.RemoveUIComponent(parent);
            return slider;

        }
        public static SavedInt Mode { get; } = new SavedInt(nameof(Mode), FileName, (int)Modes.Erase, true);
        public static SavedBool EraseClipping { get; } = new SavedBool(nameof(EraseClipping), FileName, false, true);
        public static SavedInt Strength { get; } = new SavedInt(nameof(Strength), FileName, 128, true);
        public static readonly int MaxStrength = 128;
        public static bool TempDisable = false;
        public static void LogSettings()
        {
            Debug.Log("ROTTERdam settings:\nMode: " + ((Modes)Mode.value).ToString() + "\nStrength: " + Strength.value.ToString() + "\nHide cliff texture: " + EraseClipping.value.ToString() + "\nTemporary disable:" + TempDisable.ToString());
        }
    }
    public enum Modes
    {
        Erase,
        Clamp,
        Scale
    }
}
