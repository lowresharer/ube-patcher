using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using SynUbePatcher;

namespace UBEPatcherGUI
{
    public partial class Form1 : Form
    {
        List<ModKey> excludedModKeys = new List<ModKey> {
                ModKey.FromFileName("Skyrim.esm"),
                ModKey.FromFileName("Update.esm"),
                ModKey.FromFileName("Dawnguard.esm"),
                ModKey.FromFileName("Dragonborn.esm"),
                ModKey.FromFileName("Hearthfires.esm"),
                ModKey.FromFileName("UBE_AllRace.esp")  };

        string dataFolder = "M:\\Data_test\\";

        public Form1()
        {
            InitializeComponent();
            
            var rawLoadOrder = new List<ModKey>();            

            foreach (var plugin in Directory.GetFiles(dataFolder, "*.es?", SearchOption.TopDirectoryOnly))
            {
                rawLoadOrder.Add(ModKey.FromFileName(Path.GetFileName(plugin)));
            }

            checkedListBox1.CheckOnClick = true;
            ((ListBox)checkedListBox1).DataSource = rawLoadOrder;
            ((ListBox)checkedListBox1).DisplayMember = "FileName";
            //((ListBox)this.checkedListBox1).ValueMember = "IsNull";

            //var patcher = new UbePatcher(env, "");
            //patcher.Patch();

            //foreach (var file in FilePathsToCopy)
            //{
            //    File.Move($"{DataFolderPath}\\{file}", $"{DataFolderPath}\\{file}.bak");
            //    File.Copy($"{Path.GetTempPath}\\{file}", $"{DataFolderPath}\\{file}");
            //}
        }

        private void run_button_Click(object sender, EventArgs e)
        {

            var loadOrder = new List<ModKey>();

            foreach (var plugin in checkedListBox1.CheckedItems)
            {
                loadOrder.Add((ModKey)plugin);
            }

            //using var env = GameEnvironment.Typical.Builder<ISkyrimMod, ISkyrimModGetter>(GameRelease.SkyrimSE)
            //    .TransformLoadOrderListings(x => x.Where(x => !excludedModKeys.Contains(x.ModKey)))
            //.WithTargetDataFolder(dataFolder)
            //    .WithLoadOrder(loadOrder.ToArray())
            //    //.WithOutputMod(outgoing)
            //    .Build();
        }
    }
}