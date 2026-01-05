
using Raylib_cs.BleedingEdge;
using RetroEditor.Plugins;

/// <summary>
/// This class is responsible for storing the tile palette data for rendering. 
/// </summary>
/// <remarks>
/// It is used internally by the editor, but must be constructed by the plugin.
/// </remarks>
public sealed class TilePaletteStore
{
    private Texture2D[] _bitmaps;
    private uint _largestWidth, _largestHeight;
    private ITilePalette _iTilePalette;

    /// <summary>
    /// Construct a new tile palette store.
    /// </summary>
    /// <param name="iTilePalette">Object that implements the ITilePalette interface</param>
    public TilePaletteStore(ITilePalette iTilePalette)
    {
        _iTilePalette = iTilePalette;
        var tiles = iTilePalette.FetchTiles();
        _bitmaps = new Texture2D[iTilePalette.MaxTiles];
        _largestWidth = 0;
        _largestHeight = 0;
        for (int a=0;a<tiles.Length;a++)
        {
            _largestHeight = Math.Max(_largestHeight, tiles[a].Height);
            _largestWidth = Math.Max(_largestWidth, tiles[a].Width);
            var image = new Image
            {
                Width = (int)tiles[a].Width,
                Height = (int)tiles[a].Height,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR8G8B8A8
            };
            _bitmaps[a] = Raylib.LoadTextureFromImage(image);
        }
    }

    internal void Update(float seconds)
    {
        _iTilePalette.Update(seconds);
        
        // Grab latest version of tile data?
        if (_iTilePalette.IsDirty)
        {
            var tiles = _iTilePalette.FetchTiles();
            for (int a = 0; a < tiles.Length; a++)
            {
                var pixels = tiles[a].GetImageData();

                Raylib.UpdateTexture(_bitmaps[a], pixels.ToArray());
            }
        }
    }

    internal ITilePalette TilePalette => _iTilePalette;
    internal uint LargestWidth => _largestWidth;
    internal uint LargestHeight => _largestHeight;
    internal ReadOnlySpan<Texture2D> Bitmaps => _bitmaps;
}

