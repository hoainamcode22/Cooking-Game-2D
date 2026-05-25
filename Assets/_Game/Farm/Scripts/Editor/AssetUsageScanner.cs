using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AssetUsageScanner
{
    private static readonly string[] SearchRoots = { "Assets", "ProjectSettings" };
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".unity",
        ".prefab",
        ".asset",
        ".mat",
        ".controller",
        ".overridecontroller",
        ".anim",
        ".playable",
        ".spriteatlas",
        ".spriteatlasv2",
        ".cs",
        ".shader",
        ".shadergraph",
        ".vfx",
        ".json",
        ".asmdef",
        ".inputactions",
    };

    [MenuItem("FarmTools/Asset Usage/Scan Selected Assets", priority = 2000)]
    public static void ScanSelectedAssets()
    {
        var selectedPaths = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => !string.IsNullOrWhiteSpace(path) && path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("Asset Usage Scanner", "Select one or more assets/folders in the Project window first.", "OK");
            return;
        }

        var targets = ExpandAssetTargets(selectedPaths);
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Asset Usage Scanner", "No scanable assets found in the selection.", "OK");
            return;
        }

        var searchableFiles = GetSearchableFiles();
        var report = BuildReport(selectedPaths, targets, searchableFiles);
        var reportPath = WriteReport(report);

        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(reportPath);
        EditorUtility.DisplayDialog(
            "Asset Usage Scanner",
            $"Scanned {targets.Count} assets.\nReport saved to:\n{reportPath}\n\nDo not delete assets marked USED.",
            "OK");
    }

    [MenuItem("FarmTools/Asset Usage/Scan Selected Assets", true)]
    private static bool ValidateScanSelectedAssets()
    {
        return Selection.objects != null && Selection.objects.Length > 0;
    }

    private static List<AssetTarget> ExpandAssetTargets(IEnumerable<string> selectedPaths)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var selectedPath in selectedPaths)
        {
            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { selectedPath }))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsValidFolder(assetPath))
                        paths.Add(assetPath);
                }
            }
            else
            {
                paths.Add(selectedPath);
            }
        }

        return paths
            .Select(path => new AssetTarget(path, AssetDatabase.AssetPathToGUID(path)))
            .Where(target => !string.IsNullOrEmpty(target.Guid))
            .OrderBy(target => target.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetSearchableFiles()
    {
        var files = new List<string>();

        foreach (var root in SearchRoots)
        {
            if (!Directory.Exists(root))
                continue;

            files.AddRange(Directory
                .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Where(path => TextExtensions.Contains(Path.GetExtension(path))));
        }

        return files
            .Select(NormalizePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildReport(IReadOnlyCollection<string> selectedPaths, IReadOnlyCollection<AssetTarget> targets, IReadOnlyList<string> searchableFiles)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();

        sb.AppendLine("Asset Usage Scanner Report");
        sb.AppendLine($"Generated: {now}");
        sb.AppendLine();
        sb.AppendLine("Selected:");
        foreach (var path in selectedPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"- {path}");
        sb.AppendLine();
        sb.AppendLine("Rule:");
        sb.AppendLine("- USED means the asset GUID appears in another serialized file.");
        sb.AppendLine("- Unused here means no GUID reference was found in Assets/ProjectSettings text assets. Check Addressables/Resources/runtime loading manually before deleting.");
        sb.AppendLine();

        var usedCount = 0;
        var unusedCount = 0;

        foreach (var target in targets)
        {
            var references = FindGuidReferences(target, searchableFiles);
            if (references.Count > 0)
                usedCount++;
            else
                unusedCount++;

            sb.AppendLine(references.Count > 0 ? "[USED]" : "[unused]");
            sb.AppendLine($"Asset: {target.Path}");
            sb.AppendLine($"GUID:  {target.Guid}");
            sb.AppendLine($"Refs:  {references.Count}");

            foreach (var reference in references)
                sb.AppendLine($"  - {reference}");

            sb.AppendLine();
        }

        sb.Insert(0, $"Summary: {usedCount} used, {unusedCount} unused, {targets.Count} total{Environment.NewLine}{Environment.NewLine}");
        return sb.ToString();
    }

    private static List<string> FindGuidReferences(AssetTarget target, IReadOnlyList<string> searchableFiles)
    {
        var references = new List<string>();
        var targetPath = NormalizePath(target.Path);

        foreach (var file in searchableFiles)
        {
            if (string.Equals(file, targetPath, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var text = File.ReadAllText(file);
                if (text.IndexOf(target.Guid, StringComparison.OrdinalIgnoreCase) >= 0)
                    references.Add(file);
            }
            catch (Exception ex)
            {
                references.Add($"{file} (scan failed: {ex.GetType().Name})");
            }
        }

        return references;
    }

    private static string WriteReport(string report)
    {
        const string reportDir = "Assets/_Game/Reports";
        Directory.CreateDirectory(reportDir);

        var reportPath = $"{reportDir}/AssetUsageReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        File.WriteAllText(reportPath, report, Encoding.UTF8);
        return Path.GetFullPath(reportPath);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private readonly struct AssetTarget
    {
        public readonly string Path;
        public readonly string Guid;

        public AssetTarget(string path, string guid)
        {
            Path = NormalizePath(path);
            Guid = guid;
        }
    }
}
