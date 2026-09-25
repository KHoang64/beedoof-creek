using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeaverCreek.Editor
{
    // Explicit WebGL copies avoid conditional pragma limitations and preserve native materials.
    public sealed class BeaverCreekWebShaders : IProcessSceneWithReport
    {
        const string Source = "Assets/BK/Pure_Common/Shaders";
        const string Output = "Assets/BeaverCreek/Generated/WebShaders";
        static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        public int callbackOrder => 0;

        public static void Prepare(string[] scenes)
        {
            shaders.Clear();
            materials.Clear();
            Directory.CreateDirectory(Output);
            foreach (var path in Directory.GetFiles(Source, "*.shader"))
            {
                var text = File.ReadAllText(path);
                var name = Regex.Match(text, "Shader\\s+\"([^\"]+)\"").Groups[1].Value;
                if (string.IsNullOrEmpty(name)) throw new BuildFailedException("Missing shader name: " + path);
                text = text.Replace("Shader \"" + name + "\"", "Shader \"BeaverCreek/Web/" + name + "\"");
                text = text.Replace("#pragma target 4.5", "#pragma target 3.5");
                // Remove stage declarations as well as the feature defines. Unity does not
                // reliably condition stage pragmas on graphics-API preprocessor macros.
                text = Regex.Replace(text, @"(?m)^\s*#(?:define ASE_(?:TESSELLATION\s+1|DISTANCE_TESSELLATION)|pragma (?:require tessellation tessHW|hull HullFunction|domain DomainFunction))\s*$", "");
                var output = Output + "/" + Path.GetFileName(path);
                if (!File.Exists(output) || File.ReadAllText(output) != text) File.WriteAllText(output, text);
                AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(output);
                if (!shader) throw new BuildFailedException("Failed to import web shader: " + output);
                shaders.Add(name, shader);
            }
            foreach (var path in AssetDatabase.GetDependencies(scenes, true))
            foreach (var original in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                if (!original.shader || !shaders.TryGetValue(original.shader.name, out var shader)) continue;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string guid, out long localId))
                    throw new BuildFailedException("Cannot identify material: " + original.name);
                var pathOut = Output + "/" + guid + "_" + localId + ".mat";
                var copy = AssetDatabase.LoadAssetAtPath<Material>(pathOut);
                if (!copy)
                {
                    copy = new Material(original);
                    AssetDatabase.CreateAsset(copy, pathOut);
                }
                else EditorUtility.CopySerialized(original, copy);
                copy.shader = shader;
                EditorUtility.SetDirty(copy);
                materials.Add(original, copy);
            }
            AssetDatabase.SaveAssets();
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(ForWeb).ToArray();
                foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                    if (terrain.materialTemplate) terrain.materialTemplate = ForWeb(terrain.materialTemplate);
            }
        }

        static Material ForWeb(Material original)
        {
            if (!original || !original.shader || !original.shader.name.StartsWith("BK/") && original.shader.name != "Custom/Clouds") return original;
            if (materials.TryGetValue(original, out var replacement)) return replacement;
            throw new BuildFailedException("Missing prepared WebGL material: " + original.name + ". Use Build WebGL for Vercel.");
        }
    }
}
