using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

Console.OutputEncoding = System.Text.Encoding.UTF8;


// Testing

string assemblyName = "RomPlugin_Testing";


var editorReference = Path.Combine(System.AppContext.BaseDirectory,"RetroEditor.dll");
var referenceAssembliesRoot = Path.Combine(System.AppContext.BaseDirectory,"ReferenceAssemblies");

var nugetCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget/packages");

var referencesAsList = new List<MetadataReference>();
referencesAsList.Add(MetadataReference.CreateFromFile(editorReference));
foreach (var file in Directory.GetFiles(referenceAssembliesRoot, "*.dll"))
{
    referencesAsList.Add(MetadataReference.CreateFromFile(file));   // Exploitable, but do i care?
}

var references=referencesAsList.ToArray();
//var asmPath= Path.GetDirectoryName(typeof(object).Assembly.Location);

// Add all sources in romplugins to syntax tree
List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
foreach (var file in Directory.GetFiles("Plugins/RomPlugins", "*.cs"))
{
    var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).WithFilePath(file);
    syntaxTrees.Add(syntaxTree);
}

var globalUsings = CSharpSyntaxTree.ParseText("global using global::System; ").WithFilePath("globalUsings.cs");
syntaxTrees.Add(globalUsings);

var compilation = CSharpCompilation.Create(assemblyName, 
    syntaxTrees: syntaxTrees,
    references: references, 
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

//compilation = compilation.AddReferences(new[] { MetadataReference.CreateFromFile(Path.Combine(asmPath, "System.Runtime.dll")) });

using var ms = new MemoryStream();
var result = compilation.Emit(ms);
if (!result.Success)
{
    Console.WriteLine("Compilation failed!");
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic);
    }
    return;
}

ms.Seek(0, SeekOrigin.Begin);

// Create a rom plugin context
var pluginContext = new AssemblyLoadContext("RomPluginContext", true);

var assembly = pluginContext.LoadFromStream(ms);

var romPlugins = new List<object>();
foreach (var type in assembly.GetTypes())
{
    if (type.GetInterface("IRomPlugin") != null)
    {
        var plugin = (IRomPlugin)Activator.CreateInstance(type);
        if (plugin!=null)
        {
            romPlugins.Add(plugin);
        }
    }
}

var forEditorRomPlugins = new IRomPlugin[romPlugins.Count];
for (int i = 0; i < romPlugins.Count; i++)
{
    forEditorRomPlugins[i] = (IRomPlugin)Activator.CreateInstance(romPlugins[i].GetType());
}

var plugins = new IRetroPlugin[] { new JetSetWilly48(), new Rollercoaster(), new Fairlight(), new PhantasyStar2(), new Metroid() };

var render = new Editor(plugins, forEditorRomPlugins);
render.RenderRun();

pluginContext.Unload();