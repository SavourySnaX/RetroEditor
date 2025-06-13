
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

public class StarFox : IRetroPlugin, IMenuProvider
{
    public static string Name => "Star Fox";

    public string RomPluginName => "SNES";

    public bool RequiresAutoLoad => false;

    byte[] starfox_us_rev2_headerless = [0xde, 0xf6, 0x6d, 0xb1, 0x2f, 0x5e, 0x64, 0x4c, 0x0c, 0xf0, 0x0c, 0x42, 0xcf, 0xa7, 0xae, 0x7b];

    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        throw new System.NotImplementedException("AutoLoadCondition not required");
    }

    public bool CanHandle(string path)
    {
        if (!File.Exists(path))
            return false;
        var bytes = File.ReadAllBytes(path);
        var hash = MD5.Create().ComputeHash(bytes);
        return hash.SequenceEqual(starfox_us_rev2_headerless);
    }

    public ISave Export(IMemoryAccess romAcess)
    {
        throw new System.NotImplementedException();
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
    }

    public StarFoxTesting GetTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        return new StarFoxTesting(editorInterface, rom);
    }


    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Testing",
            (editorInterface, menuItem) =>
            {
                editorInterface.OpenUserWindow($"Testing", GetTesting(editorInterface, rom));
            });
    }
}

public class StarFoxTesting : IUserWindow, IRender3DWidget
{
    public float UpdateInterval => 1 / 30.0f;

    public uint Width => 512;

    public uint Height => 512;

    public Vector3F CameraPosition => new Vector3F(0, -30, -100);
    public Vector3F CameraLookAt => new Vector3F(0, 0, 0);
    public Vector3F CameraUp => new Vector3F(0, -1, 0);
    public float CameraFovY => 45.0f;
    public bool CameraOrthographic => false;

    private struct ObjectHeader
    {
        public ushort vertOffset;
        public byte vertBank;
        public ushort faceOffset;
        public ushort zPosition;
        public byte scale;
        public ushort ColInfo;
        public ushort sizeX;
        public ushort sizeY;
        public ushort sizeZ;
        public ushort alignment;
        public ushort materials;
        public ushort id1;
        public ushort id2;
        public ushort id3;
        public ushort id4;
    }

    public Triangle[] Triangles
    {
        get
        {
            if (reload)
            {
                reload = false;
                modelNumber = temp_modelSelect.Value;
                LoadModelSafe(_rom);
            }
            return _triangles;
        }
    }

    public Line[] Lines => _lines;
    public Point[] Points => _points;

    private Triangle[] _triangles = new Triangle[0];
    private Line[] _lines = new Line[0];
    private Point[] _points = new Point[0];

    private IEditor _editorInterface;
    private IMemoryAccess _rom;
    private bool reload = false;
    private int modelNumber = 0;
    private ObjectHeader currentObject;
    private int colorIndex = 1;

    public StarFoxTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;

        LoadModelSafe(_rom);
    }

    private void LoadModelSafe(IMemoryAccess rom)
    {
        try
        {
            LoadModel(rom);
        }
        catch (Exception ex)
        {
            _editorInterface.Log(LogType.Error, $"Failed to load model: {ex.Message}");
        }
    }
    
    private string ModelHeader => "Vert | Bank | Face | ZPos | Scal | ColI | SizX | SizY | SizZ | Algn | Mats | ID1  | ID2  | ID3  | ID4 ";
    private string ModelName => $"{currentObject.vertOffset:X4} | {currentObject.vertBank:X2}   | {currentObject.faceOffset:X4} | {currentObject.zPosition:X4} | {currentObject.scale:X2}   | {currentObject.ColInfo:X4} | {currentObject.sizeX:X4} | {currentObject.sizeY:X4} | {currentObject.sizeZ:X4} | {currentObject.alignment:X4} | {currentObject.materials:X4} | {currentObject.id1:X4} | {currentObject.id2:X4} | {currentObject.id3:X4} | {currentObject.id4:X4}";

    private void LoadModel(IMemoryAccess rom)
    {
        _triangles = Array.Empty<Triangle>();
        _lines = Array.Empty<Line>();
        _points = Array.Empty<Point>();
        
        var modelIdTable = rom.ReadBytes(ReadKind.Rom, 0x264B, 250 * 2);
        var modelEntry = rom.FetchMachineOrder16(modelNumber * 2, modelIdTable);
        var modelOffset = modelEntry - 0x8000;

        var objectEntry = rom.ReadBytes(ReadKind.Rom, (uint)modelOffset, 0x1C);

        currentObject.vertOffset = rom.FetchMachineOrder16(0, objectEntry);
        currentObject.vertBank = objectEntry[2];
        currentObject.faceOffset = rom.FetchMachineOrder16(3, objectEntry);
        currentObject.zPosition = rom.FetchMachineOrder16(5, objectEntry);
        currentObject.scale = objectEntry[7];
        currentObject.ColInfo = rom.FetchMachineOrder16(8, objectEntry);
        currentObject.sizeX = rom.FetchMachineOrder16(10, objectEntry);
        currentObject.sizeY = rom.FetchMachineOrder16(12, objectEntry);
        currentObject.sizeZ = rom.FetchMachineOrder16(14, objectEntry);
        currentObject.alignment = rom.FetchMachineOrder16(16, objectEntry);
        currentObject.materials = rom.FetchMachineOrder16(18, objectEntry);
        currentObject.id1 = rom.FetchMachineOrder16(20, objectEntry);
        currentObject.id2 = rom.FetchMachineOrder16(22, objectEntry);
        currentObject.id3 = rom.FetchMachineOrder16(24, objectEntry);
        currentObject.id4 = rom.FetchMachineOrder16(26, objectEntry);

        temp_modelLabel.Name = ModelName;
        temp_modelName.Name = NameTable.Length > modelNumber ? NameTable[modelNumber] : $"Model {modelOffset + 0x8000:X4}";

        if ((currentObject.vertBank == 0) && (currentObject.vertOffset == 0))
        {
            return; // No vertex data, nothing to do
        }

        if (currentObject.vertOffset < 0x8000 || currentObject.vertOffset > 0xFFFF)
        {
            throw new InvalidDataException($"Invalid vertex offset: {currentObject.vertOffset}");
        }
        if (currentObject.faceOffset < 0x8000 || currentObject.faceOffset > 0xFFFF)
        {
            throw new InvalidDataException($"Invalid face offset: {currentObject.faceOffset}");
        }
        var vertDataOffset = (uint)(currentObject.vertBank * 0x8000 + (currentObject.vertOffset - 0x8000));
        var faceDataOffset = (uint)(currentObject.vertBank * 0x8000 + (currentObject.faceOffset - 0x8000));

        // Experiment, extract things we need
        var vertData = rom.ReadBytes(ReadKind.Rom, vertDataOffset, 32768);
        var faceData = rom.ReadBytes(ReadKind.Rom, faceDataOffset, 32768);

        var allVertices = ProcessVertexData(rom, ref vertData).ToArray();

        var triangleCode = faceData[0];
        faceData = faceData[1..];

        if (triangleCode != 0x30)
        {
            throw new InvalidDataException($"Expected triangle code 0x30, got 0x{triangleCode:X2}");
        }
        var numTris = faceData[0];
        faceData = faceData[1..];

        var triangles = new int[3 * numTris];
        for (int a = 0; a < numTris; a++)
        {
            var p0 = faceData[0];
            var p1 = faceData[1];
            var p2 = faceData[2];
            faceData = faceData[3..];

            triangles[a * 3 + 0] = p0;
            triangles[a * 3 + 1] = p1;
            triangles[a * 3 + 2] = p2;
        }

        var faceOffsets = ProcessBSP(rom, faceData);

        var triangleList = new List<Triangle>();
        var lineList = new List<Line>();
        var pointList = new List<Point>();

        foreach (var fOffset in faceOffsets)
        {
            var face = ProcessFaceOrSpriteData(rom, faceData.Slice(fOffset), allVertices);
            triangleList.AddRange(face.tris);
            lineList.AddRange(face.lines);
            pointList.AddRange(face.points);
        }

        _triangles = triangleList.ToArray();
        _lines = lineList.ToArray();
        _points = pointList.ToArray();
    }

    public bool CodeIsVertexData(byte code)
    {
        return code == 0x04 || code == 0x38 || code == 0x1C || code == 0x20 || code==0x08 || code == 0x34;
    }

    public List<Vector3F> ProcessVertexData(IMemoryAccess rom, ref ReadOnlySpan<byte> vertexData)
    {
        var vertices = new List<Vector3F>();

        var vertexCode = vertexData[0];
        vertexData = vertexData[1..];

        while (vertexCode != 0x0C)
        {
            if (!CodeIsVertexData(vertexCode))
            {
                throw new InvalidDataException($"Expected vertex code 0x04 or 0x38, got 0x{vertexCode:X2}");
            }

            if (vertexCode == 0x1C)
            {
                // Animation data - For now, we just process the first set of vertices
                var frameCount = vertexData[0];
                var firstOffset = rom.FetchMachineOrder16(1, vertexData);
                vertexData = vertexData[(2 + firstOffset)..]; // Skip to next vertex code
            }
            else if (vertexCode == 0x20)
            {
                // Skip Code
                var offset = rom.FetchMachineOrder16(0, vertexData);
                vertexData = vertexData[(1 + offset)..]; // Skip to next vertex code
            }
            else
            {
                var numVertices = vertexData[0];
                vertexData = vertexData[1..];

                var isShort = vertexCode == 0x08 || vertexCode == 0x34;
                for (int a = 0; a < numVertices; a++)
                {
                    float x = !isShort ? (sbyte)vertexData[0] : (short)rom.FetchMachineOrder16(0, vertexData);
                    float y = !isShort ? (sbyte)vertexData[1] : (short)rom.FetchMachineOrder16(2, vertexData);
                    float z = !isShort ? (sbyte)vertexData[2] : (short)rom.FetchMachineOrder16(4, vertexData);
                    vertexData = !isShort ? vertexData[3..] : vertexData[6..];
                    vertices.Add(new Vector3F(x, y, z));
                    if ((vertexCode & 0x30) == 0x30) // Mirror X-Axis
                    {
                        vertices.Add(new Vector3F(-x, y, z));
                    }
                }
            }
            vertexCode = vertexData[0];
            vertexData = vertexData[1..];
        }

        return vertices;
    }

    public int[] ProcessBSP(IMemoryAccess rom, ReadOnlySpan<byte> bspData)
    {
        var faceOffset = new List<int>();
        var currentBspOffset = 0;
        // BSP data
        var bspCode = bspData[currentBspOffset];
        currentBspOffset++;

        // 0x14 is just a standard face group, 0x50 is a special sprite case
        if (bspCode == 0x14 || bspCode == 0x50)
        {
            return [0]; // No BSP tree, return this index
        }
        if (bspCode != 0x3C) // BSP TREE
        {
            throw new InvalidDataException("Expected BSP tree code 0x3C");
        }

        ProcessBSPElement(rom, bspData, currentBspOffset, ref faceOffset);

        return faceOffset.ToArray();
    }

    private void ProcessBSPElement(IMemoryAccess rom, ReadOnlySpan<byte> bspData, int currentBspOffset, ref List<int> faceOffset)
    {
        var type = bspData[currentBspOffset];
        currentBspOffset++;
        if (type == 0x28) // Branch node
        {
            var faceSplittingTriangle = bspData[currentBspOffset];
            var faceDataOffset = rom.FetchMachineOrder16(currentBspOffset + 1, bspData);
            var oppositeSkip = bspData[currentBspOffset + 3];
            currentBspOffset += 4;

            ProcessBSPElement(rom, bspData, currentBspOffset, ref faceOffset); // Process left child
            if (oppositeSkip != 0)  // Don't process right child if skip is 0
            {
                ProcessBSPElement(rom, bspData, currentBspOffset + oppositeSkip - 1, ref faceOffset); // Process right child
            }
            faceOffset.Add(currentBspOffset + faceDataOffset - 2);

        }
        else if (type == 0x44) // Leaf node
        {
            var faceDataOffset = rom.FetchMachineOrder16(currentBspOffset, bspData);
            currentBspOffset += 2;

            faceOffset.Add(currentBspOffset + faceDataOffset - 1);
        }
        else if (type == 0x40) // Empty node
        {
            // No more elements to process, we can return
            return;
        }
        else
        {
            throw new InvalidDataException($"Unexpected BSP element type: {type}");
        }
    }

    private (List<Triangle> tris,List<Line> lines,List<Point> points) ProcessFaceOrSpriteData(IMemoryAccess rom, ReadOnlySpan<byte> faceData, Vector3F[] vertices)
    {
        if (faceData[0] == 0x50)
        {
            var center = vertices[faceData[1]];
            var color = faceData[2];
            var size = faceData[3];
            if (faceData[4] != 0x00)
            {
                throw new InvalidDataException($"Unexpected sprite data code: 0x{faceData[4]:X2}");
            }

            var a = colorIndex / (8 * 8 * 8);
            var b = (colorIndex / (8 * 8)) % 8;
            var c = (colorIndex / 8) % 8;

            // Build a simple triangle for the sprite
            var triangleA = new Triangle
            {
                Vertex1 = center + new Vector3F(size, size, 0),
                Vertex2 = center + new Vector3F(-size, size, 0),
                Vertex3 = center + new Vector3F(-size, -size, 0),
                Color = new Color4B((byte)(16 + 32 * a), (byte)(16 + 32 * b), (byte)(16 + 32 * c), 255) // Default color
            };
            var triangleB = new Triangle
            {
                Vertex1 = center + new Vector3F(size, size, 0),
                Vertex2 = center + new Vector3F(-size, -size, 0),
                Vertex3 = center + new Vector3F(size, -size, 0),
                Color = new Color4B((byte)(16 + 32 * a), (byte)(16 + 32 * b), (byte)(16 + 32 * c), 255) // Default color
            };
            colorIndex++;

            return (new List<Triangle>([triangleA, triangleB]), new List<Line>(), new List<Point>());
        }
        else if (faceData[0] == 0x14)
        {
            return ProcessFaceData(rom, faceData, vertices);
        }
        else
        {
            throw new InvalidDataException($"Unexpected face data code: 0x{faceData[0]:X2}");
        }
    }

    private (List<Triangle> tris, List<Line> lines, List<Point> points) ProcessFaceData(IMemoryAccess rom, ReadOnlySpan<byte> faceData, Vector3F[] vertices)
    {
        var triangles = new List<Triangle>();
        var lines = new List<Line>();
        var points = new List<Point>();

        var startOfGroup = faceData[0];
        faceData = faceData[1..];
        if (startOfGroup != 0x14) // Expected start of face group
        {
            throw new InvalidDataException($"Expected start of face group code 0x14, got 0x{startOfGroup:X2}");
        }

        while (faceData.Length > 0)
        {
            var numSides = faceData[0];
            faceData = faceData[1..];

            if (numSides == 0 || numSides > 8)
            {
                break; // End of face group
            }

            var verts = new int[numSides];

            var faceCode = faceData[0];
            var faceColour = faceData[1];
            var faceNormalX = (sbyte)faceData[2];
            var faceNormalY = (sbyte)faceData[3];
            var faceNormalZ = (sbyte)faceData[4];

            for (int i = 0; i < numSides; i++)
            {
                verts[i] = faceData[5 + i];
            }

            faceData = faceData[(5 + numSides)..];

            if (numSides >= 3)  // TODO line and point data
            {
                // Trianglise

                for (int i = 0; i < numSides - 2; i++)
                {
                    var p0 = verts[0];
                    var p1 = verts[(i + 1) % numSides];
                    var p2 = verts[(i + 2) % numSides];

                    var a = colorIndex / (8 * 8 * 8);
                    var b = (colorIndex / (8 * 8)) % 8;
                    var c = (colorIndex / 8) % 8;
                    colorIndex++;

                    var triangle = new Triangle
                    {
                        Vertex1 = vertices[p0],
                        Vertex2 = vertices[p1],
                        Vertex3 = vertices[p2],
                        Color = new Color4B((byte)(16 + 32 * a), (byte)(16 + 32 * b), (byte)(16 + 32 * c), 255) // Default color
                    };
                    triangles.Add(triangle);
                }
            }
            else if (numSides == 2)
            {
                // Just a line
                var p0 = verts[0];
                var p1 = verts[1];

                var a = colorIndex / (8 * 8 * 8);
                var b = (colorIndex / (8 * 8)) % 8;
                var c = (colorIndex / 8) % 8;
                colorIndex++;

                var line = new Line
                {
                    Vertex1 = vertices[p0],
                    Vertex2 = vertices[p1],
                    Color = new Color4B((byte)(16 + 32 * a), (byte)(16 + 32 * b), (byte)(16 + 32 * c), 255) // Default color
                };
                lines.Add(line);
            }
            else if (numSides == 1)
            {
                // Just a point
                var p0 = verts[0];

                var a = colorIndex / (8 * 8 * 8);
                var b = (colorIndex / (8 * 8)) % 8;
                var c = (colorIndex / 8) % 8;
                colorIndex++;

                var point = new Point
                {
                    Position = vertices[p0],
                    Color = new Color4B((byte)(16 + 32 * a), (byte)(16 + 32 * b), (byte)(16 + 32 * c), 255) // Default color
                };
                points.Add(point);
            }
        }

        return (triangles, lines, points);
    }

    IWidgetRanged temp_modelSelect;
    IWidgetLabel temp_modelLabel;
    IWidgetLabel temp_modelName;

    public readonly string[] NameTable = new string[]    {

    };

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddRenderWidget(this);
        temp_modelName = widget.AddLabel("");
        temp_modelSelect = widget.AddSlider("Model", modelNumber, 0, 248, () => { reload = true; });
        widget.AddLabel(ModelHeader);
        temp_modelLabel = widget.AddLabel(ModelName);
    }

    public void OnClose()
    {
    }
}
