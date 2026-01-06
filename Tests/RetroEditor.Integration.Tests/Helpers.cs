using System.Runtime.CompilerServices;

namespace RetroEditor.Integration.Tests;

static class Helpers
{
    public static string GenerateTemporarySnesRom(string basePath = "")
    {
        var tempDirectory = System.IO.Path.GetTempPath();
        if (basePath != "")
        {
            tempDirectory = System.IO.Path.Combine(tempDirectory, basePath);
            System.IO.Directory.CreateDirectory(tempDirectory);
        }

        var tempFile = System.IO.Path.Combine(tempDirectory, System.IO.Path.GetRandomFileName());
        var romData = new byte[0x8000];

        System.Text.Encoding.ASCII.GetBytes("RE").CopyTo(romData, 0x7FB0); // Maker Code
        System.Text.Encoding.ASCII.GetBytes("TEST").CopyTo(romData, 0x7FB2); // Game Title
        Array.Fill<byte>(romData, 0x00, 0x7FB6, 6); // Fixed Value
        romData[0x7FBD] = 0x00; // Expansion RAM Size (0 = none)
        romData[0x7FBE] = 0x00; // Special Version
        romData[0x7FBF] = 0x00; // Cartridge Type

        System.Text.Encoding.ASCII.GetBytes("    TEST      ROM    ").CopyTo(romData, 0x7FC0);
        romData[0x7FD5] = 0x20; // Map Mode (LoROM)
        romData[0x7FD6] = 0x00; // Cartridge Type (ROM only)
        romData[0x7FD7] = 0x01; // Rom Size (32KB)
        romData[0x7FD8] = 0x00; // Ram Size (none)
        romData[0x7FD9] = 0x02; // Country (All Europe)
        romData[0x7FDA] = 0x33; // Fixed Value (Extended Header)
        romData[0x7FDB] = 0x00; // Mask Rom Version
        romData[0x7FDC] = 0xFF; // Complement Checksum (to be filled later)
        romData[0x7FDD] = 0xFF; // Complement Checksum (to be filled later)
        romData[0x7FDE] = 0x00; // Checksum (to be filled later)
        romData[0x7FDF] = 0x00; // Checksum (to be filled later)

        // Compute checksum and re-record

        UInt16 checksum = 0;
        for (uint a = 0; a < romData.Length; a++)
        {
            checksum += romData[a];
        }
        // Write complement and actual checksum values to rom
        var complement = new byte[2];
        var check = new byte[2];
        romData[0x7FDC] = (byte)(~(checksum & 0xFF));
        romData[0x7FDD] = (byte)(~((checksum >> 8) & 0xFF));
        romData[0x7FDE] = (byte)(checksum & 0xFF);
        romData[0x7FDF] = (byte)((checksum >> 8) & 0xFF);

        romData[0x7FFC] = 0x00; // Reset Vector Low
        romData[0x7FFD] = 0x80; // Reset Vector High

        romData[0x00] = 0x80; // BRA
        romData[0x01] = 0xFE; // NOP

        System.IO.File.WriteAllBytes(tempFile, romData);
        return tempFile;
    }
}