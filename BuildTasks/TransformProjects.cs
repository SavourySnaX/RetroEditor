using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class TransformProject : Task
{
    [Required]
    public string InputProject { get; set; }

    [Required]
    public string OutputProject { get; set; }

    [Required]
    public string TargetFramework { get; set; }

    public override bool Execute()
    {
        System.Diagnostics.Debugger.Launch();

        var doc = XDocument.Load(InputProject);

        // Remove all ItemGroups
        foreach (var ig in doc.Root.Elements("ItemGroup").ToList())
            ig.Remove();

        // Remove all PropertyGroups
        foreach (var pg in doc.Root.Elements("PropertyGroup").ToList())
            pg.Remove();

        // Add clean PropertyGroup
        var newPG = new XElement("PropertyGroup",
            new XElement("TargetFramework", TargetFramework));
        doc.Root.Add(newPG);

        // Load original again to extract the wanted ItemGroup
        var original = XDocument.Load(InputProject);

        var wantedIG = original
            .Root
            .Elements("ItemGroup")
            .FirstOrDefault(x =>
                (string)x.Attribute("Condition") == "'$(RetroEditorBuilding)' != 'Yes'");

        if (wantedIG != null)
        {
            var cleanIG = new XElement("ItemGroup",
                wantedIG.Elements());
            doc.Root.Add(cleanIG);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutputProject));
        doc.Save(OutputProject);

        return true;
    }
}
