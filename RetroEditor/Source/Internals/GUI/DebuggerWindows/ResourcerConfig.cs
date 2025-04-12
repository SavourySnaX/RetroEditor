using System.Numerics;
using ImGuiNET;

internal class ResourcerConfig
    {
        internal enum ConfigColour
        {
            Code,
            Data,
            String,
            Label,
            Comment,
            Unknown
        }
        private uint _UnknownColor;
        private uint _CodeColor;
        private uint _DataColor;
        private uint _StringColor;
        private uint _LabelColor;
        private uint _CommentColor;

    public ResourcerConfig()
    {
        _UnknownColor = ImGui.GetColorU32(new Vector4(0, 0, 0, .5f));
        _CodeColor = ImGui.GetColorU32(new Vector4(0, 0, 1, .5f));
        _DataColor = ImGui.GetColorU32(new Vector4(0, 1, 0, .5f));
        _StringColor = ImGui.GetColorU32(new Vector4(1, 0, 0, .5f));
        _LabelColor = ImGui.GetColorU32(new Vector4(0, 0, 0.5f, .5f));
        _CommentColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, .5f));
    }

        public uint GetColorU32(ConfigColour kind)
        {
            return kind switch
            {
                ConfigColour.Unknown => _UnknownColor,
                ConfigColour.Code => _CodeColor,
                ConfigColour.Data => _DataColor,
                ConfigColour.String => _StringColor,
                ConfigColour.Label => _LabelColor,
                ConfigColour.Comment => _CommentColor,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
