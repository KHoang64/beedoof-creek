using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeaverCreek.Editor
{
    [InitializeOnLoad]
    public static class BeaverCreekWebCheck
    {
        [Serializable] class Setup { public SceneSetup[] scenes; }
        const string Key = "BeaverCreek.WebCheck";
        static string Output => "Docs/QA/WebExperience" + (SessionState.GetBool(Key + ".portrait", false) ? "/Portrait" : "");
        static int stage;
        static double since, started;
        static Vector3 startPosition, pausePosition;
        static Quaternion startRotation;
        static CreekTouchPad move, look;
        static PointerEventData moveEvent, lookEvent;
        static readonly List<string> errors = new List<string>();
        static BeaverCreekWebCheck()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Changed;
            Application.logMessageReceived += (message, trace, type) =>
            { if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) errors.Add(message); };
        }
        public static void Start(bool portrait = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before checks.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save scene changes before runtime checks.");
            SessionState.SetString(Key + ".setup", JsonUtility.ToJson(new Setup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + ".portrait", portrait);
            if (portrait) SetPortrait();
            Directory.CreateDirectory(Output); File.WriteAllText(Output + "/Runtime.txt", "Web experience runtime check " + DateTime.Now + "\n");
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/BeaverCreek/Scenes/BeaverCreek_Menu.unity");
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                stage = 0; since = started = EditorApplication.timeSinceStartup; errors.Clear();
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false);
                var setup = JsonUtility.FromJson<Setup>(SessionState.GetString(Key + ".setup", ""));
                EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
                if (SessionState.GetBool(Key + ".portrait", false))
                {
                    var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
                    gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView, SessionState.GetInt(Key + ".size", 0));
                }
                File.AppendAllText(Output + "/Runtime.txt", "Restored original scene setup.\n");
            }
        }
        static void Check(bool condition, string description)
        { File.AppendAllText(Output + "/Runtime.txt", (condition ? "PASS " : "FAIL ") + description + "\n"); if (!condition) errors.Add(description); }
        static void Click(string name)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name);
            button.onClick.Invoke();
        }
        static void Next() { stage++; since = EditorApplication.timeSinceStartup; }
        static void Capture(string name) { ScreenCapture.CaptureScreenshot(Output + "/" + name + ".png"); }
        static void SetPortrait()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            var groupType = group.GetType();
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var fixedSize = Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
            var size = Activator.CreateInstance(sizeType, fixedSize, 390, 844, "Creek portrait QA");
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            var selection = view.GetType().GetProperty("selectedSizeIndex");
            SessionState.SetInt(Key + ".size", (int)selection.GetValue(view));
            selection.SetValue(view, index);
            view.Focus();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (now - started > 120) throw new Exception("Runtime check timed out at stage " + stage);
                var host = UnityEngine.Object.FindFirstObjectByType<CreekExperience>();
                if (!host || now - since < 1.5) return;
                if (stage == 0)
                {
                    Check(host.isMenu, "Menu is the web startup scene"); Capture("Menu"); Next();
                }
                else if (stage == 1) { Click("Explore"); Next(); }
                else if (stage == 2 && host.player)
                {
                    Check(SceneManager.GetActiveScene().name == CreekExperience.FirstPersonScene, "Explore button loads first-person scene");
                    var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    File.AppendAllText(Output + "/Runtime.txt", "Portrait scene canvases=" + canvases.Length + "; roots=" + string.Join(",", canvases.Select(c => c.name + ":" + c.isActiveAndEnabled + ":" + c.renderMode)) + "; pads=" + host.GetComponentsInChildren<CreekTouchPad>(true).Length + "\n");
                    if (!host.player.UseTouchControls) Click("Touch toggle");
                    var pads = host.GetComponentsInChildren<CreekTouchPad>(); move = pads.First(p => !p.look); look = pads.First(p => p.look);
                    startPosition = host.player.transform.position; startRotation = host.player.transform.rotation;
                    var moveRect = (RectTransform)move.transform;
                    var lookRect = (RectTransform)look.transform;
                    moveEvent = new PointerEventData(EventSystem.current) { pointerId = 11, position = RectTransformUtility.WorldToScreenPoint(null, moveRect.position) };
                    move.OnPointerDown(moveEvent); moveEvent.position += new Vector2(0, 42); move.OnDrag(moveEvent);
                    lookEvent = new PointerEventData(EventSystem.current) { pointerId = 12, position = RectTransformUtility.WorldToScreenPoint(null, lookRect.position) };
                    look.OnPointerDown(lookEvent); lookEvent.position += new Vector2(90, 20); look.OnDrag(lookEvent);
                    Next();
                }
                else if (stage == 3)
                {
                    Check(Vector3.Distance(startPosition, host.player.transform.position) > .5f, "Touch joystick moves CharacterController without keyboard/mouse lock");
                    Check(Quaternion.Angle(startRotation, host.player.transform.rotation) > 1, "Second pointer rotates view while first pointer moves");
                    var foreign = new PointerEventData(EventSystem.current) { pointerId = 99, position = Vector2.zero };
                    move.OnPointerUp(foreign); Check(host.player.TouchMove.magnitude > .5f, "An unrelated finger cannot release the active joystick");
                    move.OnPointerUp(moveEvent); look.OnPointerUp(lookEvent);
                    Check(host.player.TouchMove == Vector2.zero, "Joystick release clears movement");
                    Check(host.player.GetComponent<CharacterController>().isGrounded, "Player remains grounded after touch movement");
                    Capture("TouchControls"); Next();
                }
                else if (stage == 4)
                {
                    Click("Menu"); Check(!host.player.InputEnabled && host.player.TouchMove == Vector2.zero, "Menu blocks gameplay and clears touch input");
                    Click("Watch"); Next();
                }
                else if (stage == 5 && host.cinematic)
                {
                    Check(SceneManager.GetActiveScene().name == CreekExperience.CutsceneScene, "Watch button loads cutscene");
                    Click("Pause"); Check(host.cinematic.IsPaused, "Pause button pauses the cutscene"); pausePosition = host.cinematic.view.transform.position; Next();
                }
                else if (stage == 6)
                {
                    Check(Vector3.Distance(pausePosition, host.cinematic.view.transform.position) < .001f, "Paused camera remains stationary");
                    Click("Replay"); Check(!host.cinematic.IsPaused && host.cinematic.ShotIndex == 0, "Replay returns to the first shot and resumes"); Capture("CutsceneControls"); Next();
                }
                else if (stage == 7) { Click("Menu"); Click("Explore"); Next(); }
                else if (stage == 8 && host.player)
                {
                    Check(host.player.TouchMove == Vector2.zero && host.player.InputEnabled, "Repeated mode switch starts with clean input");
                    Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Mode switches keep exactly one EventSystem");
                    File.AppendAllText(Output + "/Runtime.txt", errors.Count == 0 ? "PASS: all runtime checks; zero runtime errors.\n" : "FAIL: " + string.Join("\n", errors) + "\n");
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception e) { File.AppendAllText(Output + "/Runtime.txt", "FAIL " + e + "\n"); EditorApplication.ExitPlaymode(); }
        }
    }
}
