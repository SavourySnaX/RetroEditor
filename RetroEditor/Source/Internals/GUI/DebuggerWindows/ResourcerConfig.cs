using System.Numerics;
using MyMGui;

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
        private ImCol _UnknownColor;
        private ImCol _CodeColor;
        private ImCol _DataColor;
        private ImCol _StringColor;
        private ImCol _LabelColor;
        private ImCol _CommentColor;

    public ResourcerConfig()
    {
        _UnknownColor = ImGui.ColorConvert(new Vector4(0, 0, 0, .5f));
        _CodeColor = ImGui.ColorConvert(new Vector4(0, 0, 1, .5f));
        _DataColor = ImGui.ColorConvert(new Vector4(0, 1, 0, .5f));
        _StringColor = ImGui.ColorConvert(new Vector4(1, 0, 0, .5f));
        _LabelColor = ImGui.ColorConvert(new Vector4(0, 0, 0.5f, .5f));
        _CommentColor = ImGui.ColorConvert(new Vector4(0.5f, 0.5f, 0.5f, .5f));
    }

        public ImCol GetColorU32(ConfigColour kind)
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
