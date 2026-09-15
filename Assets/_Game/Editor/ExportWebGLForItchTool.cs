#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CookingGame.Editor
{
    public static class ExportWebGLForItchTool
    {
        private const string DefaultBuildOutputDir = "Build/WebGL_Output";
        private const string OutputZipPath = "Build/WebGL_itch.zip";

        [MenuItem("Tools/Itch.io WebGL/1-CLICK TU DONG BUILD VA NEN ZIP CHO ITCH.IO", false, -100)]
        public static void BuildAndPackageForItch()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Loi", "Vui long dung Play Mode truoc khi Build!", "OK");
                return;
            }

            ConfigureWebGLPlayerSettings();

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputFolder = Path.Combine(projectRoot, DefaultBuildOutputDir);

            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }
            Directory.CreateDirectory(outputFolder);

            var scenes = EditorBuildSettings.scenes;
            var enabledScenePaths = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
            {
                if (s.enabled) enabledScenePaths.Add(s.path);
            }

            if (enabledScenePaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Loi", "Khong co Scene nao duoc bat trong Build Settings!", "OK");
                return;
            }

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = enabledScenePaths.ToArray(),
                locationPathName = outputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[ItchWebGL] Dang tien hanh Build WebGL ra thu muc: {outputFolder}...");
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build That Bai", $"Qua trinh build gap loi: {summary.result}. Vui long kiem tra Console!", "OK");
                return;
            }

            string zipFullPath = Path.Combine(projectRoot, OutputZipPath);
            PackageDirectoryToItchZip(outputFolder, zipFullPath);

            EditorUtility.DisplayDialog("Hoan Tat!",
                $"Da Build va Nen xong file WebGL cho Itch.io thanh cong!\n\nFile Zip: {OutputZipPath}\nDung luong: {new FileInfo(zipFullPath).Length / 1024 / 1024:0.0} MB\n\nBan chi can len itch.io va upload file nay la choi duoc ngay 100%!", "Tuyet voi");

            EditorUtility.RevealInFinder(zipFullPath);
        }

        [MenuItem("Tools/Itch.io WebGL/2. Nen Thu Muc Build Hien Tai Thanh Zip Cho Itch.io", false, 10)]
        public static void PackageExistingBuild()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            
            string[] possibleDirs = new[]
            {
                Path.Combine(projectRoot, "Build/Build"),
                Path.Combine(projectRoot, "Build/WebGL_Output"),
                Path.Combine(projectRoot, "Build/WebGL")
            };

            string sourceDir = null;
            foreach (var dir in possibleDirs)
            {
                if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "index.html")))
                {
                    sourceDir = dir;
                    break;
                }
            }

            if (sourceDir == null)
            {
                sourceDir = EditorUtility.OpenFolderPanel("Chon thu muc WebGL Build (chua index.html)", Path.Combine(projectRoot, "Build"), "");
                if (string.IsNullOrEmpty(sourceDir) || !File.Exists(Path.Combine(sourceDir, "index.html")))
                {
                    EditorUtility.DisplayDialog("Loi", "Thu muc da chon khong chua file index.html!", "OK");
                    return;
                }
            }

            FixFolderStructureIfMessy(sourceDir);

            string zipFullPath = Path.Combine(projectRoot, OutputZipPath);
            PackageDirectoryToItchZip(sourceDir, zipFullPath);

            EditorUtility.DisplayDialog("Da Nen Thanh Cong!",
                $"Da dong goi thu muc {Path.GetFileName(sourceDir)} thanh file Zip chuan cho itch.io!\n\nFile: {OutputZipPath}\n\nFile da duoc mo san trong Explorer.", "OK");

            EditorUtility.RevealInFinder(zipFullPath);
        }

        private static void ConfigureWebGLPlayerSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        }

        private static void FixFolderStructureIfMessy(string rootDir)
        {
            string templateData = Path.Combine(rootDir, "TemplateData");
            string buildDir = Path.Combine(rootDir, "Build");

            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            if (Directory.Exists(templateData))
            {
                foreach (var file in Directory.GetFiles(templateData, "Build.*"))
                {
                    string target = Path.Combine(buildDir, Path.GetFileName(file));
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(file, target);
                    Debug.Log($"[ItchWebGL] Di chuyen {Path.GetFileName(file)} tu TemplateData sang Build/");
                }
            }
        }

        private static void PackageDirectoryToItchZip(string sourceDir, string zipPath)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                int baseLen = sourceDir.Length + 1;

                foreach (var filePath in files)
                {
                    string relativePath = filePath.Substring(baseLen).Replace('\\', '/');
                    zip.CreateEntryFromFile(filePath, relativePath, System.IO.Compression.CompressionLevel.Optimal);
                }
            }

            Debug.Log($"[ItchWebGL] Da nen thanh cong {zipPath} voi dinh dang Linux/itch.io tuong thich 100%!");
        }
    }
}
#endif