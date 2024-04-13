
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

public class PluginBuilder
{
    private string _assemblyName;
    private AssemblyLoadContext _loadContext;
    private List<MetadataReference> _references;
    private List<string> _globalUsings;
    private MemoryStream _ms;

    public PluginBuilder(string assemblyName)
    {
        _assemblyName = assemblyName;
        _loadContext = new AssemblyLoadContext(assemblyName, true);
        _references = new List<MetadataReference>();
        _globalUsings = new List<string>();
        _ms = new MemoryStream();
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

    public void AddGlobalUsing(string usingNamespace)
    {
        _globalUsings.Add($"global using global::{usingNamespace}; ");
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
                    var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).WithFilePath(file);
                    syntaxTrees.Add(syntaxTree);
                }
            }
        }

        var globalUsing = string.Join("", _globalUsings);
        var globalUsings = CSharpSyntaxTree.ParseText(globalUsing).WithFilePath("globalUsings.cs");
        syntaxTrees.Add(globalUsings);

        var compilation = CSharpCompilation.Create(_assemblyName,
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _ms.Seek(0, SeekOrigin.Begin);
        return compilation.Emit(_ms);
    }

    public Assembly LoadInMemoryPlugin()
    {
        _ms.Seek(0, SeekOrigin.Begin);
        return _loadContext.LoadFromStream(_ms);
    }

    public void Unload()
    {
        _loadContext.Unload();
        _loadContext = new AssemblyLoadContext(_assemblyName, true);
    }
}
