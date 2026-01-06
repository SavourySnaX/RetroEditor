using RetroEditor.Plugins;
using Xunit.Abstractions;

namespace RetroEditor.Integration.Tests;

public class CustomMameDebugger
{
    public ITestOutputHelper TestOutputHelper { get; }
    public CustomMameDebugger(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    internal class MameSnesWrapper
    {
        internal string testRom = string.Empty;
        internal string testDirectory = string.Empty;
        internal string romsDirectory = string.Empty;
        public void CreateResources()
        {
            testRom = Helpers.GenerateTemporarySnesRom("snes");
            testDirectory = System.IO.Path.GetDirectoryName(testRom) ?? "";
            romsDirectory = System.IO.Path.Combine(testDirectory!, "mame", "roms", "snes");
            Directory.CreateDirectory(romsDirectory);
            File.WriteAllBytes(System.IO.Path.Combine(romsDirectory, "spc700.rom"), new byte[64]);
        }

        public void CleanupResources()
        {
            if (!string.IsNullOrEmpty(testDirectory))
            {
                System.IO.Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public void EnsureSpinsUp()
    {
        bool notDone = true;
        Thread mameThread = null!;
        MameSnesWrapper wrapper = new MameSnesWrapper();
        wrapper.CreateResources();
        var settings = new Editor.EditorSettings();
        settings.MameDebuggerDataFolder = wrapper.testDirectory;
        LibRetroPlugin? retro = null;
        LibMameDebugger? mameInstance = null;
        try
        {
            var editor = new Editor(settings, new Helpers.TestLogger(TestOutputHelper));
            retro = editor.GetDeveloperMame();
            Assert.NotNull(retro);
            Assert.Equal(1u, retro!.Version());
            mameInstance = new LibMameDebugger(retro!);
            Assert.NotNull(mameInstance);
            retro.Init();
            retro!.LoadGame(wrapper.testRom);
            retro!.GetSystemAVInfo();

            // Spin up a thread (because mame debugger blocks)
            mameThread = new Thread(() =>
            {
                while (notDone)
                {
                    retro!.Run();
                }
            });
            mameThread.Start();
            // Wait max of 5 seconds for debugger to be ready
            int waitMs = 0;
            while (notDone && !mameInstance.DebuggerViewReady && waitMs < 5000)
            {
                Thread.Sleep(100);
                waitMs += 100;
            }
            Assert.True(mameInstance.DebuggerViewReady,"Debugger failed to become ready in time.");
        }
        finally
        {
            notDone = false;
            mameInstance?.SendCommand("go");  // Debugger starts paused, ensure we re-lease it before we attempt to close
            mameThread?.Join(1000);
            retro?.Close();
            wrapper.CleanupResources();
            if (mameThread != null)
            {
                Assert.False(mameThread.IsAlive);
            }
        }

    }
}
