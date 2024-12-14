using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Vorbis;

EnsureDataLoaded();

public string root_folder = Path.GetDirectoryName(FilePath) + "\\";
public string main_folder = root_folder + "DecompiledProject\\sounds\\";

public List<string> error_list = new List<string>();

public class GMAssetBase
{
    public string resourceType { get; set; } = String.Empty;
    public string resourceVersion { get; set; } = String.Empty;
    public string name { get; set; } = String.Empty;
}

public class GMAssetReference
{
    public GMAssetReference(string name, string path)
    {
        this.name = name;
        this.path = path;
    }
    public string name { get; set; }
    public string path { get; set; }
}

public class GMSound : GMAssetBase
{
    public GMSound()
    {
        resourceType = this.GetType().Name;
        resourceVersion = "1.0";
        name = String.Empty;
    }
    public int conversionMode { get; set; }
    public int compression { get; set; }
    public float volume { get; set; }
    public bool preload { get; set; }
    public int bitRate { get; set; } = 128; // cant obtain original value afaik
    public int sampleRate { get; set; } = 44100;
    public int type { get; set; } = 0;
    public int bitDepth { get; set; } = 1; // cant obtain original value afaik
    public GMAssetReference audioGroupId { get; set; }
    public string soundFile { get; set; }
    public float duration { get; set; } = 0f;
    public GMAssetReference parent { get; set; }
}

public class SoundData
{
    public SoundData(UndertaleSound snd)
    {
        IsDecompressedOnLoad = snd.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
        IsEmbedded = snd.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
        IsDecompressedOnLoad = snd.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsDecompressedOnLoad);
    }
    public bool IsDecompressedOnLoad { get; set; } = false;
    public bool IsCompressed { get; set; } = false;
    public bool IsEmbedded { get; set; } = false;
}

Directory.CreateDirectory(main_folder);

SetProgressBar(null, "Sounds", 0, Data.Sounds.Count);
StartProgressBarUpdater();

await Task.Run(DumpAllSounds);

await StopProgressBarUpdater();
HideProgressBar();

if (error_list.Count > 0)
{
    error_list.Add("Every sound with an error has been skipped.");
    File.WriteAllLines(root_folder + "DecompiledProject\\errors.txt", error_list);
    ScriptMessage($"Sounds decompiled with {error_list.Count - 1} errors.");
}


public void DumpSound(UndertaleSound snd)
{
    string current_path = $"{main_folder}{snd.Name.Content}\\";
    string sound_path = current_path + snd.File.Content;

    if (Directory.Exists(current_path)) Directory.Delete(current_path, true);

    Directory.CreateDirectory(current_path);
    SoundData sd = new SoundData(snd);

    bool is_external = File.Exists(root_folder + snd.File.Content);

    GMSound new_sound = new GMSound()
    {
        name = snd.Name.Content,
        volume = snd.Volume,
        parent = new GMAssetReference("Sounds", "folders/Sounds.yy"),
        preload = snd.Preload,
    };

    // compression
    if (sd.IsEmbedded) new_sound.compression = 0;
    else if (sd.IsCompressed) new_sound.compression = 1;
    else if (sd.IsDecompressedOnLoad) new_sound.compression = 2;
    else if (is_external) new_sound.compression = 3;

    // handle audiogroups
    string audio_group_name = String.Empty;
    string audio_group_path = $"{root_folder}audiogroup{snd.GroupID}.dat";

    var audio_group = Data.AudioGroups.ElementAtOrDefault(snd.GroupID);

    if (audio_group is null) audio_group_name = "audiogroup_default";
    else audio_group_name = audio_group.Name.Content;

    new_sound.audioGroupId = new GMAssetReference(audio_group_name, $"audiogroups/{audio_group_name}");


    // declare these for checking later, trimmed to 3 for ID3 checking
    byte[] wav_signature = new byte[] { (byte)'R', (byte)'I', (byte)'F' };
    byte[] ogg_signature = new byte[] { (byte)'O', (byte)'g', (byte)'g' };
    byte[] mp3_signature = new byte[] { (byte)'I', (byte)'D', (byte)'3' };

    // if not using audiogroup_default and is an external audiogroup
    if (snd.GroupID != 0 && File.Exists(audio_group_path))
    {
        // referenced from ExportAllSounds.csx
        try
        {
            UndertaleData data = null;
            // read the audio group
            using (var stream = new FileStream(audio_group_path, FileMode.Open, FileAccess.Read))
                data = UndertaleIO.Read(stream);

            File.WriteAllBytes(sound_path, data.EmbeddedAudio[snd.AudioID].Data);
        }
        catch (Exception e)
        {
            error_list.Add($"An error occured while trying to load {Data.AudioGroups[snd.GroupID].Name.Content}");
            IncrementProgress();
            return;
        }
    }
    else if (snd.AudioFile is not null)
    {
        File.WriteAllBytes(sound_path, snd.AudioFile.Data);
    }
    else if (is_external)
    {
        File.Copy(root_folder + snd.File.Content, current_path + snd.File.Content);
    }

    byte[] file_data = File.ReadAllBytes(sound_path);

    // this array is to get the first 4 bytes of the file
    byte[] file_signature = new byte[3];

    // copy the first 4 bytes into this array
    Array.Copy(file_data, 0, file_signature, 0, 3);

    WaveStream ws = null;
    string file_ext = String.Empty;

    // run through every common file type
    if (file_signature.SequenceEqual(wav_signature)) file_ext = "wav";
    else if (file_signature.SequenceEqual(ogg_signature)) file_ext = "ogg";
    else if (file_signature.SequenceEqual(mp3_signature)) file_ext = "mp3";
    else
    {
        error_list.Add($"Unable to fetch format for \'{snd.Name.Content}\'.");
        IncrementProgress();
        return;
    }

    // rename files without extension, check if its part of a user-made group because I feel like there would be issues
    if (sound_path.IndexOf(file_ext, 0, StringComparison.OrdinalIgnoreCase) == -1 && snd.GroupID != 0)
    {
        File.Move(sound_path, sound_path += $".{file_ext}");
    }

    switch (file_ext)
    {
        case "wav":
            ws = new WaveFileReader(sound_path);
            break;

        case "ogg":
            ws = new VorbisWaveReader(sound_path);
            break;

        case "mp3":
            ws = new Mp3FileReader(sound_path);
            break;

    }
    // set the sound file name in the yy file
    new_sound.soundFile = (snd.File is not null) ? Path.GetFileName(sound_path) : String.Empty;

    if (ws is not null)
    {
        TimeSpan len = ws.TotalTime;
        double hours = len.TotalHours * 3600; // hours to seconds
        double minutes = len.TotalMinutes * 60; // minutes to seconds
        double seconds = len.TotalSeconds;
        double milliseconds = len.TotalMilliseconds / 1000; // ms to seconds

        new_sound.duration = (float)(hours + minutes + seconds + milliseconds) / 4; // them all together (and divided by 4 for some reason)

        // get sample rate from wavestream
        new_sound.sampleRate = ws.WaveFormat.SampleRate;

        // 3d sounds dont seem to decompile correctly with this method, find a more consistent way later.
        new_sound.type = ws.WaveFormat.Channels-1;
    }

    File.WriteAllText($"{current_path}{snd.Name.Content}.yy", JsonSerializer.Serialize(new_sound, new JsonSerializerOptions { AllowTrailingCommas = true, WriteIndented = true }));
    IncrementProgress();
}

public void DumpAllSounds()
{
    foreach (UndertaleSound sound in Data.Sounds)
        DumpSound(sound);
}
