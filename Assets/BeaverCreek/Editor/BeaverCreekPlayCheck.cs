using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace BeaverCreek.Editor
{
    [InitializeOnLoad]
    public static class BeaverCreekPlayCheck
    {
        [Serializable] class RootState { public string scene; public int index; public bool active; }
        [Serializable] class SavedState
        {
            public string activeScene,holdingPath;
            public List<RootState> roots=new List<RootState>();
            public List<string> reopen=new List<string>();
        }
        const string Key="BeaverCreek.PlayCheck";
        static readonly HashSet<string> behaviors=new HashSet<string>();
        static readonly HashSet<int> shots=new HashSet<int>();
        static readonly HashSet<string> wingPoses=new HashSet<string>();
        static readonly List<string> errors=new List<string>();
        static Keyboard keyboard;
        static Vector3 startPosition, movedPosition;
        static bool started, pressed, released;
        static bool wideCaptured,duckCaptured;
        static int maxParticles;
        static BeaverCreekPlayCheck()
        {
            EditorApplication.playModeStateChanged+=Changed;
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=Log;
        }
        static void Log(string text,string stack,LogType type)
        {
            if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)) errors.Add(text);
        }
        public static void Start(int firstStage=0)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before validation.");
            var saved=new SavedState {activeScene=SceneManager.GetActiveScene().path};
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(!scene.path.StartsWith(BeaverCreekBuilder.Root+"/Scenes/")) continue;
                if(scene.isDirty) throw new Exception("Save Beaver Creek scene changes before running Play-mode checks.");
                saved.reopen.Add(scene.path);
            }
            if(saved.reopen.Count>0)
            {
                var holding=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                saved.holdingPath=AssetDatabase.GenerateUniqueAssetPath(BeaverCreekBuilder.Root+"/Generated/PlayModeWorkspace.unity");
                EditorSceneManager.SaveScene(holding,saved.holdingPath);
                foreach(string path in saved.reopen) EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(path),true);
            }
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);var roots=scene.GetRootGameObjects();
                for(int j=0;j<roots.Length;j++)
                {
                    saved.roots.Add(new RootState {scene=scene.path,index=j,active=roots[j].activeSelf});
                    roots[j].SetActive(false);
                }
            }
            SessionState.SetString(Key+".saved",JsonUtility.ToJson(saved));SessionState.SetInt(Key+".stage",firstStage);SessionState.SetBool(Key,true);
            File.WriteAllText(BeaverCreekBuilder.Root+"/Validation/PlayModeValidation.txt","Play-mode checks started "+DateTime.Now+"\n");
            Open(firstStage);
        }
        static void Open(int stage)
        {
            string name=stage==0?"Cutscene":"FirstPerson";
            var scene=EditorSceneManager.OpenScene(BeaverCreekBuilder.Root+"/Scenes/BeaverCreek_"+name+".unity",OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                started=false;pressed=false;released=false;wideCaptured=false;duckCaptured=false;maxParticles=0;behaviors.Clear();shots.Clear();wingPoses.Clear();errors.Clear();
                Application.runInBackground=true;
                var gameView=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Focus();
            }
            if(state!=PlayModeStateChange.EnteredEditMode) return;
            int stage=SessionState.GetInt(Key+".stage",0);
            var scene=SceneManager.GetSceneByPath(BeaverCreekBuilder.Root+"/Scenes/BeaverCreek_"+(stage==0?"Cutscene":"FirstPerson")+".unity");
            if(scene.IsValid()) EditorSceneManager.CloseScene(scene,true);
            if(stage==0) {SessionState.SetInt(Key+".stage",1);EditorApplication.delayCall+=()=>Open(1);return;}
            var saved=JsonUtility.FromJson<SavedState>(SessionState.GetString(Key+".saved",""));
            foreach(var item in saved.roots)
            {
                var source=SceneManager.GetSceneByPath(item.scene);
                if(source.IsValid()) {var roots=source.GetRootGameObjects();if(item.index<roots.Length) roots[item.index].SetActive(item.active);}
            }
            foreach(string path in saved.reopen) EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var active=SceneManager.GetSceneByPath(saved.activeScene);if(active.IsValid()) SceneManager.SetActiveScene(active);
            if(!string.IsNullOrEmpty(saved.holdingPath))
            {
                var holding=SceneManager.GetSceneByPath(saved.holdingPath);if(holding.IsValid()) EditorSceneManager.CloseScene(holding,true);
                AssetDatabase.DeleteAsset(saved.holdingPath);
            }
            SessionState.SetBool(Key,false);
            File.AppendAllText(BeaverCreekBuilder.Root+"/Validation/PlayModeValidation.txt","Completed "+DateTime.Now+"; restored previously open scene roots.\n");
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused) return;
            int stage=SessionState.GetInt(Key+".stage",0);
            var scene=SceneManager.GetActiveScene();var roots=scene.GetRootGameObjects();
            var ducks=roots.SelectMany(r=>r.GetComponentsInChildren<CreekDuck>()).ToArray();
            if(ducks.Length==0) return;
            if(!started)
            {
                started=true;
                if(stage==1)
                {
                    keyboard=InputSystem.AddDevice<Keyboard>("BeaverCreek validation keyboard");
                    startPosition=roots.SelectMany(r=>r.GetComponentsInChildren<CreekFirstPerson>()).First().transform.position;
                }
            }
            foreach(var duck in ducks)
            {
                if(duck.CurrentBehavior!=null) behaviors.Add(duck.CurrentBehavior);
                if(duck.CurrentBehavior=="Flap")
                {
                    var wing=duck.animator.transform.Find("Duck_Rig/Wing 1.L");
                    if(wing) wingPoses.Add(wing.localRotation.eulerAngles.ToString("F1"));
                }
            }
            float elapsed=Time.timeSinceLevelLoad;
            var weather=roots.SelectMany(r=>r.GetComponentsInChildren<CreekWeather>()).FirstOrDefault();
            if(weather) maxParticles=Mathf.Max(maxParticles,weather.LiveParticles);
            if(stage==0)
            {
                var director=roots.SelectMany(r=>r.GetComponentsInChildren<CreekCinematic>()).First();shots.Add(director.ShotIndex);
                if(elapsed>7&&!wideCaptured) {wideCaptured=true;BeaverCreekBuilder.Capture(director.view,BeaverCreekBuilder.Root+"/Validation/Runtime_AfterRain.png");}
                if(elapsed>22&&!duckCaptured) {duckCaptured=true;BeaverCreekBuilder.Capture(director.view,BeaverCreekBuilder.Root+"/Validation/Runtime_Ducks.png");}
                if(elapsed<64) return;
                File.AppendAllText(BeaverCreekBuilder.Root+"/Validation/PlayModeValidation.txt",
                    "Cutscene: 64 seconds; shots visited="+string.Join(",",shots.OrderBy(x=>x))+"; behaviors="+string.Join(",",behaviors)+"; distinct animated flap wing poses="+wingPoses.Count+"; ducks="+ducks.Length+"; peak live weather particles="+maxParticles+"; runtime errors="+errors.Count+"\n");
                if(shots.Count!=4||behaviors.Count<5||wingPoses.Count<10) errors.Add("Cinematic/animation coverage did not meet expected checks.");
                if(!weather||maxParticles<10||maxParticles>420) errors.Add("Weather particle bounds or playback failed.");
            }
            else
            {
                var player=roots.SelectMany(r=>r.GetComponentsInChildren<CreekFirstPerson>()).First();
                Cursor.lockState=CursorLockMode.Locked;
                if(elapsed>2&&!pressed) {InputSystem.QueueStateEvent(keyboard,new KeyboardState(KeyCodeToInput()));pressed=true;}
                if(elapsed>5&&!released) {InputSystem.QueueStateEvent(keyboard,new KeyboardState());movedPosition=player.transform.position;released=true;}
                if(elapsed<11) return;
                float distance=Vector3.Distance(startPosition,movedPosition);
                bool grounded=player.GetComponent<CharacterController>().isGrounded;
                File.AppendAllText(BeaverCreekBuilder.Root+"/Validation/PlayModeValidation.txt","FirstPerson: input-driven displacement="+distance.ToString("F2")+"m; grounded="+grounded+"; final y="+player.transform.position.y.ToString("F2")+"; runtime errors="+errors.Count+"\n");
                if(distance<1||!grounded) errors.Add("First person movement/grounding failed.");
                InputSystem.RemoveDevice(keyboard);keyboard=null;
            }
            File.AppendAllText(BeaverCreekBuilder.Root+"/Validation/PlayModeValidation.txt",errors.Count==0?"PASS\n":"FAIL: "+string.Join("\n",errors)+"\n");
            EditorApplication.ExitPlaymode();
        }
        static UnityEngine.InputSystem.Key KeyCodeToInput() => UnityEngine.InputSystem.Key.W;
    }
}
