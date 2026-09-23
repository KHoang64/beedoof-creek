using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeaverCreek.Editor
{
    public static class BeaverCreekWebBuild
    {
        const string Root = "Assets/BeaverCreek";
        static readonly string[] Scenes = { Root + "/Scenes/BeaverCreek_Menu.unity", Root + "/Scenes/BeaverCreek_FirstPerson.unity", Root + "/Scenes/BeaverCreek_Cutscene.unity" };
        [MenuItem("Tools/Beaver Creek/Prepare menu and web controls")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before preparing scenes.");
            var original = SceneManager.GetActiveScene();
            foreach (var path in Scenes)
            {
                var loaded = SceneManager.GetSceneByPath(path);
                if (loaded.IsValid() && loaded.isDirty) throw new InvalidOperationException("Scene has unsaved changes: " + path);
            }
            foreach (var path in Scenes)
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool alreadyOpen = scene.IsValid();
                bool newScene = !File.Exists(path);
                if (!alreadyOpen) scene = newScene ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive) : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                try
                {
                    var host = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CreekExperience>(true)).FirstOrDefault();
                    if (!host) host = new GameObject("Creek experience").AddComponent<CreekExperience>();
                    host.isMenu = path == Scenes[0];
                    host.player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CreekFirstPerson>(true)).FirstOrDefault();
                    host.cinematic = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CreekCinematic>(true)).FirstOrDefault();
                    if (host.isMenu)
                    {
                        host.backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Validation/FirstPerson.png");
                        if (newScene)
                        {
                            var camera = new GameObject("Menu camera").AddComponent<Camera>();
                            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.03f, .09f, .06f); camera.cullingMask = 0;
                            camera.gameObject.AddComponent<AudioListener>();
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, path);
                }
                finally { if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid()) SceneManager.SetActiveScene(original);
            var configured = EditorBuildSettings.scenes.Where(s => s.path != Scenes[0]).ToList();
            configured.Insert(0, new EditorBuildSettingsScene(Scenes[0], true)); EditorBuildSettings.scenes = configured.ToArray();
            PlayerSettings.WebGL.template = "PROJECT:BeaverCreek";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultWebScreenWidth = 720; PlayerSettings.defaultWebScreenHeight = 1280;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/BeaverCreekWebPrepare.txt", "Prepared menu and scene controls; three-scene WebGL export configured. " + DateTime.Now);
        }
        public static void SwitchWeb() { EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL); }
        public static void OptimizeWebTextures()
        {
            var changed = new System.Collections.Generic.List<string>();
            foreach (var path in AssetDatabase.GetDependencies(Scenes, true))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer || importer.textureShape != TextureImporterShape.Texture2D) continue;
                var settings = importer.GetPlatformTextureSettings("WebGL");
                if (settings.overridden && settings.maxTextureSize <= 1024) continue;
                if (!settings.overridden && importer.maxTextureSize <= 1024) continue;
                string backup = "Backups/WebTextureImport/" + path + ".meta";
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup)) File.Copy(path + ".meta", backup);
                settings.name = "WebGL"; settings.overridden = true; settings.maxTextureSize = 1024;
                settings.format = TextureImporterFormat.Automatic;
                settings.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(settings); importer.SaveAndReimport(); changed.Add(path);
            }
            Directory.CreateDirectory("Docs/QA/WebExperience");
            File.WriteAllText("Docs/QA/WebExperience/WebTextureOverrides.txt", "WebGL-only maximum texture size: 1024. Original .meta files are in Backups/WebTextureImport.\n" + string.Join("\n", changed));
        }
        [MenuItem("Tools/Beaver Creek/Build WebGL for Vercel")]
        public static void BuildWeb()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) throw new InvalidOperationException("Switch to WebGL first.");
            OptimizeWebTextures();
            Directory.CreateDirectory("Builds/BeaverCreekWeb");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes, locationPathName = "Builds/BeaverCreekWeb", target = BuildTarget.WebGL, options = BuildOptions.None
            });
            File.WriteAllText("Temp/BeaverCreekWebBuild.txt", report.summary.result + "\nSize: " + report.summary.totalSize + "\nDuration: " + report.summary.totalTime + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("WebGL build failed; see build report and Editor log.");
            File.Copy("WebSupport/vercel.json", "Builds/BeaverCreekWeb/vercel.json", true);
        }
    }
}
