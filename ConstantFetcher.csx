using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
// bring back for easy debugging
//using System.Windows.Forms;

// make sure a data.win is loaded.
EnsureDataLoaded();
// create list with every asset
var assets = new List<(dynamic data, string name)>
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
// beep beep beep :)
Console.Beep(200, 100);
Console.Beep(400, 100);
Console.Beep(600, 100);
// this is here to start the loop
public bool running = true;
RunScript();

void RunScript()
{
    // obtain input from user
    string input = String.Empty;
    while (input == String.Empty)
    {
        input = SimpleTextInput("ConstantFetcher.csx", "Enter IDs! (seperated by ',')", String.Empty, true, true);
    }

    // this means user closed out of the window
    if (input is null)
    {
        running = false;
        return;
    }
    
    // yummy pie
    if (input.Contains("3.141592653589793"))
        ScriptMessage("ITS PI!!!");

    // regex to get rid of everything except numbers and commas
    input = Regex.Replace(input, @"[^\d,.-]", String.Empty);

    // split string at commas.
    string[] lines = input.Split(new[] { ',' }, StringSplitOptions.None);


    // create a reverse dictionary whatever the hell that is
    var reverseConstants = Data.BuiltinList.Constants
        .GroupBy(kvp => kvp.Value)
        .ToDictionary(group => group.Key, group => group.Select(kvp => kvp.Key).ToList());

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
        int code = int.Parse(line);
        
        // gets the name of the key from the UTMT key enum.
        string keyname = Enum.GetName(typeof(EventSubtypeKey), code);

        // Append a little seperator for readability
        sb.AppendLine($"----------------------------------\n\nValues for {code}:");

        // use the reverse dictionary to get all of the connected values to the code.
        reverseConstants.TryGetValue(code, out List<string> names);

        // if if the TryGetValue returns false this isnt created, so create it.
        names ??= new List<string>();

        for (int entry = 0; entry < names.Count; entry++)
        {
            names[entry] = $"Constant : {names[entry]}";
        }

        // make sure it isnt null (if its too large it can be) and in bounds of the arguments for the ord function
        if (keyname != null && (code >= 48 && code <= 90))
        {
            names.Insert(0, $"Constant : ord(\"{keyname.ToCharArray()[0]}\")"); // insert the character into the list
        }

        // loop through assets
        foreach (var asset in assets)
        {
            // make sure were not going under or over the array
            if (code < asset.data.Count && code >= 0)
            {
                names.Insert(0, $"{asset.name} : {asset.data[code].Name.ToString().Replace("\"", "")}"); // insert the asset into the list
            }
        }

        // fun string.join stuff that adds a comma and a space after each corresponding matching value
        sb.AppendLine(string.Join(",\n", names));
        // append an empty line
        sb.AppendLine(String.Empty);
        if (names.Count == 0)
            sb.AppendLine("No Values."); // if theres nothing

    }

    // a little output box showing all the values
    SimpleTextInput("ConstantFetcher.csx", String.Empty, sb.ToString(), true);
    running = false;

}

// really dumb and sloppy logic to let the script loop
while (!running)
{
    bool runagain = ScriptQuestion("Run script again?");
    if (runagain)
    {
        running = true;
        RunScript();
    }
    else
        break;
}
    
