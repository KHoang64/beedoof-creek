using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeaverCreek.Editor
{
    // Local, explicit command mailbox for repeatable Editor-side validation.
    [InitializeOnLoad]
    public static class BeaverCreekBridge
    {
        const string Request = "Temp/BeaverCreek.request";
        static BeaverCreekBridge() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (command == "inventory") Inventory();
                else if (command == "WebPrepare") BeaverCreekWebBuild.Prepare();
                else if (command == "WebSwitch") BeaverCreekWebBuild.SwitchWeb();
                else if (command == "WebBuild") BeaverCreekWebBuild.BuildWeb();
                else if (command == "WebSmoke") BeaverCreekWebCheck.Start();
                else if (command == "WebPortraitSmoke") BeaverCreekWebCheck.Start(true);
                else
                {
                    var type = Type.GetType("BeaverCreek.Editor.BeaverCreekBuilder, Assembly-CSharp-Editor");
                    if(type == null) throw new InvalidOperationException("Builder has not compiled yet. Retry after import.");
                    var method = type.GetMethod(command);
                    if(method == null) throw new InvalidOperationException("Unknown command: " + command);
                    method.Invoke(null, null);
                }
                File.WriteAllText("Temp/BeaverCreek.result", "OK " + command + " " + DateTime.Now);
            }
            catch (Exception e) { File.WriteAllText("Temp/BeaverCreek.result", e.ToString()); Debug.LogException(e); }
        }
        static void Inventory()
        {
            var report = new StringBuilder();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                string[] roots = { "Assets/BK/PureNature_FantasyForest/Prefabs", "Assets/SoStylized/Environment/Trees/Pine/Prefabs", "Assets/Patchmesh/Country Critters - Stylized Hand-Painted Ducks/Prefabs" };
                foreach (string path in AssetDatabase.FindAssets("t:Prefab", roots).Select(AssetDatabase.GUIDToAssetPath))
                {
                    if (path.Contains("Mushroom") || path.Contains("Ivies") || path.Contains("Snow")) continue;
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
                    var renderers = obj.GetComponentsInChildren<Renderer>();
                    Bounds bounds = new Bounds(); bool first = true;
                    foreach(var r in renderers) { if(first) { bounds=r.bounds;first=false; } else bounds.Encapsulate(r.bounds); }
                    report.AppendLine(path + " | bounds=" + bounds + " | materials=" + string.Join(",",renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>m.name+":"+m.shader.name)));
                    UnityEngine.Object.DestroyImmediate(obj);
                }
                File.WriteAllText("Temp/BeaverCreekInventory.txt",report.ToString());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
