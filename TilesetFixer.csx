/*

    This script is the messiest thing ive ever made.
    But if it aint broke, dont fix it.

*/


using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;
using UndertaleModLib.Util;
using ImageMagick;
using System.Windows.Forms;

// Ensure data is loaded before accessing it
EnsureDataLoaded();

string root_folder = Path.GetDirectoryName(FilePath) + "\\";
string script_folder = $"{root_folder}Fixed_Tilesets\\";

Directory.CreateDirectory(script_folder);
TextureWorker worker = new TextureWorker();
IMagickImage<byte> final_result = null;

public class TileData
{
    public TileData(UndertaleBackground bg)
    {
        name = bg.Name.Content;
        width = (int)bg.GMS2TileWidth;
        height = (int)bg.GMS2TileHeight;
        xoff = (int)bg.GMS2OutputBorderX;
        yoff = (int)bg.GMS2OutputBorderX;
        hsep = (int)bg.GMS2OutputBorderX * 2;
        vsep = (int)bg.GMS2OutputBorderY * 2;
        out_tilehborder = (int)bg.GMS2OutputBorderX;
        out_tilevborder = (int)bg.GMS2OutputBorderY;
        columns = (int)bg.GMS2TileColumns;
        tile_count = (int)bg.GMS2TileCount;
    }
    public string name { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int xoff { get; set; }
    public int yoff { get; set; }
    public int hsep { get; set; }
    public int vsep { get; set; }
    public int out_tilehborder { get; set; }
    public int out_tilevborder { get; set; }
    public int columns { get; set; }
    public int tile_count { get; set; }
}

// create the directory
Directory.CreateDirectory($"{root_folder}Corrected_Tilesets");

public Bitmap ConvertToBitmap(IMagickImage<byte> img)
{
    using (MemoryStream memoryStream = new MemoryStream())
    {
        img.Write(memoryStream, MagickFormat.Png);
        memoryStream.Position = 0;
        return new Bitmap(memoryStream);
    }
}

public Bitmap? ProcessTileset(UndertaleBackground bg, int rows)
{
    // create tiledata based on the background
    TileData td = new TileData(bg);
    // obtain the image for the background
    using var image = worker.GetTextureFor(bg.Texture, bg.Name.Content);
    // seperate the image into a list of tiles.
    var tiledImage = image.CropToTiles(td.width + td.hsep, td.height + td.vsep).ToList();
    max_rows = tiledImage.Count;
    // set the geometry to the tileset dimensions
    var geometry = new MagickGeometry(td.width, td.height);
    //remove checkerboard
    tiledImage[0] = new MagickImage(MagickColors.Transparent, td.width, td.height);
    // iterate through each tile, fixing the padding by setting the tileset to the correct dimensions.
    for (int i = 0; i <= tiledImage.Count - 1; i++)
    {
        tiledImage[i].Extent(geometry, Gravity.Center, MagickColors.Transparent);
    }

    using var exported_image = new MagickImageCollection();
    // construct the image by each tile.
    foreach (var tile in tiledImage)
    {
        exported_image.Add(tile);
    }

    MontageSettings ms = new MontageSettings()
    {
        Geometry = geometry,
        TileGeometry = new MagickGeometry(rows, 0),
        BackgroundColor = MagickColors.None,
        Gravity = Gravity.Center
    };

    // save the image to a file when complete.
    using (var result = exported_image.Montage(ms))
    {
        final_result = result.Clone();
        var resized_result = result;
        resized_result.Resize(400, 400);
        return ConvertToBitmap(resized_result);
    }
}
// variables to manage state
int current_index = 0;
int rows = 0;
int tileset_count = Data.Backgrounds.Count;
int max_rows = 0;
// create form
Form form = new Form();
PictureBox display = new PictureBox();
TrackBar row_slider = new TrackBar();
TextBox value_box = new TextBox();
TrackBar index_slider = new TrackBar();
TextBox index_box = new TextBox();
Button save_button = new Button();

void InitializeForm()
{
    // settings
    form.ClientSize = new Size(400, 500);
    form.Text = "Tileset Fixer";

    // display
    display.Anchor = AnchorStyles.Top;
    display.Location = new Point(0, 0);
    display.Size = new Size(400, 400);
    form.Controls.Add(display);

    // value box
    value_box.Location = new Point(240, 16 + 400);
    value_box.Size = new Size(48, 20);
    value_box.Text = "0";
    value_box.TextChanged += (sender, e) => {
        if (int.TryParse(value_box.Text, out int new_value))
        {
            row_slider.Value = new_value;
            if (new_value > 0 && new_value < max_rows)
                rows = new_value;
            UpdateDisplay();
        }
    };
    form.Controls.Add(value_box);

    // row slider
    row_slider.Location = new Point(8, 8 + 400);
    row_slider.Size = new Size(224, 45);
    row_slider.Maximum = 100;
    row_slider.TickFrequency = 5;
    row_slider.LargeChange = 3;
    row_slider.SmallChange = 1;
    row_slider.Scroll += (sender, e) => {
        value_box.Text = row_slider.Value.ToString();
        rows = row_slider.Value;
        UpdateDisplay();
    };
    form.Controls.Add(row_slider);

    // Initial display
    UpdateDisplay();


    // index slider
    index_slider.Location = new Point(8, 58 + 400);
    index_slider.Size = new Size(224, 45);
    index_slider.Maximum = Data.Backgrounds.Count;
    index_slider.TickFrequency = 5;
    index_slider.LargeChange = 3;
    index_slider.SmallChange = 1;
    index_slider.Scroll += (sender, e) => {
        index_box.Text = index_slider.Value.ToString();
        current_index = index_slider.Value;
        UpdateDisplay();
    };
    form.Controls.Add(index_slider);

    // index box
    index_box.Location = new Point(240, 66 + 400);
    index_box.Size = new Size(48, 20);
    index_box.Text = "0";
    index_box.TextChanged += (sender, e) => {
        if (int.TryParse(index_box.Text, out int new_value))
        {
            index_slider.Value = new_value;
            if (new_value >= 0 && new_value < Data.Backgrounds.Count)
                current_index = new_value;
            UpdateDisplay();
        }
    };
    form.Controls.Add(index_box);
    save_button.Location = new Point(300, 410);
    save_button.Text = "Save";
    save_button.Click += (sender, e) => {
        TextureWorker.SaveImageToFile(final_result, $"{script_folder}{Data.Backgrounds[current_index].Name.Content}.png");
        current_index++;
        index_box.Text = current_index.ToString();
    };

    form.Controls.Add(save_button);
}

// Update the display with current tileset
void UpdateDisplay()
{
    display.Image = ProcessTileset(Data.Backgrounds[current_index], rows);
}

// Initialize and run the form
InitializeForm();
Application.Run(form);

// Cleanup
worker.Dispose();