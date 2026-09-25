using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

namespace BeaverCreek.Editor
{
    public static class BeaverCreekPerformanceAudit
    {
        public static void Run()
        {
            var scenes = new[] { "Assets/BeaverCreek/Scenes/BeaverCreek_Menu.unity", "Assets/BeaverCreek/Scenes/BeaverCreek_FirstPerson.unity", "Assets/BeaverCreek/Scenes/BeaverCreek_Cutscene.unity" };
            var report = new StringBuilder("WebGL asset audit (imported texture allocation, not device GPU profiling)\n");
            long memory = 0;
            int count = 0;
            foreach (var path in AssetDatabase.GetDependencies(scenes, true))
            foreach (var texture in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture>())
            {
                long size = Profiler.GetRuntimeMemorySizeLong(texture);
                memory += size; count++;
                report.AppendLine($"Texture\t{texture.width}x{texture.height}\t{(texture is Texture2D t ? t.format.ToString() : texture.GetType().Name)}\t{size}\t{path}");
            }
            report.AppendLine($"Total textures: {count}; imported allocation bytes: {memory}");
            foreach (var path in scenes.Skip(1))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
                report.AppendLine($"Scene {path}: renderers including all LODs={renderers.Length}; material slots={renderers.Sum(r=>r.sharedMaterials.Length)}; LOD groups={roots.Sum(r=>r.GetComponentsInChildren<LODGroup>(true).Length)}");
                foreach (var group in roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.childCount>20))
                    report.AppendLine($"Group {group.name}: children={group.childCount}; renderers={group.GetComponentsInChildren<Renderer>(true).Length}");
            }
            Directory.CreateDirectory("Docs/QA/WebExperience");
            File.WriteAllText("Docs/QA/WebExperience/MobileBaseline.txt", report.ToString());
        }
    }
}
