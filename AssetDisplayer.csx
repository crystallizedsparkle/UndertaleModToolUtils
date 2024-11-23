using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Collections.Generic;

// Ensure data is loaded
EnsureDataLoaded();

// the path that the script operates in
string datapath = $"{Path.GetDirectoryName(FilePath)}\\exported_assets\\";

// create directory if it doesnt exist
if (!Directory.Exists(datapath))
    Directory.CreateDirectory(datapath);

public class GMConstants
{
    public static Dictionary<int, string> Colors { get; } = new()
    {
        { 16776960, "c_aqua" },
        { 0, "c_black" },
        { 16711680, "c_blue" },
        { 4210752, "c_dkgray" },
        { 16711935, "c_fuchsia" },
        { 8421504, "c_gray" },
        { 32768, "c_green" },
        { 65280, "c_lime" },
        { 12632256, "c_ltgray" },
        { 128, "c_maroon" },
        { 8388608, "c_navy" },
        { 32896, "c_olive" },
        { 4235519, "c_orange" },
        { 8388736, "c_purple" },
        { 255, "c_red" },
        { 8421376, "c_teal" },
        { 16777215, "c_white" },
        { 65535, "c_yellow" }
    };

    public static Dictionary<int, string> InstanceKeywords { get; } = new()
    {
        { -1, "self" },
        { -2, "other" },
        { -3, "all" },
        { -4, "noone" }
    };

    // TODO: add more constants

}


// every important asset type including their place in Data
var assets = new List<(string filename, dynamic data)>
{
    ("audiogroups", Data.AudioGroups),
    ("sounds", Data.Sounds),
    ("sprites", Data.Sprites),
    ("tilesets", Data.Backgrounds),
    ("paths", Data.Paths),
    ("scripts", Data.Scripts),
    ("shaders", Data.Shaders),
    ("fonts", Data.Fonts),
    ("timelines", Data.Timelines),
    ("objects", Data.GameObjects),
    ("rooms", Data.Rooms),
    ("extensions", Data.Extensions)
};

var constants = new List<(string filename, dynamic data)>
{
    ("colors", GMConstants.Colors),
    ("instancekeywords", GMConstants.InstanceKeywords)
};



void DumpIDS()
{
    // loop through assets
    foreach (var asset in assets)
    {
        // create name for the text file
        string txtfile = $"{datapath}{asset.filename}.txt";

        // clear the text file.
        File.WriteAllText(txtfile, String.Empty);

        // streamwriter automatically creates and writes to the text file
        using (StreamWriter sw = new StreamWriter(txtfile, true, Encoding.Default))
        {
            // loop through each asset
            for (var i = 0; i < asset.data.Count; i++)
            {
                // append a new line to the streamwriter and therefore the text file
                sw.WriteLine($"{i} : {asset.data[i].Name.ToString().Replace("\"", "")},"); // turn the asset name into a normal string, and then remove the quotes
            }
        }
    }
}

// seperate from other ID's because theyre constants, not assets

// I hate repeating code :(
void DumpConstants()
{
    string constants_path = $"{datapath}constants\\";
    // create directory if it doesnt exist
    if (!Directory.Exists(constants_path))
        Directory.CreateDirectory(constants_path);

    // loop through constants
    foreach (var dict in constants)
    {
        // create name for the text file
        string txtfile = $"{constants_path}{dict.filename}.txt";

        // clear the text file.
        File.WriteAllText(txtfile, String.Empty);

        // streamwriter automatically creates and writes to the text file
        using (StreamWriter sw = new StreamWriter(txtfile, true, Encoding.Default))
        {
            // loop through each constant
            foreach (var kvp in dict.data)
            {
                // append a new line to the streamwriter and therefore the text file
                sw.WriteLine($"{kvp.Key} : {kvp.Value}");
            }
        }
    }
}
bool dump_ids = (ScriptQuestion("Dump ID's?"));
bool dump_constants = (ScriptQuestion("Dump Constants?"));


// run the methods
if (dump_ids)
    DumpIDS();

if (dump_constants)
    DumpConstants();

// this displays when the script is over
MessageBox.Show("Done!");
