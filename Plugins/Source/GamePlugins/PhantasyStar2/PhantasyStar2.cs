using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class PhantasyStar2 : IRetroPlugin, IMenuProvider
{
    // MD5 of Phantasy Star EU-US Rev 2
    private byte[] PhantasyStarUSEURev2 = new byte[] { 15, 163, 139, 18, 207, 10, 176, 22, 61, 134, 86, 0, 172, 115, 26, 154 };

    public static string Name => "Phantasy Star 2";

    public string RomPluginName => "Megadrive";

    public bool RequiresAutoLoad => false;

    const int MapDataTableEntries = 0x65;

    public bool CanHandle(string filename)
    {
        if (File.Exists(filename))
        {
            var md5 = MD5.Create().ComputeHash(File.ReadAllBytes(filename));
            return PhantasyStarUSEURev2.SequenceEqual(md5);
        }
        return false;
    }

    public int GetImageCount(IMemoryAccess rom)
    {
        return MapDataTableEntries;
    }

    public static string GetMapName(int mapIndex)
    {
        return mapIndex switch
        {
            0x00 => "Mota Overworld",
            0x01 => "Skure B2",
            0x02 => "Skure B1",
            0x03 => "Dezolis Skure",
            0x04 => "Paseo",
            0x05 => "Arima",
            0x06 => "Oputa",
            0x07 => "Zema",
            0x08 => "Kueri",
            0x09 => "Piata",
            0x0A => "Aukba",
            0x0B => "Zosa",
            0x0C => "Ryuon",
            0x0D => "Tube Near Paseo",
            0x0E => "Darum Tube",
            0x0F => "Tube Locked Door",
            0x10 => "Esper Mansion Basement 1",
            0x11 => "Esper Mansion Floor 1",
            0x12 => "Uzo",
            0x13 => "Underwater Passage",
            0x14 => "Crevice Basement 2",
            0x15 => "Crevice Basement 1",
            0x16 => "Crevice Ground Floor",
            0x17 => "Shure Ground Floor",
            0x18 => "Shure Floor 1",
            0x19 => "Shure Floor 2",
            0x1A => "Shure Floor 3",
            0x1B => "Nido Ground Floor",
            0x1C => "Nido Floor 1",
            0x1D => "Nido Floor 2",
            0x1E => "Roron Basement 5",
            0x1F => "Roron Basement 4",
            0x20 => "Roron Basement 3",
            0x21 => "Roron Basement 2",
            0x22 => "Roron Basement 1",
            0x23 => "Roron Ground Floor",
            0x24 => "Yellow Dam Ground Floor",
            0x25 => "Yellow Dam Floor 1",
            0x26 => "Yellow Dam Floor 2",
            0x27 => "Yellow Dam Floor 3",
            0x28 => "Red Dam Ground Floor",
            0x29 => "Red Dam Floor 1",
            0x2A => "Red Dam Floor 2",
            0x2B => "Blue Dam Ground Floor",
            0x2C => "Blue Dam Floor 1",
            0x2D => "Blue Dam Floor 2",
            0x2E => "Blue Dam Floor 3",
            0x2F => "Blue Dam Floor 4",
            0x30 => "Green Dam Ground Floor",
            0x31 => "Green Dam Floor 1",
            0x32 => "Biosystems Lab Basement 1",
            0x33 => "Biosystems Lab Ground Floor",
            0x34 => "Biosystems Lab Floor 1",
            0x35 => "Biosystems Lab Floor 2",
            0x36 => "Climatrol Ground Floor",
            0x37 => "Climatrol Floor 1",
            0x38 => "Climatrol Floor 2",
            0x39 => "Climatrol Floor 3",
            0x3A => "Climatrol Floor 4",
            0x3B => "Climatrol Floor 5",
            0x3C => "Climatrol Floor 6",
            0x3D => "Climatrol Floor 7",
            0x3E => "Control Tower Ground Floor",
            0x3F => "Control Tower Floor 1",
            0x40 => "Tube Near Zema",
            0x41 => "Gaira",
            0x42 => "Gaira (Copy)",
            0x43 => "Naval Ground Floor",
            0x44 => "Naval Floor 1",
            0x45 => "Naval Floor 2",
            0x46 => "Naval Floor 3",
            0x47 => "Naval Floor 4",
            0x48 => "Menobe Ground Floor",
            0x49 => "Menobe Floor 1",
            0x4A => "Menobe Floor 2",
            0x4B => "Menobe Floor 3",
            0x4C => "Ikuto Basement 6",
            0x4D => "Ikuto Basement 5",
            0x4E => "Ikuto Basement 4",
            0x4F => "Ikuto Basement 3",
            0x50 => "Ikuto Basement 2",
            0x51 => "Ikuto Basement 1",
            0x52 => "Ikuto Ground Floor",
            0x53 => "Guaron Ground Floor",
            0x54 => "Guaron Floor 1",
            0x55 => "Guaron Floor 2",
            0x56 => "Guaron Floor 3",
            0x57 => "Guaron Floor 4",
            0x58 => "Guaron Floor 5",
            0x59 => "Guaron Floor 6",
            0x5A => "Guaron Floor 7",
            0x5B => "Guaron Floor 8",
            0x5C => "Guaron Floor 9",
            0x5D => "Guaron Floor 10",
            0x5E => "Guaron Floor 11",
            0x5F => "Guaron Floor 12",
            0x60 => "Guaron Floor 13",
            0x61 => "Guaron Floor 14",
            0x62 => "Guaron Floor 15",
            0x63 => "Noah Ground Floor",
            0x64 => "Noah Floor 1",

            _ => "Unknown",
        };
    }

    public PhantasyStar2Map GetImage(IMemoryAccess rom, int mapIndex)
    {
        return new PhantasyStar2Map(rom, mapIndex);
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        var imageMenu = menu.AddItem("Image Viewer");
        for (int a = 0; a < GetImageCount(rom); a++)
        {
            var idx = a;    // Otherwise lambda captures last value of a
            var mapName = GetMapName(idx);
            menu.AddItem(imageMenu, mapName, 
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow(mapName, GetImage(rom, idx));
                });
        }
    }

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x2b2, new byte[] { 0x73, 0x48 });     // Skip Sega logo
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        throw new NotImplementedException();
    }
}

public class PhantasyStar2Map : IImage, IUserWindow
{
    int index;
    IMemoryAccess rom;
    const uint MapDataTableAddress = 0x27C0A;

    public PhantasyStar2Map(IMemoryAccess rom, int index)
    {
        this.rom = rom;
        this.index = index;
    }

    public uint Width => GetMapWidth(index);

    public uint Height => GetMapHeight(index);

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public Pixel[] GetImageData(float seconds)
    {
        return RenderMap(index);
    }

    private MapDataEntry GetMapDataEntry(int index)
    {
        uint address = MapDataTableAddress + (uint)(index * 20);
        MapDataEntry mapDataEntry = new MapDataEntry();
        mapDataEntry.LoadDataFromAddress(rom, address);
        return mapDataEntry;
    }

    public uint GetMapWidth(int mapIndex)
    {
        return (uint)GetMapDataEntry(mapIndex).MaxCameraPosX;
    }

    public uint GetMapHeight(int mapIndex)
    {
        return (uint)GetMapDataEntry(mapIndex).MaxCameraPosY;
    }

    
    void RenderLayer(byte[] chunks, byte[] tileBitmaps, PalettePtr.PaletteEntry palEntry, int chunkMaxX, int chunkMaxY, byte[] mapLayout, bool isFore, Pixel[] layer, int stride)
    {
        for (int mapY=0;mapY<chunkMaxY;mapY++)
        //int mapY = 0x1f;
        {
            for (int mapX=0;mapX<chunkMaxX;mapX++)
            //int mapX = 0xf;
            {
                var mapOffs = mapX + mapY * chunkMaxX;
                if (mapOffs >= mapLayout.Length)
                {
                    for (int chunkY = 0; chunkY < 4; chunkY++)
                    {
                        for (int chunkX = 0; chunkX < 4; chunkX++)
                        {
                            for (int tileY = 0; tileY < 8; tileY++)
                            {
                                for (int tileX = 0; tileX < 8; tileX++)
                                {
                                    var gX = mapX * 32 + chunkX * 8 + tileX;
                                    var gY = mapY * 32 + chunkY * 8 + tileY;
                                    layer[mapX+mapY*stride] = new Pixel(0xFF, 0xFF, 0x00);
                                }
                            }
                        }
                    }
                    continue;
                }
                var chunkNum = mapLayout[mapOffs];
                var chunkOffset = chunkNum * 32 ;

                // 64 bytes at this location, 4x4 megadrive tiles
                for (int chunkY=0;chunkY<4;chunkY++)
                {
                    for (int chunkX=0;chunkX<4;chunkX++)
                    {
                        ushort vdpWord = (ushort)(chunks[chunkOffset + chunkX*2 + chunkY * 8 + 0] << 8);
                        vdpWord |= (ushort)(chunks[chunkOffset + chunkX*2 + chunkY * 8 + 1] << 0);
                        var vdpTileIndex = vdpWord & 0x7FF;
                        var flipX = (vdpWord & 0x800) == 0x800;
                        var flipY = (vdpWord & 0x1000) == 0x1000;
                        var palIndexOffset = ((vdpWord & 0x6000) >> 13)-2;      // -2 because at least for the first map, the palette is loaded in position 2 and 3
                        var priority = (vdpWord & 0x8000) == 0x8000;

                        var tileOffset = vdpTileIndex * 8 * 8;

                        for (int tileY=0;tileY<8;tileY++)
                        {
                            for (int tileX=0;tileX<8;tileX++)
                            {
                                var gX = mapX * 32 + chunkX * 8 + (flipX ? (7-tileX) : tileX);
                                var gY = mapY * 32 + chunkY * 8 + (flipY ? (7-tileY) : tileY);
                                var offs = tileOffset + tileX + tileY * 8;
                                if (offs >= tileBitmaps.Length)
                                {
                                    layer[gX+gY*stride] = new Pixel(0xFF, 0x00, 0xFF);
                                    continue;
                                }
                                var palIndex = tileBitmaps[offs];
                                if (isFore && palIndex == 0)
                                    continue; 
                                var rgb = palEntry.GetColour(rom, palIndex + palIndexOffset*16);
                                layer[gX+gY*stride] = new Pixel(rgb.R, rgb.G, rgb.B);
                            }
                        }
                    }
                }
            }
        }
    }

    public Pixel[] RenderMap(int index)
    {
        var mapDataEntry = GetMapDataEntry(index);

        PTR_ARRAY_9342 unknown_9342 = new PTR_ARRAY_9342();
        unknown_9342.Load(rom, 0x9342);

        PalettePtr palettePtr = new PalettePtr();
        palettePtr.Load(rom);

        int PlanetIndex = mapDataEntry.PlanetIndex;
        
        int tableIndex = mapDataEntry.Table9342Index;

        var addressA = unknown_9342.GetCompressedBitmapData(tableIndex);

        Nemesis nemesis = new Nemesis();
        byte[] tileBitmaps = nemesis.Decompress(rom, addressA);

        var addressB = unknown_9342.GetAddressB(tableIndex);
        var decomp2 = new BitPack();
        byte[] chunks = decomp2.Decompress(rom, addressB);

        var cameraMaxYPos = mapDataEntry.MaxCameraPosY;
        var cameraMaxXPos = mapDataEntry.MaxCameraPosX;

        var formationsIndex1 = mapDataEntry.Formation1Index;
        var mapLayoutBGAddress = mapDataEntry.MapLayoutBG;
        var mapLayoutBG = new BitPack().Decompress(rom, mapLayoutBGAddress);

        var formationsIndex2 = mapDataEntry.Formation2Index;
        var mapLayoutFGAddress = mapDataEntry.MapLayoutFG;
        var mapLayoutFG = new BitPack().Decompress(rom, mapLayoutFGAddress);


        var chunkMaxY = cameraMaxYPos / 32;
        var chunkMaxX = cameraMaxXPos / 32;

        int palette = mapDataEntry.PaletteIndex;

        var palEntry = palettePtr.GetEntry(palette);


        var mapLayer = new Pixel[cameraMaxXPos*cameraMaxYPos];

        RenderLayer(chunks, tileBitmaps, palEntry, chunkMaxX, chunkMaxY, mapLayoutBG, false, mapLayer, cameraMaxXPos);
        RenderLayer(chunks, tileBitmaps, palEntry, chunkMaxX, chunkMaxY, mapLayoutFG, true, mapLayer, cameraMaxXPos);

        return mapLayer;
    }

    public float UpdateInterval => 1 / 30.0f;

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddImageView(this);
    }

    public void OnClose()
    {
    }
}