using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using SynUbePatcher.Settings;
using Mutagen.Bethesda.Plugins;
using static Mutagen.Bethesda.Synthesis.SynthesisPipeline;
using System.IO.Abstractions;
using Mutagen.Bethesda.Environments;
using Synthesis.Bethesda.Commands;
using Mutagen.Bethesda.Synthesis.CLI;
using CommandLine;
using GameFinder.Common;
using System.Linq;
using Noggog;
using static Mutagen.Bethesda.Skyrim.SkyrimModHeader;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Records;
using DynamicData;
using System.Reflection;
using Mutagen.Bethesda.Plugins.Order;
using System.Security.Cryptography.X509Certificates;
using Mutagen.Bethesda.Plugins.Cache;

namespace SynUbePatcher
{
    public class UbePatcher
    {
        static PatcherSettings Settings = null!;

        string DataFolderPath;
        string OutputPath;
        ILoadOrder<IModListing<ISkyrimModGetter>> LoadOrder;
        ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache;
        SkyrimMod? PatchMod;
        TBodySlot[] IgnoredSlots = new TBodySlot[] { TBodySlot.Hair, TBodySlot.Head, TBodySlot.Ears, TBodySlot.Circlet, TBodySlot.LongHair };

        uint ArmorFilterRaceId = 25; //Nord race
        List<string> FilePathsToCopy = new List<string>();

        bool runFromSynthesis = false;

        public UbePatcher(string dataFolderPath, string outputPath, ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder, ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache)
        {
            DataFolderPath = dataFolderPath ?? throw new ArgumentNullException(nameof(dataFolderPath));
            OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            LoadOrder = loadOrder ?? throw new ArgumentNullException(nameof(loadOrder));
            LinkCache = linkCache ?? throw new ArgumentNullException(nameof(linkCache));
        }

        public UbePatcher(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env, string outputPath)
        {
            DataFolderPath = env.DataFolderPath;
            OutputPath = outputPath;
            LoadOrder = (ILoadOrder<IModListing<ISkyrimModGetter>>) env.LoadOrder;
            LinkCache = env.LinkCache;
        }

        public UbePatcher(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, PatcherSettings settings)
        {
            Settings = settings;
            DataFolderPath = state.DataFolderPath;
            OutputPath = state.OutputPath;
            LoadOrder = state.LoadOrder;
            LinkCache = state.LinkCache;
            PatchMod = (SkyrimMod)state.PatchMod;
            runFromSynthesis = true;
        }

        public void Patch()
        {
            var settings = Settings;
            if (settings == null) return;

            var outputhPluginPath = OutputPath;

            var useMO2 = !string.IsNullOrEmpty(settings.ModOrganizerModsPath);

            var mutableInputMod = SkyrimMod.CreateFromBinary($"{DataFolderPath}//UBE_AllRace.esp", SkyrimRelease.SkyrimSE);
            var ubeCache = mutableInputMod.ToImmutableLinkCache();

            var templateArmorAddon = ubeCache.Resolve<IArmorAddonGetter>("UBE_TemplateAA");
            var sTargetPluginName = "Synthesis.esp";
            var patchModKey = ModKey.FromNameAndExtension(Path.GetFileName(sTargetPluginName));
            SkyrimMod outputMod = new SkyrimMod(patchModKey, SkyrimRelease.SkyrimSE);

            if (settings.UseSinglePatchPlugin)
            {
                sTargetPluginName = settings.SinglePatchPluginName;
                if (File.Exists($"{DataFolderPath}//{sTargetPluginName}"))
                    outputMod = SkyrimMod.CreateFromBinary($"{DataFolderPath}//{sTargetPluginName}", SkyrimRelease.SkyrimSE);
                else
                {   //create new single patch plugin                    
                    outputMod = new SkyrimMod(patchModKey, SkyrimRelease.SkyrimSE);
                    outputMod.ModHeader.Flags |= HeaderFlag.LightMaster;
                }
            }

            if (settings.UseSynthesisPatchPlugin && PatchMod != null)
            {
                outputMod = PatchMod;
            }

            int patchedArmorCountTotal = 0;

            foreach (var modGetter in LoadOrder.PriorityOrder)
            {
                Print($"PROCESSING:{modGetter.ModKey.FileName}");

                if (!modGetter.ExistsOnDisk)
                {
                    Print($"Plugin file doesn't exist on disk", 1);
                    continue;
                }

                if(!settings.LoadOrderSettings.IncludedMods_Source.Contains(modGetter.ModKey))
                {
                    continue;
                }

                outputhPluginPath = OutputPath;


                #region ---------------PLUGIN----------------
                if (modGetter == null || modGetter.Mod == null) continue;

                var sCurrentPluginName = modGetter.ModKey.Name;
                sTargetPluginName = $"{sCurrentPluginName} UBE.esp";

                if (modGetter.ModKey.Type == ModType.Master)
                {
                    settings.UseSinglePatchPlugin = false;
                    Print($"Plugin is ESM. EditOriginalPlugin disabled.", 1);
                }

                if (!settings.UseSinglePatchPlugin && !settings.UseSynthesisPatchPlugin)
                {
                    if (settings.EditOriginalPlugin)
                        sCurrentPluginName = modGetter.ModKey.FileName;
                    //outputMod = SkyrimMod.CreateFromBinary($"{DataFolderPath}//{modGetter.FileName}", SkyrimRelease.SkyrimSE);

                    var outputModKey = ModKey.FromNameAndExtension(Path.GetFileName(sTargetPluginName));
                    outputMod = new SkyrimMod(outputModKey, SkyrimRelease.SkyrimSE);
                    outputMod.ModHeader.Flags |= HeaderFlag.LightMaster;
                }
                if (!settings.UseSynthesisPatchPlugin && LoadOrder.ContainsKey(outputMod.ModKey) && File.Exists($"{DataFolderPath}\\{outputMod.ModKey.FileName}"))
                {
                    Print($"Found targeted UBE patch pluging in Synthesis load order. Remove <{outputMod.ModKey.FileName}> from Synthesis patch group", 1);
                    return;
                }

                if (useMO2) outputhPluginPath = $"{settings.ModOrganizerModsPath}\\{outputMod.ModKey.Name}\\{sTargetPluginName}"; //MO2 path

                #endregion               

                int patchedArmorCount = 0;
                bool isUbePatch = false;
                foreach (var armorGetter in modGetter.Mod.Armors)
                {
                    Print($"PROCESSING:{armorGetter.EditorID}", 1);

                    if (armorGetter.FormKey.ID == 3428) continue; //naked skin armor

                    if (settings.IgnoreNonPlayable && armorGetter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)) continue; //non-playable

                    if (LoadOrder.PriorityOrder.Armor().WinningOverrides().FirstOrDefault(x => x.FormKey == armorGetter.FormKey) == null) continue; //not winning override

                    if (armorGetter.FormKey.ModKey.Name + "_UBE" == modGetter.ModKey.Name) //armor override already exists in target plugin
                    {
                        Print($"Armor override already exists in <{outputMod.ModKey.FileName}>. Removing it.", 2);
                        //state.LoadOrder.RemoveKey(modGetter.ModKey);                       
                        ////outputMod.Armors.FirstOrDefault(x => x.FormKey == armorGetter.FormKey)?.Remove(armorGetter.FormKey);
                        //FilePathsToCopy.Add(modGetter.ModKey.FileName);
                        //isUbePatch = true;
                        //break;
                    }

                    var armature = armorGetter.Armature.Select(x => x.Resolve(LinkCache)).ToArray();

                    if (armature.Any(x => x.Race.FormKey == templateArmorAddon.Race.FormKey))
                    {
                        Print($"Armor already has AA for UBE race, skipping", 2);
                        continue;
                    }

                    var armorOverride = outputMod.Armors.GetOrAddAsOverride(armorGetter);

                    foreach (var AA in armature)
                    {
                        var usableByRace = false;

                        Print($"PROCESSING:{AA.EditorID}", 2);

                        if (AA.Race.FormKey.ID == ArmorFilterRaceId)
                            usableByRace = true;
                        else
                        {
                            foreach (var addRace in AA.AdditionalRaces)
                            {
                                if (addRace.FormKey.ID == ArmorFilterRaceId)
                                {
                                    usableByRace = true;
                                    break;
                                }
                            }
                        }
                        if (!usableByRace) continue; //skip AA

                        var newAA = outputMod.ArmorAddons.DuplicateInAsNewRecord(AA);
                        var slots = GetBodySlots(newAA);

                        newAA.EditorID = $"{newAA.EditorID}_UBE";

                        var onlyIgnoredSlots = !slots.All(x => IgnoredSlots.Contains(x));
                        var isShield = armorOverride.MajorFlags.HasFlag(Armor.MajorFlag.Shield);

                        //set model paths
                        if (settings.OriginalModelPathIgnored || onlyIgnoredSlots || isShield)
                        {
                            var oldPath = newAA.WorldModel?.Female?.File.DataRelativePath;
                            var oldFullPath = $"{DataFolderPath}\\{oldPath}";

                            if (string.IsNullOrEmpty(oldPath))
                            {
                                oldPath = newAA.WorldModel?.Male?.File.DataRelativePath;
                                oldFullPath = $"{DataFolderPath}\\{oldPath}";
                            }
                            if (string.IsNullOrEmpty(oldPath))
                            {
                                Console.WriteLine($"{newAA.EditorID}: no model paths?");
                                continue;
                            }

                            var newPath = "meshes\\!UBE\\" + oldPath?.Replace("meshes", "", StringComparison.InvariantCultureIgnoreCase);
                            var newFullPath = oldFullPath.Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase);
                            if (useMO2) newFullPath = $"{settings.ModOrganizerModsPath}\\{outputMod.ModKey.Name}\\{oldPath}"
                                    .Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase); ;

                            newAA.WorldModel?.Female?.File.SetPath(newPath);

                            if (settings.CopyNifFiles && !File.Exists(newFullPath))
                            {
                                if (File.Exists(oldFullPath))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newFullPath) ?? "");
                                    File.Copy(oldFullPath, newFullPath);
                                }
                                else
                                    Print($"File doesn't exist: {oldFullPath}", 3);
                            }

                            if (!settings.MaleAsFemale)
                            {
                                oldPath = newAA.WorldModel?.Male?.File.DataRelativePath;
                                oldFullPath = $"{DataFolderPath}\\{oldPath}";
                                newPath = "meshes\\!UBE\\" + oldPath?.Replace("meshes", "", StringComparison.InvariantCultureIgnoreCase);
                                newFullPath = oldFullPath.Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase);
                                if (useMO2) newFullPath = $"{settings.ModOrganizerModsPath}\\{outputMod.ModKey.Name}\\{oldPath}"
                                        .Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase); ;

                                if (settings.CopyNifFiles && !File.Exists(newFullPath))
                                {
                                    if (File.Exists(oldFullPath))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(newFullPath) ?? "");
                                        File.Copy(oldFullPath, newFullPath);
                                    }
                                    else
                                        Print($"File doesn't exist: {oldFullPath}", 3);
                                }
                            }
                            newAA.WorldModel?.Male?.File.SetPath(newPath);
                            newAA.FirstPersonModel?.Male?.File.SetPath(newAA.FirstPersonModel?.Female?.File.DataRelativePath);
                        }

                        //set races
                        newAA.Race.SetTo(templateArmorAddon.Race);

                        newAA.AdditionalRaces.Clear();
                        newAA.AdditionalRaces.AddRange(templateArmorAddon.AdditionalRaces);

                        //slots 
                        if (slots.Contains(TBodySlot.Body))
                        {
                            newAA.BodyTemplate ??= new BodyTemplate();
                            newAA.BodyTemplate.FirstPersonFlags |= (BipedObjectFlag)((int)TBodySlot.LegMainOrRight);
                        }
                        armorOverride.Armature.Insert(0, newAA.FormKey.ToLinkGetter<IArmorAddonGetter>());
                        patchedArmorCount++;
                        patchedArmorCountTotal++;
                    }
                }

                if (isUbePatch) continue;

                if (FilePathsToCopy.Contains(outputMod.ModKey.FileName))
                    outputhPluginPath = Path.GetTempPath();

                if (!settings.UseSinglePatchPlugin && patchedArmorCount > 0)
                {
                    if (settings.EditOriginalPlugin)
                    {
                        outputMod.WriteToBinary($"{outputhPluginPath}.bak",
                        new BinaryWriteParameters()
                        {
                            LightMasterLimit = LightMasterLimitOption.ExceptionOnOverflow,
                            ModKey = ModKeyOption.NoCheck
                        });
                    }

                    outputMod.WriteToBinary($"{outputhPluginPath}",
                    new BinaryWriteParameters()
                    {
                        LightMasterLimit = LightMasterLimitOption.ExceptionOnOverflow,
                        ModKey = ModKeyOption.NoCheck
                    });
                }
            }

            if (settings.UseSinglePatchPlugin && patchedArmorCountTotal > 0)
                outputMod.WriteToBinary($"{outputhPluginPath}",
                    new BinaryWriteParameters()
                    {
                        LightMasterLimit = LightMasterLimitOption.ExceptionOnOverflow,
                        ModKey = ModKeyOption.NoCheck
                    });
        }

        static void Print(string message, int step = 0)
        {
            var tabString = "";
            for (int i = 0; i < step; i++)
                tabString += "  ";
            Console.WriteLine($"{tabString}{message}");
        }


        internal static IEnumerable<TBodySlot> ArmorSlots = HelperUtils.GetEnumValues<TBodySlot>();

        public static IEnumerable<TBodySlot> GetBodySlots(IArmorGetter armor)
        {
            BipedObjectFlag flags = armor.BodyTemplate?.FirstPersonFlags ?? 0;
            return ArmorSlots.Where(x => flags.HasFlag((BipedObjectFlag)x));
        }

        public static IEnumerable<TBodySlot> GetBodySlots(IArmorAddonGetter addon)
        {
            BipedObjectFlag flags = addon.BodyTemplate?.FirstPersonFlags ?? 0;
            return ArmorSlots.Where(x => flags.HasFlag((BipedObjectFlag)x));
        }

        public enum TBodySlot : uint
        {
            Head = 1,                   // 30
            Hair = 2,                   // 31
            Body = 4,                   // 32
            Hands = 8,                  // 33
            Forearms = 16,              // 34
            Amulet = 32,                // 35
            Ring = 64,                  // 36
            Feet = 128,                 // 37
            Calves = 256,               // 38
            Shield = 512,               // 39
            Tail = 1024,                // 40
            LongHair = 2048,            // 41
            Circlet = 4096,             // 42
            Ears = 8192,                // 43
            FaceMouth = 16384,          // 44
            Neck = 32768,               // 45
            Chest = 65536,              // 46
            Back = 131072,              // 47
            Misc1 = 262144,             // 48
            Pelvis = 524288,            // 49
            DecapitateHead = 1048576,   // 50
            Decapitate = 2097152,       // 51
            PelvisUnder = 4194304,      // 52
            LegMainOrRight = 8388608,   // 53
            LegAltOrLeft = 16777216,    // 54
            FaceAlt = 33554432,         // 55
            ChestUnder = 67108864,      // 56
            Shoulder = 134217728,       // 57
            ArmAltOrLeft = 268435456,   // 58
            ArmMainORight = 536870912,  // 59
            Misc2 = 1073741824,         // 60
            FX01 = 2147483648           // 61
        }

    }
}
