using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib.Util;
using UndertaleModLib.Models;

// make decompiler settings, really dumb method but this is the only way to do it afaik.

GlobalDecompileContext globalDecompileContext = new(Data);

IDecompileSettings userSettings = Data.ToolInfo.DecompilerSettings;

DecompileSettings decompilerSettings = new DecompileSettings
{
    IndentString = userSettings.IndentString,
    UseSemicolon = userSettings.UseSemicolon,
    UseCSSColors = userSettings.UseCSSColors,
    PrintWarnings = userSettings.PrintWarnings,
    MacroDeclarationsAtTop = userSettings.MacroDeclarationsAtTop,
    EmptyLineAfterBlockLocals = userSettings.EmptyLineAfterBlockLocals,
    EmptyLineAroundEnums = userSettings.EmptyLineAroundEnums,
    EmptyLineAroundBranchStatements = userSettings.EmptyLineAroundBranchStatements,
    EmptyLineBeforeSwitchCases = userSettings.EmptyLineBeforeSwitchCases,
    EmptyLineAfterSwitchCases = userSettings.EmptyLineAfterSwitchCases,
    EmptyLineAroundFunctionDeclarations = userSettings.EmptyLineAroundFunctionDeclarations,
    EmptyLineAroundStaticInitialization = userSettings.EmptyLineAroundStaticInitialization,
    OpenBlockBraceOnSameLine = userSettings.OpenBlockBraceOnSameLine,
    RemoveSingleLineBlockBraces = userSettings.RemoveSingleLineBlockBraces,
    CleanupTry = userSettings.CleanupTry,
    CleanupElseToContinue = userSettings.CleanupElseToContinue,
    CleanupDefaultArgumentValues = userSettings.CleanupDefaultArgumentValues,
    CleanupBuiltinArrayVariables = userSettings.CleanupBuiltinArrayVariables,
    CreateEnumDeclarations = false,
    UnknownEnumName = userSettings.UnknownEnumName,
    UnknownEnumValuePattern = userSettings.UnknownEnumValuePattern,
    UnknownArgumentNamePattern = "arg{0}",
    AllowLeftoverDataOnStack = userSettings.AllowLeftoverDataOnStack
};

// make sure we DONT declare enums
decompilerSettings.CreateEnumDeclarations = false;

decompilerSettings.UnknownArgumentNamePattern = "arg{0}";

#region classes
public class GMAssetBase
{
    public string resourceType { get; set; }
    public string resourceVersion { get; set; }
    public string name { get; set; }
}
public class AssetIDReference
{
    public string name { get; set; }
    public string path { get; set; }

    private AssetIDReference(string name, string path)
    {
        this.name = name;
        this.path = path;
    }

    // Static factory method for object creation
    public static AssetIDReference? Create(dynamic? asset = null, string folder_name = "")
    {
        if (asset is not null)
        {
            string name = asset.Name.Content;
            string path = $"{folder_name}/{name}/{name}.yy";
            return new AssetIDReference(name, path);
        }
        else
        {
            return null;
        }
    }
}

public class GMREffectProperty
{
    public int type { get; set; } = 0;
    public string name { get; set; }
    public string value { get; set; }
}
public class GMRLayerBase : GMAssetBase
{
    public GMRLayerBase()
    {
        resourceType = "GMRLayer";
        resourceVersion = "1.0";
    }
    public bool visible { get; set; } = true;
    public float depth { get; set; } = 0;
    public bool userdefinedDepth { get; set; } = false;
    public bool inheritLayerDepth { get; set; } = false;
    public bool inheritLayerSettings { get; set; } = false;
    //public bool inheritVisibility { get; set; } = true;
    //public bool inheritSubLayers { get; set; } = false;
    public double gridX { get; set; } = 32;
    public double gridY { get; set; } = 32;
    public List<GMRLayerBase> layers { get; set; } = new();
    public bool hierarchyFrozen { get; set; } = false;
    public bool effectEnabled { get; set; } = true;
    public string? effectType { get; set; } = null;
    public List<GMREffectProperty> properties { get; set; } = new();
}
public class GMRBackgroundLayer : GMRLayerBase
{
    public GMRBackgroundLayer()
    {
        resourceType = "GMRBackgroundLayer";
    }
    public AssetIDReference? spriteId { get; set; } = null;
    public uint colour { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public bool htiled { get; set; }
    public bool vtiled { get; set; }
    public float hspeed { get; set; }
    public float vspeed { get; set; }
    public bool stretch { get; set; }
    public float animationFPS { get; set; }
    public int animationSpeedType { get; set; }
    public bool userdefinedAnimFPS { get; set; }
}
public class GMRInstance : GMAssetBase
{
    public GMRInstance()
    {
        resourceType = "GMRInstance";
        resourceVersion = "1.0";
    }
    // i dont know what this is, please tell me or make a PR
    public List<object> properties { get; set; } = new();
    public bool isDnd { get; set; }
    public AssetIDReference objectId { get; set; }
    public bool inheritCode { get; set; }
    public bool hasCreationCode { get; set; }
    public uint colour { get; set; }
    public float rotation { get; set; }
    public float scaleX { get; set; }
    public float scaleY { get; set; }
    public float imageSpeed { get; set; }
    public int imageIndex { get; set; }
    public AssetIDReference? inheritedItemId { get; set; } = null;
    public bool frozen { get; set; }
    public bool ignore { get; set; }
    public bool inheritItemSettings { get; set; }
    public float x { get; set; }
    public float y { get; set; }
}
public class GMRInstanceLayer : GMRLayerBase
{
    public GMRInstanceLayer()
    {
        resourceType = "GMRInstanceLayer";
        resourceVersion = "1.0";
    }
    public List<GMRInstance> instances { get; set; } = new();
}
public class GMRTileData
{
    //public int TileDataFormat { get; set; } = 1; // unknown
    public int SerialiseWidth { get; set; }
    public int SerialiseHeight { get; set; }
    // uint because thats what it is in gamemaker.
    public List<uint> TileSerialiseData { get; set; } = new();
}
public class GMRGraphic : GMRAsset
{
    public GMRGraphic()
    {
        resourceType = "GMRGraphic";
        resourceVersion = "1.0";
    }
    public string name { get; set; } = String.Empty;
    public AssetIDReference? spriteId { get; set; } = null;
    public int x { get; set; }
    public int y { get; set; }
    public float w { get; set; }
    public float h { get; set; }
    public float u0 { get; set; }
    public float v0 { get; set; }
    public float u1 { get; set; }
    public float v1 { get; set; }
    public uint colour { get; set; } = 0xffffffff;
    public List<string> tags { get; set; } = new();
}
public class GMRTileLayer : GMRLayerBase
{
    public GMRTileLayer()
    {
        resourceType = "GMRTileLayer";
        resourceVersion = "1.1";
    }
    public AssetIDReference? tilesetId { get; set; }
    // offset
    public float x { get; set; }
    public float y { get; set; }
    public GMRTileData tiles { get; set; }
}
public class GMRPathLayer : GMRLayerBase
{
    public GMRPathLayer()
    {
        resourceType = "GMRPathLayer";
        resourceVersion = "1.0";
    }
    public AssetIDReference? pathId { get; set; } = null;
    public uint colour { get; set; }
}
public class GMRAsset : GMAssetBase
{
    public GMRAsset()
    {
        resourceType = "GMRSpriteGraphic";
        resourceVersion = "1.0";
    }
    public float x { get; set; }
    public float y { get; set; }
    public AssetIDReference spriteId { get; set; }
    public float headPosition { get; set; }
    public float rotation { get; set; }
    public float scaleX { get; set; }
    public float scaleY { get; set; }
    public float animationSpeed { get; set; }
    public uint colour { get; set; }
    public AssetIDReference? inheritedItemId { get; set; } = null;
    public bool frozen { get; set; }
    public bool ignore { get; set; }
    public bool inheritItemSettings { get; set; }
}
public class GMREffectLayer : GMRLayerBase
{
    public GMREffectLayer()
    {
        resourceType = "GMREffectLayer";
        resourceVersion = "1.0";
    }
    // its basically the layer base
}
public class GMRAssetLayer : GMRLayerBase
{
    public GMRAssetLayer()
    {
        resourceType = "GMRAssetLayer";
        resourceVersion = "1.0";
    }
    
    public List<GMRAsset> assets { get; set; } = new();
}
public class GMRView
{
    public bool inherit { get; set; } = false;
    public bool visible { get; set; } = false;
    public int xview { get; set; } = 0;
    public int yview { get; set; } = 0;
    public int wview { get; set; } = 1366;
    public int hview { get; set; } = 768;
    public int xport { get; set; } = 0;
    public int yport { get; set; } = 0;
    public int wport { get; set; } = 1366;
    public int hport { get; set; } = 768;
    public uint hborder { get; set; } = 32;
    public uint vborder { get; set; } = 32;
    public int hspeed { get; set; } = -1;
    public int vspeed { get; set; } = -1;
    public AssetIDReference? objectId { get; set; } = null;
}
public class GMRoomSettings
{
    public bool inheritRoomSettings { get; set; } = false;
    public uint Width { get; set; } = 1366;
    public uint Height { get; set; } = 768;
    public bool persistent { get; set; } = false;
}
public class GMRoomViewSettings
{
    public bool inheritViewSettings { get; set; } = false;
    public bool enableViews { get; set; } = false;
    public bool clearViewBackground { get; set; } = false;
    public bool clearDisplayBuffer { get; set; } = true;
}
public class GMRoomPhysicsSettings
{
    public bool inheritPhysicsSettings { get; set; } = false;
    public bool PhysicsWorld { get; set; } = false;
    public float PhysicsWorldGravityX { get; set; } = 0f;
    public float PhysicsWorldGravityY { get; set; } = 10f;
    public float PhysicsWorldPixToMetres { get; set; } = 0.1f;
}
// literally for only one scenario
public class ParentData
{
    public string name { get; set; }
    public string path { get; set; } = "folders/Rooms.yy";
}
public class GMRoom : GMAssetBase
{
    public GMRoom(string room_name)
    {
        resourceType = "GMRoom";
        resourceVersion = "1.0";
        // this is that scenario
        parent = new ParentData();
        parent.name = "Rooms";

        name = room_name;
    }
    
    public bool isDnd { get; set; } = false;
    public float volume { get; set; } = 1f;
    public AssetIDReference? parentRoom { get; set; } = null;
    public List<GMRView> views { get; set; } = new();
    public List<dynamic> layers { get; set; } = new();
    public bool inheritLayers { get; set; } = false;
    public string creationCodeFile { get; set; } = String.Empty;
    public bool inheritCode { get; set; } = false;
    public List<AssetIDReference> instanceCreationOrder { get; set; } = new();
    public bool inheritCreationOrder { get; set; }
    public AssetIDReference? sequenceId { get; set; } = null;
    public GMRoomSettings roomSettings { get; set; } = new GMRoomSettings();
    public GMRoomViewSettings viewSettings { get; set; } = new GMRoomViewSettings();
    public GMRoomPhysicsSettings physicsSettings { get; set; } = new GMRoomPhysicsSettings();
    public ParentData parent { get; set; }
}
#endregion
// make sure the datafile is loaded.
EnsureDataLoaded();

// fetch data path
string data_path = $"{Path.GetDirectoryName(FilePath)}\\";

// get the folder that the script will operate in
string main_folder = $"{data_path}Exported_Rooms\\";

// create the directory
Directory.CreateDirectory(main_folder);

void DumpRoom(UndertaleRoom room_data)
{
    string room_directory = $"{main_folder}\\{room_data.Name.Content}\\";
    // create the room folder
    Directory.CreateDirectory(room_directory);
    // construct the object
    GMRoom room = new GMRoom(room_data.Name.Content);
    // loop through all of the views, constructing them into the GMS2 format.
    foreach (UndertaleRoom.View view in room_data.Views)
    {
        // views
        room.views.Add(new GMRView
        {
            // inherit doesnt exist, probably not compiled.
            visible = view.Enabled,
            xview = view.ViewX,
            yview = view.ViewY,
            wview = view.ViewWidth,
            hview = view.ViewHeight,
            xport = view.PortX,
            yport = view.PortY,
            wport = view.PortWidth,
            hport = view.PortHeight,
            hborder = view.BorderX,
            vborder = view.BorderY,
            hspeed = view.SpeedX,
            vspeed = view.SpeedY,
            objectId = AssetIDReference.Create(view.ObjectId, "objects")
        });
    }
    
    #region layer handling
    // layers are the hardest part of the room decompiler.

    // loop through each layer
    foreach (UndertaleRoom.Layer layer in room_data.Layers)
    {
        // this is the end result layer
        GMRLayerBase current_layer = new();

        switch (layer.LayerType)
        {
            case UndertaleRoom.LayerType.Path:
                {
                    GMRPathLayer new_layer = new();
                    // path layers dont seem to be fully supported as of now.
                    current_layer = new_layer;
                    break;
                }
            case UndertaleRoom.LayerType.Background:
                {
                    var new_layer = new GMRBackgroundLayer();
                    // translate data
                    new_layer.spriteId = AssetIDReference.Create(layer.BackgroundData.Sprite, "sprites");
                    new_layer.colour = layer.BackgroundData.Color;
                    new_layer.x = layer.XOffset;
                    new_layer.y = layer.YOffset;
                    new_layer.htiled = layer.BackgroundData.TiledHorizontally;
                    new_layer.vtiled = layer.BackgroundData.TiledVertically;
                    new_layer.hspeed = layer.HSpeed;
                    new_layer.vspeed = layer.VSpeed;
                    new_layer.stretch = layer.BackgroundData.Stretch;
                    new_layer.animationFPS = layer.BackgroundData.AnimationSpeed;
                    new_layer.animationSpeedType = (int)layer.BackgroundData.AnimationSpeedType;
                    new_layer.userdefinedAnimFPS = false;
                    // push to to end result
                    current_layer = new_layer;
                    break;
                }
            case UndertaleRoom.LayerType.Instances:
                {
                    GMRInstanceLayer new_layer = new();
                    foreach (UndertaleRoom.GameObject inst in layer.InstancesData.Instances)
                    {
                        // create the name of the instance, it will be used for creation code file names and the instance name itself
                        string instance_name = $"inst_{inst.InstanceID}";
                        // code dump
                        if (inst.CreationCode is not null)
                        {
                            string file_path = $"{room_directory}\\InstanceCreationCode_{instance_name}.gml";
                            File.WriteAllText(file_path, new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, inst.CreationCode, decompilerSettings).DecompileToString());
                        }
                        // construct the instance
                        new_layer.instances.Add(new GMRInstance
                        {
                            name = instance_name,
                            isDnd = false,
                            objectId = AssetIDReference.Create(inst.ObjectDefinition, "objects"),
                            inheritCode = false,
                            hasCreationCode = (inst.CreationCode is not null),
                            colour = inst.Color,
                            rotation = inst.Rotation,
                            scaleX = inst.ScaleX,
                            scaleY = inst.ScaleY,
                            imageSpeed = inst.ImageSpeed,
                            imageIndex = inst.ImageIndex,
                            inheritedItemId = null,
                            frozen = false, // doesnt seem to be implemented.
                            ignore = false, // same here?
                            inheritItemSettings = false, // again?
                            x = inst.X,
                            y = inst.Y
                        });
                        // add an entry to instanceCreationOrder
                        AssetIDReference order_asset = AssetIDReference.Create(room_data, "rooms");
                        order_asset.name = instance_name;
                        room.instanceCreationOrder.Add(order_asset);
                    }
                    // push to end result
                    current_layer = new_layer;
                    break;
                }
            case UndertaleRoom.LayerType.Assets:
                {
                    GMRAssetLayer new_layer = new();
                    // legacy tile stuff, referenced quantumV's room decompiler out of desperation because idk how they worked (thanks for making it easier)
                    foreach (UndertaleRoom.Tile tile_asset in layer.AssetsData.LegacyTiles)
                    {
                        new_layer.assets.Add(new GMRGraphic
                        {
                            name = $"graphic_{tile_asset.InstanceID}",
                            spriteId = AssetIDReference.Create(tile_asset.ObjectDefinition, "sprites"),
                            x = tile_asset.X,
                            y = tile_asset.Y,
                            w = tile_asset.Width * tile_asset.ScaleX,
                            h = tile_asset.Height * tile_asset.ScaleY,
                            u0 = tile_asset.SourceX,
                            v0 = tile_asset.SourceY,
                            u1 = tile_asset.SourceX + Convert.ToUInt32(tile_asset.Width),
                            v1 = tile_asset.SourceY + Convert.ToUInt32(tile_asset.Height),
                        });
                    }
                    // normal assets
                    foreach (UndertaleRoom.SpriteInstance sprite_asset in layer.AssetsData.Sprites)
                    {
                        new_layer.assets.Add(new GMRAsset
                        {
                            name = sprite_asset.Name.Content,
                            x = sprite_asset.X,
                            y = sprite_asset.Y,
                            spriteId = AssetIDReference.Create(sprite_asset.Sprite, "sprites"),
                            headPosition = sprite_asset.FrameIndex,
                            rotation = sprite_asset.Rotation,
                            scaleX = sprite_asset.ScaleX,
                            scaleY = sprite_asset.ScaleY,
                            animationSpeed = sprite_asset.AnimationSpeed,
                            colour = sprite_asset.Color,
                            inheritedItemId = null, // probably not compiled
                            frozen = false, // oh god this again
                            ignore = false,
                            inheritItemSettings = false,
                        });
                    }
                    // push to end result
                    current_layer = new_layer;
                    break;
                }
            case UndertaleRoom.LayerType.Tiles:
                {
                    GMRTileLayer new_layer = new();
                    // construct tile data, itll be for the tileset handling below
                    GMRTileData tile_data = new GMRTileData
                    {
                        SerialiseWidth = (int)layer.TilesData.TilesX,
                        SerialiseHeight = (int)layer.TilesData.TilesY,
                    };

                    new_layer.tilesetId = AssetIDReference.Create(layer.TilesData.Background, "tilesets");
                    new_layer.x = layer.XOffset;
                    new_layer.y = layer.YOffset;
                    new_layer.tiles = tile_data;

                    // obtain tile data
                    foreach (uint[] row in layer.TilesData.TileData)
                    {
                        foreach (uint tile in row)
                        {
                            tile_data.TileSerialiseData.Add(tile);
                        }
                    }
                    current_layer = new_layer;
                    break;
                }
            case UndertaleRoom.LayerType.Effect:
                {
                    GMREffectLayer new_layer = new();
                    // afaik the same as the base
                    current_layer = new_layer;
                    break;
                }
            default:
                {
                    GMRLayerBase new_layer = new();
                    current_layer = new_layer;
                    break;
                }
        }
        // fetch these from the 'layer' variable
        current_layer.name = layer.LayerName.Content;
        current_layer.visible = layer.IsVisible;
        current_layer.depth = layer.LayerDepth;
        current_layer.effectEnabled = layer.EffectEnabled;
        // this made me get stuck for like an hour I didnt even know you could declare nullable things like this it feels wrong
        current_layer.effectType = layer.EffectType?.Content;

        foreach (UndertaleRoom.EffectProperty effect_property in layer.EffectProperties)
        {
            current_layer.properties.Add(new GMREffectProperty
            {
                name = effect_property.Name.Content,
                type = (int)effect_property.Kind,
                value = effect_property.Value.Content
            });
        }

        // TODO: better checking for these vars
        current_layer.userdefinedDepth = true;
        current_layer.inheritLayerDepth = false;
        current_layer.inheritLayerSettings = false;
        //current_layer.inheritVisibility = true;
        //current_layer.inheritSubLayers = false;
        current_layer.hierarchyFrozen = false;

        current_layer.gridX = room_data.GridWidth;
        current_layer.gridY = room_data.GridHeight;

        room.layers.Add(current_layer);
    }
    #endregion

    // decompile room CC
    room.creationCodeFile = room_data.CreationCodeId is not null ? $"${{project_dir}}\\rooms\\{room_data.Name.Content}\\RoomCreationCode.gml" : String.Empty; 
    if (room_data.CreationCodeId is not null)
    {
        string file_path = $"{room_directory}\\RoomCreationCode.gml";
        File.WriteAllText(file_path, new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, room_data.CreationCodeId, decompilerSettings).DecompileToString());
    }
    // settings stuff
    room.roomSettings = new GMRoomSettings
    {
        inheritRoomSettings = false,
        Width = room_data.Width,
        Height = room_data.Height,
        persistent = room_data.Persistent
    };
    room.viewSettings = new GMRoomViewSettings
    {
        inheritViewSettings = false,
        enableViews = room_data.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews),
        clearViewBackground = room_data.DrawBackgroundColor,
        clearDisplayBuffer = room_data.DrawBackgroundColor
    };
    room.physicsSettings = new GMRoomPhysicsSettings
    {
        inheritPhysicsSettings = false,
        PhysicsWorld = room_data.World,
        PhysicsWorldGravityX = room_data.GravityX,
        PhysicsWorldGravityY = room_data.GravityY,
        PhysicsWorldPixToMetres = room_data.MetersPerPixel
    };

    // turn object into json
    string json_string = JsonSerializer.Serialize(room, new JsonSerializerOptions { AllowTrailingCommas = true, WriteIndented = true });
    File.WriteAllText($"{room_directory}\\{room_data.Name.Content}.yy", json_string);
    IncrementProgressParallel();
}

async Task DumpAllRooms()
{
    int rooms_completed = 0;
    // loop through each room and dump them
    foreach (UndertaleRoom room in Data.Rooms)
    {
        // give the script some time to think (required to make the progress bar work)
        await Task.Delay(1);
        // dump the room
        DumpRoom(room);
    }
}
// progress bar stuff
SetProgressBar(null, "Rooms", 0, Data.Rooms.Count);
StartProgressBarUpdater();
// run the main task
await DumpAllRooms();
// clean up progress bar
await StopProgressBarUpdater();
HideProgressBar();

ScriptMessage($"Export Complete.");