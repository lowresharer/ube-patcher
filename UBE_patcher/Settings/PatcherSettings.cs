using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;

namespace SynUbePatcher.Settings
{
    public class PatcherSettings
    {       
        [SynthesisOrder]
        [SynthesisSettingName("Use ModOrganizer2 Mods Folder")]
        [SynthesisTooltip("Copy plugin and nif files to new MO2 mods folder")]
        public bool UseModOrganizerModsPath { get; set; } = false;

        [SynthesisOrder]
        [SynthesisSettingName("ModOrganizer2 Mods Folder Path")]
        public string ModOrganizerModsPath { get; set; } = "<CHANGE TO YOUR MO2 MODS FOLDER PATH>";


        [SynthesisOrder]
        [SynthesisTooltip("Direct all patched records to plugin created by Synthesis")]
        public bool UseSynthesisPatchPlugin { get; set; } = true;

        [SynthesisOrder]
        [SynthesisTooltip("Edit original plugin isntead of creating patch")]
        public bool EditOriginalPlugin { get; set; } = false;

        [SynthesisOrder]
        [SynthesisTooltip("Direct all patched records to single plugin")]
        public bool UseSinglePatchPlugin { get; set; } = false;

        [SynthesisOrder]
        [SynthesisTooltip("Name for the single patch plugin")]
        public string SinglePatchPluginName { get; set; } = "UBE_ArmorPatch.esp";

        [SynthesisOrder]
        [SynthesisTooltip("If there are no files in \"!UBE\\pathToArmorNif\" script will copy AA models to that path")]
        public bool CopyNifFiles { get; set; } = true;

        [SynthesisOrder]
        [SynthesisTooltip("Set male model path to be the same as female one. If there is no data for female model use male one instead")]
        public bool MaleAsFemale { get; set; } = true;

        [SynthesisOrder]
        [SynthesisSettingName("Original Model Path")]
        [SynthesisTooltip("All patched armor will use original armor models")]
        public bool OriginalModelPathAll { get; set; } = false;

        [SynthesisOrder]
        [SynthesisSettingName("Original Model for Ignored Slots")]
        [SynthesisTooltip("Armor Addons that have ONLY slots listed in <Ignored Slots> will use original armor models")]
        public bool OriginalModelPathIgnored { get; set; } = true;

        [SynthesisOrder]
        public List<TBodySlot> IgnoredSlots { get; set; } = new() { TBodySlot.Hair, TBodySlot.Head, TBodySlot.Ears, TBodySlot.Circlet, TBodySlot.LongHair };

        [SynthesisOrder]
        [SynthesisTooltip("Don't patch non playable armors")]
        public bool IgnoreNonPlayable { get; set; } = true;

        [SynthesisOrder]
        [SynthesisTooltip("Update data in already existing UBE ARMAs")]
        public bool UpdateExistingArmorAddons { get; set; } = false;

        [SynthesisOrder]
        public LoadOrderSettings LoadOrderSettings { get; set; } = new();
    }

    public class LoadOrderSettings
    {
        [SynthesisOrder]
        [SynthesisSettingName("Included Mods (Source)")]
        [SynthesisTooltip("Only patch items originated from those mods. Leave it blank to ignore.")]
        public HashSet<ModKey> IncludedMods_Source { get; set; } = new();

        [SynthesisOrder]
        [SynthesisSettingName("Excluded Mods (Source)")]
        [SynthesisTooltip("All items originated from those mods will be excluded.")]
        public HashSet<ModKey> ExcludedMods_Source { get; set; } = new();

        [SynthesisOrder]
        [SynthesisSettingName("Included Mods (Override)")]
        [SynthesisTooltip("Only patch items overridden by those mods. Leave it blank to ignore.")]
        public HashSet<ModKey> IncludedMods_Override { get; set; } = new();

        [SynthesisOrder]
        [SynthesisSettingName("Excluded Mods (Override)")]
        [SynthesisTooltip("All items overridden by those mods will be excluded.")]
        public HashSet<ModKey> ExcludedMods_Override { get; set; } = new();
    }
}
