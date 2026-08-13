using System;
using BepInEx.Configuration;

namespace TaskSearch
{
    internal static class TaskSearchConfig
    {
        private const string SectionGeneral = "1. General";
        private const string SectionFields = "2. Searched Fields";
        private const string SectionLayout = "3. Search Field Layout";

        internal static ConfigEntry<bool> Enabled { get; private set; }

        internal static ConfigEntry<bool> SearchQuestNames { get; private set; }
        internal static ConfigEntry<bool> SearchDescriptions { get; private set; }
        internal static ConfigEntry<bool> SearchObjectives { get; private set; }
        internal static ConfigEntry<bool> SearchLocations { get; private set; }
        internal static ConfigEntry<bool> SearchTraders { get; private set; }
        internal static ConfigEntry<bool> SearchItems { get; private set; }
        internal static ConfigEntry<bool> SearchRequirements { get; private set; }
        internal static ConfigEntry<bool> SearchQuestIds { get; private set; }

        internal static ConfigEntry<float> FieldWidth { get; private set; }
        internal static ConfigEntry<float> FieldHeight { get; private set; }
        internal static ConfigEntry<float> FieldOffsetX { get; private set; }
        internal static ConfigEntry<float> FieldOffsetY { get; private set; }

        internal static event Action LayoutChanged;

        internal static event Action SearchedFieldsChanged;

        internal static void Initialize(ConfigFile config)
        {
            Enabled = config.Bind(
                SectionGeneral, "Enabled", true,
                "Master switch. When off, the Tasks screen is left completely untouched.");

            SearchQuestNames = config.Bind(
                SectionFields, "Quest Names", true,
                "Search the task's name.");

            SearchDescriptions = config.Bind(
                SectionFields, "Descriptions", true,
                "Search the task's briefing text.");

            SearchObjectives = config.Bind(
                SectionFields, "Objectives", true,
                "Search objective text and objective types.");

            SearchLocations = config.Bind(
                SectionFields, "Locations", true,
                "Search the task's map, plus any map or zone named by an objective.");

            SearchTraders = config.Bind(
                SectionFields, "Traders", true,
                "Search the name of the trader who gives the task.");

            SearchItems = config.Bind(
                SectionFields, "Items", true,
                "Search names of items an objective refers to.");

            SearchRequirements = config.Bind(
                SectionFields, "Requirements", true,
                "Search names of prerequisite tasks.");

            SearchQuestIds = config.Bind(
                SectionFields, "Quest IDs", true,
                "Search the raw quest id. Useful for mod authors, harmless otherwise.");

            FieldWidth = config.Bind(
                SectionLayout, "Width", 340f,
                new ConfigDescription(
                    "Width in canvas units.",
                    new AcceptableValueRange<float>(80f, 1200f)));

            FieldHeight = config.Bind(
                SectionLayout, "Height", 28f,
                new ConfigDescription(
                    "Height in canvas units.",
                    new AcceptableValueRange<float>(14f, 80f)));

            FieldOffsetX = config.Bind(
                SectionLayout, "Horizontal Offset", 0f,
                new ConfigDescription(
                    "Nudge left (negative) or right (positive) from the task list's right edge.",
                    new AcceptableValueRange<float>(-800f, 800f)));

            FieldOffsetY = config.Bind(
                SectionLayout, "Vertical Offset", 100f,
                new ConfigDescription(
                    "Lower Values goes down, Higher Values goes up, duhhhh.",
                    new AcceptableValueRange<float>(-300f, 300f)));

            config.SettingChanged += OnSettingChanged;
        }

        private static void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            ConfigEntryBase changed = args == null ? null : args.ChangedSetting;

            if (changed == null)
            {
                return;
            }

            if (changed.Definition.Section == SectionLayout)
            {
                LayoutChanged?.Invoke();
                return;
            }

            if (changed.Definition.Section == SectionFields)
            {
                SearchedFieldsChanged?.Invoke();
            }
        }
    }
}
