using System.Diagnostics;
using System.Xml.Linq;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.macOS;

public class MacPlatformService : IPlatformService
{
    private FileStream? _lockFile;
    private const string LockFileName = "screentranslator.lock";
    private const string PlistName = "com.screentranslator.plist";

    public string GetSettingsDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Application Support", "ScreenTranslator");
    }

    public bool EnsureSingleInstance()
    {
        var lockPath = Path.Combine(GetSettingsDirectory(), LockFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);

        try
        {
            _lockFile = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public void ReleaseSingleInstance()
    {
        _lockFile?.Dispose();
        _lockFile = null;
    }

    public void SetAutoStart(bool enable)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var launchAgentsDir = Path.Combine(home, "Library", "LaunchAgents");
        var plistPath = Path.Combine(launchAgentsDir, PlistName);

        if (enable)
        {
            Directory.CreateDirectory(launchAgentsDir);
            var exePath = Environment.ProcessPath ?? "";
            var plist = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN",
                    "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
                new XElement("plist", new XAttribute("version", "1.0"),
                    new XElement("dict",
                        new XElement("key", "Label"),
                        new XElement("string", "com.screentranslator"),
                        new XElement("key", "ProgramArguments"),
                        new XElement("array", new XElement("string", exePath)),
                        new XElement("key", "RunAtLoad"),
                        new XElement("true")
                    )
                )
            );
            plist.Save(plistPath);
        }
        else
        {
            if (File.Exists(plistPath))
                File.Delete(plistPath);
        }
    }

    public void SetClickThrough(nint windowHandle, bool enable)
    {
        // macOS click-through is handled differently via NSWindow
        // For now this is a no-op; can be implemented via ObjC runtime interop
    }
}
