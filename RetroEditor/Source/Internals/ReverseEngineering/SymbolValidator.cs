using System.Collections.Generic;
using System.Linq;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.ReverseEngineering;

/// <summary>
/// Provides validation for symbol and label names.
/// </summary>
internal static class SymbolValidator
{
    /// <summary>
    /// Checks if a name is valid for a symbol or label.
    /// Valid names must:
    /// - Not be empty or whitespace
    /// - Start with a letter (a-z, A-Z) or underscore (_)
    /// - Contain only letters, digits, and underscores
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>null if valid, or an error message if invalid.</returns>
    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name cannot be empty.";
        }

        // Check first character
        char firstChar = name[0];
        if (!char.IsLetter(firstChar) && firstChar != '_')
        {
            return "Name must start with a letter or underscore.";
        }

        // Check remaining characters
        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return $"Invalid character '{c}' at position {i + 1}. Only letters, digits, and underscores are allowed.";
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a symbol name conflicts with existing symbols across all regions.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="parsers">Dictionary of all ROM data parsers by region.</param>
    /// <param name="excludeAddress">Optional address to exclude from conflict checking (when renaming existing symbol).</param>
    /// <param name="excludeSize">Optional size to exclude from conflict checking (when renaming existing symbol).</param>
    /// <returns>null if no conflict, or an error message if conflict found.</returns>
    public static string? CheckSymbolConflict(string name, Dictionary<MemoryRegionKey, RomDataParser> parsers, ulong? excludeAddress = null, int? excludeSize = null)
    {
        foreach (var kvp in parsers)
        {
            var parser = kvp.Value;
            var symbols = parser.SymbolProvider.GetAllSymbols(kvp.Key);

            foreach (var symbol in symbols)
            {
                // Skip the symbol we're renaming
                if (excludeAddress.HasValue && excludeSize.HasValue &&
                    symbol.Address == excludeAddress.Value && symbol.Size == excludeSize.Value)
                {
                    continue;
                }

                if (symbol.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return $"Symbol name '{name}' already exists at address 0x{symbol.Address:X} in region '{kvp.Key}'.";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a label name conflicts with existing labels across all regions.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="parsers">Dictionary of all ROM data parsers by region.</param>
    /// <param name="excludeAddress">Optional address to exclude from conflict checking (when renaming existing label).</param>
    /// <returns>null if no conflict, or an error message if conflict found.</returns>
    public static string? CheckLabelConflict(string name, Dictionary<MemoryRegionKey, RomDataParser> parsers, ulong? excludeAddress = null)
    {
        foreach (var kvp in parsers)
        {
            var parser = kvp.Value;
            var labels = parser.LabelProvider.GetAllLabels(kvp.Key);

            foreach (var label in labels)
            {
                // Skip the label we're renaming
                if (excludeAddress.HasValue && label.Address == excludeAddress.Value)
                {
                    continue;
                }

                if (label.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return $"Label name '{name}' already exists at address 0x{label.Address:X} in region '{kvp.Key}'.";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a name conflicts with both symbols and labels.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="parsers">Dictionary of all ROM data parsers by region.</param>
    /// <param name="excludeAddress">Optional address to exclude from conflict checking.</param>
    /// <param name="excludeSize">Optional size to exclude from symbol conflict checking.</param>
    /// <returns>null if no conflict, or an error message if conflict found.</returns>
    public static string? CheckAnyConflict(string name, Dictionary<MemoryRegionKey, RomDataParser> parsers, ulong? excludeAddress = null, int? excludeSize = null)
    {
        var symbolConflict = CheckSymbolConflict(name, parsers, excludeAddress, excludeSize);
        if (symbolConflict != null)
        {
            return symbolConflict;
        }

        var labelConflict = CheckLabelConflict(name, parsers, excludeAddress);
        if (labelConflict != null)
        {
            return labelConflict;
        }

        return null;
    }
}
