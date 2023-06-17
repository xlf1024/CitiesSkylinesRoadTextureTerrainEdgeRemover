﻿using ColossalFramework;
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
        private static UISlider rangeSlider = null;
        private static UITextField rangeNumber = null;
        public static void OnSettingsUI(UIHelperBase helper)
        {
#if DEBUG
            Debug.Log("ROTTERdam: Make settings was called");
#endif
            var edgeFilterSettingsGroup = helper.AddGroup("edge filter options");
            var edgeFilterCheckbox = edgeFilterSettingsGroup.AddCheckbox("enable edge filter (recommended)", EnableEdgeFilter.value, (isChecked) => { EnableEdgeFilter.value = isChecked; Patcher.RepatchAll(); }) as UICheckBox;
            void OnRangeChanged(float range, bool apply)
            {
                rangeSlider.value = range;
                rangeNumber.text = range.ToString();
                if (apply && range != EdgeFilterRange.value)
                {
                    EdgeFilterRange.value = range;
                    if (EnableEdgeFilter.value) SubstituteTextureManager.RegenerateCache();
                }
            }
            var rangePanel = edgeFilterCheckbox.parent.AddUIComponent<UIPanel>();
            rangePanel.autoLayout = true;
            rangePanel.autoFitChildrenHorizontally = true;
            rangePanel.autoFitChildrenVertically = true;
            rangePanel.autoLayoutDirection = LayoutDirection.Vertical;
            var rangeLabel = rangePanel.AddUIComponent<UILabel>();
            rangeLabel.text = "edge filter range (recommended: 0)";
            rangeLabel.textScale = 1.125f;
            var rangeRow = rangePanel.AddUIComponent<UIPanel>();
            rangeRow.autoLayout = true;
            rangeRow.autoFitChildrenHorizontally = true;
            rangeRow.autoFitChildrenVertically = true;
            rangeRow.autoLayoutDirection = LayoutDirection.Horizontal;
            rangeRow.autoLayoutPadding = new RectOffset(0, 8, 0, 0);
            rangeSlider = createSlider(helper, 0, 256, 1f, EdgeFilterRange, (value) => { OnRangeChanged(value, false); });
            rangeRow.AttachUIComponent(rangeSlider.gameObject);
            rangeNumber = createTextField(helper, EdgeFilterRange.value.ToString(), (_) => { }, (value) => { OnRangeChanged(Util.LenientStringToFloat(value, 0, float.PositiveInfinity, EdgeFilterRange.value), true); });
            rangeRow.AttachUIComponent(rangeNumber.gameObject);
            rangeNumber.numericalOnly = true;
            rangeNumber.allowFloats = false;
            rangeNumber.allowNegative = false;
            rangeNumber.maxLength = 4;
            rangeNumber.width /= 3;
            rangeSlider.height = rangeNumber.height;
            rangeSlider.eventMouseUp += (_, __) => OnRangeChanged(rangeSlider.value, true);
            rangeSlider.eventMouseLeave += (_, __) => OnRangeChanged(rangeSlider.value, true);
            rangeSlider.eventLeaveFocus += (_, __) => OnRangeChanged(rangeSlider.value, true);
            rangeSlider.eventLostFocus += (_, __) => OnRangeChanged(rangeSlider.value, true);
#if DEBUG
            edgeFilterSettingsGroup.AddCheckbox("enable debug drawing", EnableDebugOverlay, (isChecked) => { EnableDebugOverlay = isChecked; Patcher.RepatchAll(); });
#endif
            // todo: Range option adjustment
            var legacySettingsGroup = helper.AddGroup("legacy options");
            legacySettingsGroup.AddCheckbox("overwrite terrain appearance", EraseClipping, (isChecked) => { EraseClipping.value = isChecked; SubstituteTextureManager.RegenerateCache(); });
            var modeDropdown = legacySettingsGroup.AddDropdown("operating mode (recommended: None, use edge filter mode above)", Enum.GetNames(typeof(Modes)), Mode.value, (value) =>
            {
                var oldmode = Mode.value;
                Mode.value = value;
                if (((Modes)oldmode == Modes.None) ^ ((Modes)value == Modes.None)) Patcher.RepatchAll();
                else SubstituteTextureManager.RegenerateCache();
            }) as UIDropDown;
            var mainPanel = modeDropdown.parent.parent as UIPanel;
            void OnStrengthChanged(int strength, bool apply)
            {
                strengthSlider.value = strength;
                strengthNumber.text = strength.ToString();
                if (apply && strength != Strength.value)
                {
                    Strength.value = strength;
                    if ((Modes)Mode.value != Modes.Erase && (Modes)Mode.value != Modes.None) SubstituteTextureManager.RegenerateCache();
                }
            }
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
            //legacySettingsGroup.AddCheckbox("temporarily disable the mod (for quick comparison)", TempDisable, (isChecked) => { TempDisable = isChecked; SubstituteTextureManager.RegenerateCache(); });
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

        public static SavedBool EnableEdgeFilter { get; } = new SavedBool(nameof(EnableEdgeFilter), FileName, true, true);
        public static SavedFloat EdgeFilterRange { get; } = new SavedFloat(nameof(EdgeFilterRange), FileName, 0f, true);
        public static SavedInt Mode { get; } = new SavedInt(nameof(Mode), FileName, (int)Modes.None, true);
        public static SavedBool EraseClipping { get; } = new SavedBool(nameof(EraseClipping), FileName, false, true);
        public static SavedInt Strength { get; } = new SavedInt(nameof(Strength), FileName, 128, true);

        public static readonly int MaxStrength = 128;
        public static bool TempDisable = false;
        public static bool EnableDebugOverlay = false;
        public static void LogSettings()
        {
            Debug.Log("ROTTERdam settings:\n  Edge filter enabled: " + EnableEdgeFilter.value.ToString() + "\n  Edge filter range: " + EdgeFilterRange.value.ToString() + "\n  Legacy Mode: " + ((Modes)Mode.value).ToString() + "\n  Strength: " + Strength.value.ToString() + "\n  Overwrite terrain appearance: " + EraseClipping.value.ToString() + "\n  Temporary disable:" + TempDisable.ToString());
        }
    }
    public enum Modes
    {
        Erase,
        Clamp,
        Scale,
        None
    }
}
