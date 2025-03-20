using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
// make sure a data.win is loaded.
EnsureDataLoaded();

// for macro stuff!
public string definitionDir = $"{AppDomain.CurrentDomain.BaseDirectory}GameSpecificData\\Definitions\\";
public string macroDir = $"{AppDomain.CurrentDomain.BaseDirectory}GameSpecificData\\Underanalyzer\\";

public class MacroData
{
    public MacroTypes Types { get; set; } = new();
    public class MacroTypes
    {
        public Dictionary<string, EnumData> Enums { get; set; } = new();
    }
}

public class EnumData
{
    public EnumData(string name, Dictionary<string, long>? values)
    {
        this.Name = name;
        if (values is not null)
            this.Values = values; 
    }
    public Dictionary<string, long> Values { get; set; } = new();
    public string Name { get; set; }
}

// beep beep beep :)
Console.Beep(200, 100);
Console.Beep(400, 100);
Console.Beep(600, 100);

// create a reverse dictionary whatever the hell that is
public var assets = new List<(dynamic data, string name)>
{
    (Data.TextureGroupInfo, "TextureGroup" ),
    (Data.Extensions, "Extension"),
    (Data.AnimationCurves, "AnimationCurve"),
    (Data.Sequences, "Sequence"),
    (Data.Timelines, "Timeline"),
    (Data.Fonts, "Font"),
    (Data.Paths, "Path"),
    (Data.Backgrounds, "Background"),
    (Data.AudioGroups, "AudioGroup"),
    (Data.Shaders, "Shader"),
    (Data.Scripts, "Script"),
    (Data.Rooms, "Room"),
    (Data.Sounds, "Sound"),
    (Data.Sprites, "Sprite"),
    (Data.GameObjects, "Object")
};


Form form = new()
{
    Text = "ConstantFetcher",
    AutoSize = true,
    MinimizeBox = false,
    MaximizeBox = false,
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog
};

TextBox inputBox = new()
{
    AcceptsReturn = true,
    AcceptsTab = true,
    Multiline = true,
    ScrollBars = ScrollBars.Vertical,
    Text = "",
    Size = new System.Drawing.Size(200, 400),
    Dock = DockStyle.Left
};

TextBox outputBox = new()
{
    AcceptsReturn = true,
    AcceptsTab = true,
    Multiline = true,
    ScrollBars = ScrollBars.Vertical,
    Text = "",
    Size = new System.Drawing.Size(400, 400),
    Dock = DockStyle.Right,
    ReadOnly = true
};
Button confirmButton = new()
{
    Text = "Confirm",
    Dock = DockStyle.Top,
};

confirmButton.Click += ConfirmButton_Click;

public void ConfirmButton_Click(object sender, EventArgs e)
{
    outputBox.Text = String.Empty;
    outputBox.Text = GetConstants(inputBox.Text);
}

form.Controls.Add(inputBox);
form.Controls.Add(outputBox);
form.Controls.Add(confirmButton);

public string GetConstants(string input)
{
    if (input is null)
        return "Input is null.";
    
    // regex to get rid of everything except numbers and commas
    input = Regex.Replace(input, @"[^\d,.-]", String.Empty);

    // split string at commas.
    List<string> lines = input.Split(new[] { ',' }, StringSplitOptions.None).ToList();

    // removes duplicate entries to clean up output
    lines = lines.Distinct().ToList();

    // create stringbuilder for the output text
    StringBuilder sb = new StringBuilder();

    // loop through each line
    foreach (string line in lines)
    {
        bool is_number = int.TryParse(line, out int result);
        // failsafe
        if (line == String.Empty || !is_number)
            continue;

        // turn the line into an integer so it can be processed into the dictionary
        if (int.TryParse(line, out int id))
        {
            char ASCIIChar = (char)id;


            // Append a little seperator for readability
            sb.AppendLine($"----------------------------------\r\n\r\nValues for {id}:");

            // reverse dictionary
            Dictionary<double, List<string>> reverseConstants = Data.BuiltinList.Constants
                .GroupBy(kvp => kvp.Value)
                .ToDictionary(group => group.Key, group => group.Select(kvp => kvp.Key).ToList());

            // use the reverse dictionary to get all of the connected values to the id.
            reverseConstants.TryGetValue(id, out List<string> names);

            // if if the TryGetValue returns false this isnt created, so create it.
            names ??= new List<string>();

            for (int entry = 0; entry < names.Count; entry++)
            {
                names[entry] = $"Constant : {names[entry]}";
            }

            // make sure it isnt null (if its too large it can be) and in bounds of the arguments for the ord function
            if (id >= 48 && id <= byte.MaxValue)
            {
                names.Insert(0, $"Constant : ord(\"{ASCIIChar}\")"); // insert the character into the list
            }

            // loop through assets
            foreach (var asset in assets)
            {
                // make sure were not going under or over the array
                if (id < asset.data.Count && id >= 0)
                {
                    names.Insert(0, $"{asset.name} : {asset.data[id].Name.ToString().Replace("\"", "")}"); // insert the asset into the list
                }
            }

            // comment this block out if you arent using underanalyzer
            // /*
            // im gonna manually rip the enums from it because im lazy and I dont want to comprehend how UTMT gets it all.
            string[] defs = Directory.GetFiles(definitionDir);

            foreach (string def in defs)
            {
                GameSpecificResolver.GameSpecificDefinition currentDef = JsonSerializer.Deserialize<GameSpecificResolver.GameSpecificDefinition>(File.ReadAllText(def));

                foreach (GameSpecificResolver.GameSpecificCondition condition in currentDef.Conditions)
                {
                    if ((condition.ConditionKind == "DisplayName.Regex" && Regex.IsMatch(Data.GeneralInfo.DisplayName.Content, condition.Value)) || condition.ConditionKind == "Always")
                    {
                        string macroPath = $"{macroDir}{currentDef.UnderanalyzerFilename}";
                        if (File.Exists(macroPath))
                        {
                            MacroData macro = JsonSerializer.Deserialize<MacroData>(File.ReadAllText(macroPath));

                            foreach (KeyValuePair<string, EnumData> kvp in macro.Types.Enums)
                            {
                                var myKey = kvp.Value.Values.FirstOrDefault(x => x.Value == id).Key;
                                // add the enum
                                if (myKey != String.Empty && myKey is not null)
                                    names.Add($"Enum: {kvp.Value.Name}.{myKey}");
                            }
                        }
                    }

                }
            }
            // */
            // fun string.join stuff that adds a comma and a space after each corresponding matching value
            sb.AppendLine(string.Join("\r\n", names));
            // append an empty line
            sb.AppendLine(String.Empty);

            if (names.Count == 0)
                sb.AppendLine("No Values."); // if theres nothing

        }

    }

    return sb.ToString();
}

form.ShowDialog();
