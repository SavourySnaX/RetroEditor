
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

public struct Vector3F
{
    public float x, y, z;
}

public class StarFoxTesting : IUserWindow
{
    public float UpdateInterval => 1 / 30.0f;

    private IEditor _editorInterface;
    public StarFoxTesting(IEditor editorInterface, IMemoryAccess rom)
    {
        _editorInterface = editorInterface;

        // Experiment, extract things we need
        var startOf3dDataForARWing = rom.ReadBytes(ReadKind.Rom, 0x8F173, 256);

        var vertexCode = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (vertexCode == 0x04)
        {
            var numVertices = startOf3dDataForARWing[0];
            startOf3dDataForARWing = startOf3dDataForARWing[1..];

            var vertices = new Vector3F[numVertices];
            for (int a = 0; a < numVertices; a++)
            {
                var x = (sbyte)startOf3dDataForARWing[0];
                var y = (sbyte)startOf3dDataForARWing[1];
                var z = (sbyte)startOf3dDataForARWing[2];
                startOf3dDataForARWing = startOf3dDataForARWing[3..];
                vertices[a].x = x;
                vertices[a].y = y;
                vertices[a].z = z;
            }
        }

        vertexCode = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (vertexCode == 0x38) // Mirror X-Axis
        {
            var numVertices = startOf3dDataForARWing[0];
            startOf3dDataForARWing = startOf3dDataForARWing[1..];

            var vertices = new Vector3F[2 * numVertices];
            for (int a = 0; a < numVertices; a++)
            {
                var x = (sbyte)startOf3dDataForARWing[0];
                var y = (sbyte)startOf3dDataForARWing[1];
                var z = (sbyte)startOf3dDataForARWing[2];
                startOf3dDataForARWing = startOf3dDataForARWing[3..];
                vertices[a * 2 + 0].x = x;
                vertices[a * 2 + 0].y = y;
                vertices[a * 2 + 0].z = z;
                vertices[a * 2 + 1].x = -x; // not clear if consecutive, or follow later
                vertices[a * 2 + 1].y = y;
                vertices[a * 2 + 1].z = z;
            }

        }

        vertexCode = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (vertexCode == 0x0C) // End of list
        {

        }

        var triangleCode = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (triangleCode == 0x30)
        {
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
        }

        // BSP data
        var bspCode = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (bspCode == 0x3C) // BSP TREE
        {
            bspCode = startOf3dDataForARWing[0];
            startOf3dDataForARWing = startOf3dDataForARWing[1..];

            if (bspCode == 0x28) // BSP node
            {
                var faceSplittingTriangle = startOf3dDataForARWing[0];
                var faceDataOffset = rom.FetchMachineOrder16(1, startOf3dDataForARWing);
                var oppositeSkip = startOf3dDataForARWing[3];

                startOf3dDataForARWing = startOf3dDataForARWing[(2+faceDataOffset)..];
            }

        }

        var startFace = startOf3dDataForARWing[0];
        startOf3dDataForARWing = startOf3dDataForARWing[1..];

        if (startFace == 0x14)
        {
            var numSides = startOf3dDataForARWing[0];
            var faceID = startOf3dDataForARWing[1];
            var colourTextureNum = startOf3dDataForARWing[2];
            var normalX = (sbyte)startOf3dDataForARWing[3];
            var normalY = (sbyte)startOf3dDataForARWing[4];
            var normalZ = (sbyte)startOf3dDataForARWing[5];

            startOf3dDataForARWing = startOf3dDataForARWing[6..];

            var faceVerts = new int[numSides];

            for (int a = 0; a < numSides; a++)
            {
                var vtx = startOf3dDataForARWing[0];
                startOf3dDataForARWing = startOf3dDataForARWing[1..];

                faceVerts[a] = vtx;
            }

        }

    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        //        throw new System.NotImplementedException();
    }

    public void OnClose()
    {
        //        throw new System.NotImplementedException();
    }
}
