
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

    public Vector3F CameraPosition => new Vector3F(0, 30, -100);
    public Vector3F CameraLookAt => new Vector3F(0, 0, 0);
    public Vector3F CameraUp => new Vector3F(0, 1, 0);
    public float CameraFovY => 45.0f;
    public bool CameraOrthographic => false;
    public Triangle[] Triangles
    {
        get
        {
            if (reload)
            {
                reload = false;
                modelNumber = temp_modelSelect.Value;
                LoadModel(_rom);
            }
            return _triangles;
        }
    }

    Triangle [] _triangles = new Triangle[0];

    private IEditor _editorInterface;
    private IMemoryAccess _rom;
    private bool reload = false;
    public StarFoxTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;
        _rom = rom;

        LoadModel(_rom);
    }

    private void LoadModel(IMemoryAccess rom)
    {
        var modelIdTable = rom.ReadBytes(ReadKind.Rom, 0x264B, 250 * 2);
        var modelEntry = rom.FetchMachineOrder16(modelNumber*2, modelIdTable);
        var modelOffset = modelEntry - 0x8000;

        var objectEntry = rom.ReadBytes(ReadKind.Rom, (uint)modelOffset, 0x1C);

        var vertOffset = rom.FetchMachineOrder16(0, objectEntry);
        var vertBank = objectEntry[2];

        var vertDataOffset = (uint)(vertBank * 0x8000 + (vertOffset - 0x8000));

        // Experiment, extract things we need
        var startOf3dDataForARWing = rom.ReadBytes(ReadKind.Rom, vertDataOffset/*0x8F173*/, 512);

        var allVertices = ProcessVertexData(rom, ref startOf3dDataForARWing);

        var triangleCode = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (triangleCode != 0x30)
        {
            throw new InvalidDataException($"Expected triangle code 0x30, got 0x{triangleCode:X2}");
        }
        var numTris = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        var triangles = new int[3 * numTris];
        for (int a = 0; a < numTris; a++)
        {
            var p0 = startOf3dDataForARWing[0];
            var p1 = startOf3dDataForARWing[1];
            var p2 = startOf3dDataForARWing[2];
            startOf3dDataForARWing = startOf3dDataForARWing[3..];

            triangles[a * 3 + 0] = p0;
            triangles[a * 3 + 1] = p1;
            triangles[a * 3 + 2] = p2;
        }

        var faceOffsets = ProcessBSP(rom, startOf3dDataForARWing);

        var triangleList = new List<Triangle>();

        foreach (var faceDataOffset in faceOffsets)
        {
            triangleList.AddRange(ProcessFaceData(rom, startOf3dDataForARWing.Slice(faceDataOffset), allVertices));
        }

        _triangles = triangleList.ToArray();
    }

    public Vector3F[] ProcessVertexData(IMemoryAccess rom, ref ReadOnlySpan<byte> vertexData)
    {
        var vertices = new List<Vector3F>();

        var vertexCode = vertexData[0];
        vertexData = vertexData[1..];

        while (vertexCode != 0x0C)
        {
            if (vertexCode != 0x04 && vertexCode != 0x38)
            {
                throw new InvalidDataException($"Expected vertex code 0x04 or 0x38, got 0x{vertexCode:X2}");
            }

            var numVertices = vertexData[0];
            vertexData = vertexData[1..];

            for (int a = 0; a < numVertices; a++)
            {
                var x = (sbyte)vertexData[0];
                var y = (sbyte)vertexData[1];
                var z = (sbyte)vertexData[2];
                vertexData = vertexData[3..];
                vertices.Add(new Vector3F(x, y, z));
                if (vertexCode == 0x38) // Mirror X-Axis
                {
                    vertices.Add(new Vector3F(-x, y, z)); // not clear if consecutive, or follow later
                }
            }
            vertexCode = vertexData[0];
            vertexData = vertexData[1..];
        }

        return vertices.ToArray();
    }

    public int[] ProcessBSP(IMemoryAccess rom, ReadOnlySpan<byte> bspData)
    {
        var faceOffset = new List<int>();
        var currentBspOffset = 0;
        // BSP data
        var bspCode = bspData[currentBspOffset];
        currentBspOffset++;

        if (bspCode == 0x14)
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
            ProcessBSPElement(rom, bspData, currentBspOffset + oppositeSkip - 1, ref faceOffset); // Process right child

            faceOffset.Add(currentBspOffset + faceDataOffset - 2);

        }
        else if (type == 0x44) // Leaf node
        {
            var faceDataOffset = rom.FetchMachineOrder16(currentBspOffset, bspData);
            currentBspOffset += 2;

            faceOffset.Add(currentBspOffset + faceDataOffset - 1);
        }
        else
        {
            throw new InvalidDataException($"Unexpected BSP element type: {type}");
        }
    }

    int colorIndex = 1;
    int modelNumber = 2;

    private List<Triangle> ProcessFaceData(IMemoryAccess rom, ReadOnlySpan<byte> faceData, Vector3F[] vertices)
    {
        var triangles = new List<Triangle>();

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
            if (numSides < 3)
            {
                throw new InvalidDataException($"Expected 3 sides for a triangle, got {numSides}");
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

            faceData = faceData[(5+numSides)..];

            // Trianglise

            for (int i = 0; i < numSides-2; i++)
            {
                var p0 = verts[0];
                var p1 = verts[(i + 1) % numSides];
                var p2 = verts[(i + 2) % numSides];

                var a = colorIndex / (8 * 8 * 8);
                var b = (colorIndex / (8 * 8)) % 8;
                var c = (colorIndex / 8) % 8;

                var triangle = new Triangle
                {
                    Vertex1 = vertices[p0],
                    Vertex2 = vertices[p1],
                    Vertex3 = vertices[p2],
                    Color = new Color4B((byte)(16 + 32 * a), (byte)(16 + 32 * b), (byte)(16 + 32 * c), 255) // Default color
                };
                triangles.Add(triangle);

                colorIndex++;
            }
        }

        return triangles;
    }

    IWidgetRanged temp_modelSelect;

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddRenderWidget(this);
        temp_modelSelect = widget.AddSlider("Model", 2, 1, 250, () => { reload = true; });
    }

    public void OnClose()
    {
    }
}
