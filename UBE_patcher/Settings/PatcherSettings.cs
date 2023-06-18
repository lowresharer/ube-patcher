using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SynUbePatcher.Settings
{   
        public class PatcherSettings
    {
        [SynthesisOrder]
        [SynthesisSettingName("ModOrganizer Mods Folder Path")]
        [SynthesisTooltip("(Required) Slot settings.")]
        public string ModOrganizerModsPath { get; set; } = "M:\\Data_test\\mods";

        [SynthesisOrder]
        [SynthesisTooltip("Edit original plugin isntead of creating patch")]
        public bool EditOriginalPlugin { get; set; } = false;

        [SynthesisOrder]       
        public bool UseSynthesisPatchPlugin { get; set; } = false;

        [SynthesisOrder]
        [SynthesisTooltip("Direct all patched records to single plugin")]
        public bool UseSinglePatchPlugin { get; set; } = false; 
     
        [SynthesisOrder]
        [SynthesisTooltip(" Name for the single patch plugin")]
        public string SinglePatchPluginName { get; set; } = "UBE_ArmorPatch.esp"; 

        [SynthesisOrder]
        [SynthesisTooltip("If there are no files in \"!UBE/pathToArmorNif\" script will copy AA models to that path")]
        public bool CopyNifFiles { get; set; } = true; 
       
        [SynthesisOrder]
        [SynthesisTooltip("Set male model path to be the same as female one. If there is no data for female model use male one instead")]
        public bool MaleAsFemale { get; set; } = true;  

        [SynthesisOrder]
        [SynthesisSettingName("Original Model Path")]
        [SynthesisTooltip("All patched armor will use original armor models")]
        public bool OriginalModelPathAll { get; set; } = false; 

        [SynthesisOrder]
        [SynthesisTooltip("Armor addons that have slots from aIgnoredSlots will use original armor models")]
        public bool OriginalModelPathIgnored { get; set; } = true; 

        [SynthesisOrder]
        [SynthesisTooltip("Don't patch non playable armors")]
        public bool IgnoreNonPlayable { get; set; } = true; 

        [SynthesisOrder]
        [SynthesisTooltip("Update data in already existing UBE ARMAs")]
        public bool UpdateExistingArmorAddons { get; set; } = false; 

        [SynthesisOrder]
        public LoadOrderSettings LoadOrderSettings { get; set; } = new();

        List<int> IgnoredSlots { get; set; } = new List<int>();  
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

    public class ArmorSettings
    {
        [SynthesisOrder]
        [SynthesisSettingName("Enable Armor Patching")]
        [SynthesisTooltip("Enable patching armor records.")]
        public bool EnableModule = true;

        [SynthesisOrder]
        [SynthesisSettingName("Excluded Armors")]
        [SynthesisTooltip("List of armor records to be excluded.")]
        public HashSet<IFormLinkGetter<IArmorGetter>> ExcludedArmors { get; set; } = new();

    }

    public class ArmorAddonSettings
    {
        [SynthesisOrder]
        [SynthesisSettingName("Enable Armor Addons Patching")]
        [SynthesisTooltip("Enable patching armor addon records.")]
        public bool EnableModule = true;

        [SynthesisOrder]
        [SynthesisSettingName("Excluded Armor Addons")]
        [SynthesisTooltip("List of armor addon records to be excluded.")]
        public HashSet<IFormLinkGetter<IArmorAddonGetter>> ExcludedArmorAddons { get; set; } = new();

    }
}
