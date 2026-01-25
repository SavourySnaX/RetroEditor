using System.Globalization;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

/// <summary>
/// ZX Spectrum/Z80 trace parser
/// </summary>
internal class ZXSpectrumTraceParser : ITraceParser
{
    public bool TryParseTraceLine(string line, out TraceEntry entry)
    {
        entry = new TraceEntry { IsValid = false };

        if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
            return false;

        var parts = line.Split('|');
        // Expect AF, BC, DE, HL, IX, IY, SP, PC
        if (parts.Length < 8)
            return false;

        bool ok =
            parts[0].StartsWith("AF=") &&
            parts[1].StartsWith("BC=") &&
            parts[2].StartsWith("DE=") &&
            parts[3].StartsWith("HL=") &&
            parts[4].StartsWith("IX=") &&
            parts[5].StartsWith("IY=") &&
            parts[6].StartsWith("SP=") &&
            parts[7].StartsWith("PC=");
        if (!ok) return false;

        if (!ushort.TryParse(parts[7].Substring(3), NumberStyles.HexNumber, null, out ushort pc))
            return false;

        var cpuState = new Z80State { InterruptMode = false };
        var regState = new Z80RegisterState();

        entry = new TraceEntry
        {
            Address = pc,
            CpuState = cpuState,
            RegisterState = regState,
            IsValid = true
        };

        return true;
    }
}
