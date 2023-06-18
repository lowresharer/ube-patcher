using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynUbePatcher.Settings;
using static Mutagen.Bethesda.Skyrim.SkyrimModHeader;

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
            if(useMO2)
                Print($"Using ModOrganizer mods folder");

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
                Print($"[{modGetter.ModKey.FileName}]");

                if (!modGetter.ExistsOnDisk)
                {
                    Print($"Plugin file doesn't exist on disk\n", 1);
                    continue;
                }

                if(settings.LoadOrderSettings.IncludedMods_Source.Count > 0 && !settings.LoadOrderSettings.IncludedMods_Source.Contains(modGetter.ModKey))
                {
                    Print($"Mod not found in <IncludedMods_Source>\n", 1);
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
                    Print($"Plugin is ESM. <EditOriginalPlugin> disabled.", 1);
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
                    Print($"[{HelperUtils.GetSmallName(armorGetter)}]", 1);

                    if (armorGetter.FormKey.ID == 3428) continue; //vanilla naked skin armor

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
                        Print($"Armor already has AA for UBE race, skipping\n", 2);
                        continue;
                    }

                    var armorOverride = outputMod.Armors.GetOrAddAsOverride(armorGetter);

                    foreach (var AA in armature)
                    {
                        var usableByRace = false;

                        Print($"[{HelperUtils.GetSmallName(AA)}]", 2);

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
                        var slots = ArmorSlots.GetBodySlots(newAA);

                        newAA.EditorID = $"{newAA.EditorID}_UBE";

                        var onlyIgnoredSlots = slots.All(x => settings.IgnoredSlots.Contains(x));
                        var isShield = armorOverride.MajorFlags.HasFlag(Armor.MajorFlag.Shield);

                        //set model paths
                        if (
                            !settings.OriginalModelPathAll &&
                            !(settings.OriginalModelPathIgnored && onlyIgnoredSlots) ||
                            !isShield
                            )
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
                                    .Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase);

                            newAA.WorldModel?.Female?.File.SetPath(newPath);

                            if (settings.CopyNifFiles)
                            {
                                if (File.Exists(newFullPath))
                                {
                                    Print($"File already exists: {newFullPath}", 3);
                                }
                                else
                                {
                                    if (File.Exists(oldFullPath))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(newFullPath) ?? "");
                                        Print($"Copying: {oldFullPath} --> {newFullPath}", 3);
                                        File.Copy(oldFullPath, newFullPath);
                                    }
                                    else
                                        Print($"File doesn't exist: {oldFullPath}", 3);
                                }
                            }
                            if (!settings.MaleAsFemale)
                            {
                                oldPath = newAA.WorldModel?.Male?.File.DataRelativePath;
                                oldFullPath = $"{DataFolderPath}\\{oldPath}";
                                newPath = "meshes\\!UBE\\" + oldPath?.Replace("meshes", "", StringComparison.InvariantCultureIgnoreCase);
                                newFullPath = oldFullPath.Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase);
                                if (useMO2) newFullPath = $"{settings.ModOrganizerModsPath}\\{outputMod.ModKey.Name}\\{oldPath}"
                                        .Replace("meshes", "meshes\\!UBE", StringComparison.InvariantCultureIgnoreCase); ;

                                if (settings.CopyNifFiles)
                                {
                                    if (File.Exists(newFullPath))
                                    {
                                        Print($"File already exists: {newFullPath}", 3);                                       
                                    }
                                    else
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
                            }
                            else
                            {
                                newAA.WorldModel?.Male?.File.SetPath(newPath);
                                newAA.FirstPersonModel?.Male?.File.SetPath(newAA.FirstPersonModel?.Female?.File.DataRelativePath);
                            }
                        }
                        else
                        {
                            if (settings.MaleAsFemale)
                            {
                                newAA.WorldModel?.Male?.File.SetPath(newAA.WorldModel?.Female?.File.DataRelativePath);
                                newAA.FirstPersonModel?.Male?.File.SetPath(newAA.FirstPersonModel?.Female?.File.DataRelativePath);
                            }
                            Print($"Using original model paths", 3);                           
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

                if (settings.UseSynthesisPatchPlugin) continue;

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

                    Print($"Saving patch as {outputhPluginPath}");
                    outputMod.WriteToBinary($"{outputhPluginPath}",
                    new BinaryWriteParameters()
                    {
                        LightMasterLimit = LightMasterLimitOption.ExceptionOnOverflow,
                        ModKey = ModKeyOption.NoCheck
                    });
                }
                Print();
            }

            if (settings.UseSynthesisPatchPlugin) return;

            if (settings.UseSinglePatchPlugin && patchedArmorCountTotal > 0)
            {
                Print($"Saving patch as {outputhPluginPath}");
                outputMod.WriteToBinary($"{outputhPluginPath}",
                    new BinaryWriteParameters()
                    {
                        LightMasterLimit = LightMasterLimitOption.ExceptionOnOverflow,
                        ModKey = ModKeyOption.NoCheck
                    });
            }
        }

        static void Print(string message = "", int step = 0)
        {
            var tabString = "";
            for (int i = 0; i < step; i++)
                tabString += "   ";
            Console.WriteLine($"{tabString}{message}");
        }         
    }    
}
