
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

internal class PluginBuilder
{
    private string _assemblyName;
    private AssemblyLoadContext _loadContext;
    private List<MetadataReference> _references;
    private StreamSymbolPair? _lastGood;

    private StreamSymbolPair _streamSymbolPair;

    private struct StreamSymbolPair
    {
        public MemoryStream assembly;
        public MemoryStream symbols;
    }

    public PluginBuilder(string assemblyName)
    {
        _assemblyName = assemblyName;
        _loadContext = new AssemblyLoadContext(assemblyName, true);
        _references = new List<MetadataReference>();
        _streamSymbolPair = new StreamSymbolPair();
        _streamSymbolPair.assembly = new MemoryStream();
        _streamSymbolPair.symbols = new MemoryStream();
        _lastGood = null;
    }
    public void AddReference(string referencePath)
    {
        _references.Add(MetadataReference.CreateFromFile(referencePath));
    }

    public void AddReferences(string referencePath)
    {
        foreach (var file in Directory.GetFiles(referencePath, "*.dll"))
        {
            _references.Add(MetadataReference.CreateFromFile(file));
        }
    }

    public EmitResult BuildPlugin(string sourceRoot)
    {
        var references = _references.ToArray();

        // Add all sources in romplugins to syntax tree
        var sourcesList = new Queue<string>();
        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
        sourcesList.Enqueue(sourceRoot);
        while (sourcesList.Count > 0)
        {
            var current = sourcesList.Dequeue();
            foreach (var entry in Directory.GetDirectories(current))
            {
                sourcesList.Enqueue(entry);
            }
            foreach (var file in Directory.GetFiles(current))
            {
                if (file.EndsWith(".cs"))
                {
                    var sbuffer = Encoding.UTF8.GetBytes(File.ReadAllText(file));
                    var ssourceText = SourceText.From(sbuffer, sbuffer.Length, Encoding.UTF8, canBeEmbedded: true);
                    var syntaxTree = CSharpSyntaxTree.ParseText(ssourceText, path: file);
                    //var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).WithFilePath(file);
                    syntaxTrees.Add(syntaxTree);
                }
            }
        }

        var compilation = CSharpCompilation.Create(_assemblyName,
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _streamSymbolPair.assembly.Seek(0, SeekOrigin.Begin);
        _streamSymbolPair.symbols.Seek(0, SeekOrigin.Begin);

        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);

        var embeddedTexts = new List<EmbeddedText>();
        foreach (var syntaxTree in syntaxTrees)
        {
            embeddedTexts.Add(EmbeddedText.FromSource(syntaxTree.FilePath, syntaxTree.GetText()));
        }

        var result = compilation.Emit(peStream:_streamSymbolPair.assembly, pdbStream: _streamSymbolPair.symbols, embeddedTexts: embeddedTexts, options: emitOptions);
        if (result.Success)
        {
            _lastGood = _streamSymbolPair;
        }
        else
        {
            if (_lastGood != null)
            {
                _streamSymbolPair = _lastGood.Value;
            }
        }
        return result;
    }

    public Assembly? LoadInMemoryPlugin()
    {
        if (_lastGood == null)
        {
            return null;
        }
        _streamSymbolPair.assembly.Seek(0, SeekOrigin.Begin);
        _streamSymbolPair.symbols.Seek(0, SeekOrigin.Begin);
        return _loadContext.LoadFromStream(_streamSymbolPair.assembly, _streamSymbolPair.symbols);
    }

    public void Unload()
    {
        _loadContext.Unload();
        _loadContext = new AssemblyLoadContext(_assemblyName, true);
    }
}
