using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu: Build → Web + Windows (zipped). Builds both releases through their Build Profiles
// (so each uses its own settings, e.g. Web compression), then zips them ready for itch.
// Web goes first and Windows last, so the editor ends up back on Windows for normal work.
public static class BuildAll
{
    const string WebProfile = "Assets/Settings/Build Profiles/Web - Desktop - Release.asset";
    const string WindowsProfile = "Assets/Settings/Build Profiles/Windows.asset";
    const string Root = "Builds";   // next to Assets/ (keep it in .gitignore)

    [MenuItem("Build/Web + Windows (zipped)")]
    static void BuildBoth()
    {
        if (BuildWeb() && BuildWindows())
        {
            Debug.Log($"Both builds done → {Path.GetFullPath(Root)}");
            EditorUtility.RevealInFinder(Path.GetFullPath(Root));
        }
    }

    [MenuItem("Build/Web only")]
    static void WebOnly() => BuildWeb();

    [MenuItem("Build/Windows only")]
    static void WindowsOnly() => BuildWindows();

    static bool BuildWeb()
    {
        string folder = Path.Combine(Root, "Web");
        if (!Build(WebProfile, folder)) return false;
        // zip the CONTENTS, so index.html sits at the root of the zip (what itch needs)
        Zip(folder, Path.Combine(Root, $"{FileName}_Web.zip"));
        return true;
    }

    static bool BuildWindows()
    {
        string folder = Path.Combine(Root, "Windows");
        if (!Build(WindowsProfile, Path.Combine(folder, FileName + ".exe"))) return false;
        Zip(folder, Path.Combine(Root, $"{FileName}_Windows.zip"));
        return true;
    }

    // The product name as a safe file name: "Wrench It: The AIpocalypse" -> "Wrench It The AIpocalypse"
    // (Windows doesn't allow : \ / * ? " < > | in file names; the in-game name keeps them)
    static string FileName
    {
        get
        {
            string name = PlayerSettings.productName;
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "");
            while (name.Contains("  ")) name = name.Replace("  ", " ");
            return name.Trim();
        }
    }

    static bool Build(string profilePath, string location)
    {
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
        if (profile == null) { Debug.LogError($"Build profile not found: {profilePath}"); return false; }

        // start clean so files from an older build can't end up in the zip
        string folder = Path.HasExtension(location) ? Path.GetDirectoryName(location) : location;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,
            locationPathName = location,
            options = BuildOptions.None
        });

        bool ok = report.summary.result == BuildResult.Succeeded;
        if (ok) Debug.Log($"Built {profile.name}: {report.summary.totalSize / (1024f * 1024f):0.0} MB in {report.summary.totalTime.TotalSeconds:0}s");
        else Debug.LogError($"Build failed: {profile.name} ({report.summary.result}). See the console above.");
        return ok;
    }

    // Zips a folder's contents, skipping Unity's debug-only folders that players don't need
    static void Zip(string folder, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        string full = Path.GetFullPath(folder);
        using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (string file in Directory.GetFiles(full, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(full.Length + 1).Replace('\\', '/');
                if (relative.Contains("_BurstDebugInformation_DoNotShip") || relative.Contains("_BackUpThisFolder_ButDontShipItWithYourGame"))
                    continue;
                zip.CreateEntryFromFile(file, relative, System.IO.Compression.CompressionLevel.Optimal);
            }
        }
        Debug.Log($"Zipped → {zipPath}");
    }
}
