using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace BeaverCreek
{
    public sealed class CreekExperience : MonoBehaviour
    {
        public const string MenuScene = "BeaverCreek_Menu";
        public const string FirstPersonScene = "BeaverCreek_FirstPerson";
        public const string CutsceneScene = "BeaverCreek_Cutscene";
        public bool isMenu;
        public Texture2D backdrop;
        public CreekFirstPerson player;
        public CreekCinematic cinematic;
        static readonly Color Cream = new Color(.94f, .93f, .84f);
        static readonly Color Forest = new Color(.045f, .105f, .08f, .94f);
        static readonly Color Sage = new Color(.72f, .8f, .57f);
        Font font;
        CanvasScaler scaler;
        RectTransform safe, menuCard;
        GameObject menu, hud, touch, resume;
        Text playLabel, shotLabel, loadingLabel;
        bool menuOpen, loading;
        Rect previousSafe;
        Vector2 previousSize;
        Sprite disc;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                var camera = player ? player.view.GetComponent<Camera>() : cinematic ? cinematic.view : null;
                if (camera)
                {
                    var data = camera.GetUniversalAdditionalCameraData();
                    data.requiresDepthTexture = true;
                    data.requiresColorTexture = true;
                }
            }
            if (!EventSystem.current)
            {
                var events = new GameObject("Creek UI events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(events, gameObject.scene);
            }
            var canvasObject = new GameObject("Creek interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.matchWidthOrHeight = .5f;
            if (isMenu && backdrop)
            {
                var photo = new GameObject("Pond", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                photo.transform.SetParent(canvasObject.transform, false);
                photo.GetComponent<RawImage>().texture = backdrop;
                photo.GetComponent<RawImage>().raycastTarget = false;
                var fitter = photo.GetComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)backdrop.width / backdrop.height;
                Stretch(photo.GetComponent<RectTransform>());
                Panel("Shade", canvasObject.transform, new Color(.01f, .055f, .035f, .35f), false);
            }
            safe = Rect("Safe area", canvasObject.transform); Stretch(safe);
            hud = Rect("HUD", safe).gameObject; Stretch((RectTransform)hud.transform);
            if (player)
            {
                player.UseTouchControls = Application.isMobilePlatform || Touchscreen.current != null;
                CreateTouch();
                Button("Menu", hud.transform, "Menu", new Vector2(0, 1), new Vector2(76, -55), new Vector2(116, 84), () => ShowMenu(true));
                Button("Reset", hud.transform, "Reset", new Vector2(1, 1), new Vector2(-76, -55), new Vector2(116, 84), player.ResetPosition);
                Label("Mode", hud.transform, "EXPLORE THE CREEK", 18, new Vector2(.5f, 1), new Vector2(0, -54), new Vector2(310, 50));
                resume = Button("Resume look", hud.transform, "Click to explore", new Vector2(.5f, .5f), Vector2.zero, new Vector2(300, 76), player.ResumeLook).gameObject;
                Button("Touch toggle", hud.transform, "Touch controls", new Vector2(.5f, 0), new Vector2(0, 50), new Vector2(190, 80), () =>
                {
                    player.UseTouchControls = !player.UseTouchControls; player.ReleaseLook();
                    touch.SetActive(player.UseTouchControls);
                });
            }
            if (cinematic)
            {
                Button("Menu", hud.transform, "Menu", new Vector2(0, 1), new Vector2(76, -55), new Vector2(116, 84), () => ShowMenu(true));
                Label("Mode", hud.transform, "A QUIET AFTERNOON", 18, new Vector2(.5f, 1), new Vector2(35, -54), new Vector2(360, 50));
                var strip = Panel("Playback", hud.transform, Forest, false);
                strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(1, 0); strip.pivot = new Vector2(.5f, 0);
                strip.sizeDelta = new Vector2(0, 162); strip.anchoredPosition = Vector2.zero;
                shotLabel = Label("Shot", strip, "", 19, new Vector2(.5f, 1), new Vector2(0, -32), new Vector2(620, 40));
                playLabel = Button("Pause", strip, "Pause", new Vector2(.5f, 0), new Vector2(-90, 60), new Vector2(150, 84), cinematic.TogglePause).GetComponentInChildren<Text>();
                Button("Replay", strip, "Replay", new Vector2(.5f, 0), new Vector2(90, 60), new Vector2(150, 84), cinematic.Restart);
            }
            CreateMenu();
            ShowMenu(isMenu);
            UpdateLayout();
            Application.targetFrameRate = 60;
        }
        void CreateMenu()
        {
            menu = Panel("Menu overlay", safe, isMenu ? new Color(0, 0, 0, .08f) : new Color(.015f, .04f, .03f, .78f), true).gameObject;
            menuCard = Panel("Menu card", menu.transform, Forest, false);
            Place(menuCard, new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 620));
            Label("Eyebrow", menuCard, "FIELD NOTES   /   BEAVER POND", 17, new Vector2(.5f, 1), new Vector2(0, -52), new Vector2(550, 35), Sage);
            Label("Title", menuCard, "Beaver Creek", 58, new Vector2(.5f, 1), new Vector2(0, -119), new Vector2(560, 85));
            Label("Description", menuCard, "A little rain. A quiet pond.\nTake a moment in the woods.", 23, new Vector2(.5f, 1), new Vector2(0, -207), new Vector2(535, 88));
            Button("Explore", menuCard, "Explore in first person   →", new Vector2(.5f, 1), new Vector2(0, -308), new Vector2(508, 82), () => LoadMode(FirstPersonScene), true);
            Button("Watch", menuCard, "Watch the cutscene   →", new Vector2(.5f, 1), new Vector2(0, -407), new Vector2(508, 82), () => LoadMode(CutsceneScene));
            if (!isMenu)
                Button("Back", menuCard, "Return to experience", new Vector2(.5f, 1), new Vector2(0, -497), new Vector2(508, 65), () => ShowMenu(false));
            else
                Label("Note", menuCard, "52-second film  ·  Walk at your own pace", 18, new Vector2(.5f, 1), new Vector2(0, -505), new Vector2(530, 48), Sage);
            Label("Controls", menuCard, "DESKTOP  ·  WASD + mouse\nMOBILE  ·  Move left, look right", 17, new Vector2(.5f, 0), new Vector2(0, 46), new Vector2(535, 65));
            loadingLabel = Label("Loading", menu.transform, "", 24, new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 100));
            loadingLabel.gameObject.SetActive(false);
        }
        void CreateTouch()
        {
            touch = Rect("Touch controls", hud.transform).gameObject; Stretch((RectTransform)touch.transform);
            var look = Panel("Look pad", touch.transform, Color.clear, true);
            look.anchorMin = new Vector2(.46f, .16f); look.anchorMax = new Vector2(1, .87f); look.offsetMin = look.offsetMax = Vector2.zero;
            var lookPad = look.gameObject.AddComponent<CreekTouchPad>(); lookPad.player = player; lookPad.look = true;
            Label("Look hint", touch.transform, "SWIPE TO LOOK", 17, new Vector2(1, 0), new Vector2(-142, 176), new Vector2(250, 50));
            var stick = Panel("Move pad", touch.transform, new Color(.91f, .94f, .81f, .25f), true);
            Place(stick, new Vector2(0, 0), new Vector2(130, 177), new Vector2(188, 188));
            stick.GetComponent<Image>().sprite = Disc();
            var knob = Panel("Thumb", stick, new Color(.87f, .92f, .76f, .85f), false);
            Place(knob, new Vector2(.5f, .5f), Vector2.zero, new Vector2(68, 68)); knob.GetComponent<Image>().sprite = disc;
            var movePad = stick.gameObject.AddComponent<CreekTouchPad>(); movePad.player = player; movePad.knob = knob;
            Label("Walk hint", touch.transform, "MOVE", 17, new Vector2(0, 0), new Vector2(130, 57), new Vector2(130, 40));
            touch.SetActive(player.UseTouchControls);
        }
        public void ShowMenu(bool show)
        {
            menuOpen = show; menu.SetActive(show); hud.SetActive(!show);
            if (player) { player.InputEnabled = !show; player.ReleaseLook(); if (!show) player.ResumeLook(); }
            if (cinematic) cinematic.InputEnabled = !show;
            if (show) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        public void LoadMode(string scene)
        {
            if (loading) return;
            if (SceneManager.GetActiveScene().name == scene) { ShowMenu(false); return; }
            StartCoroutine(Load(scene));
        }
        IEnumerator Load(string scene)
        {
            loading = true; ShowMenu(true); menuCard.gameObject.SetActive(false); loadingLabel.gameObject.SetActive(true);
            loadingLabel.text = "Entering the creek…";
            var operation = SceneManager.LoadSceneAsync(scene);
            while (!operation.isDone) { loadingLabel.text = "Entering the creek… " + Mathf.RoundToInt(operation.progress / .9f * 100) + "%"; yield return null; }
        }
        void Update()
        {
            if (previousSize != new Vector2(Screen.width, Screen.height) || previousSafe != Screen.safeArea) UpdateLayout();
            if (!isMenu && !loading && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ShowMenu(!menuOpen);
            if (player && resume) resume.SetActive(!player.UseTouchControls && Cursor.lockState != CursorLockMode.Locked);
            if (cinematic && playLabel) { playLabel.text = cinematic.IsPaused ? "Play" : "Pause"; shotLabel.text = cinematic.CurrentShotName; }
        }
        void UpdateLayout()
        {
            previousSize = new Vector2(Screen.width, Screen.height); previousSafe = Screen.safeArea;
            scaler.referenceResolution = Screen.height > Screen.width ? new Vector2(720, 1280) : new Vector2(1280, 720);
            var safeArea = Screen.safeArea;
            // Some editor Game view profiles expose the host window's safe area rather than the render target.
            // Treat an out-of-bounds rectangle as a full viewport; physical device safe areas remain intact.
            if (safeArea.width <= 0 || safeArea.height <= 0 || safeArea.xMin < 0 || safeArea.yMin < 0 ||
                safeArea.xMax > previousSize.x * 1.5f || safeArea.yMax > previousSize.y * 1.5f)
                safeArea = new Rect(0, 0, previousSize.x, previousSize.y);
            if (Application.isEditor) safeArea = new Rect(0, 0, previousSize.x, previousSize.y);
            safe.anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / previousSize.x), Mathf.Clamp01(safeArea.yMin / previousSize.y));
            safe.anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / previousSize.x), Mathf.Clamp01(safeArea.yMax / previousSize.y));
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            if (player) player.ClearTouch();
            foreach (var pad in GetComponentsInChildren<CreekTouchPad>(true)) pad.ResetPad();
        }
        RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false); return (RectTransform)obj.transform;
        }
        RectTransform Panel(string name, Transform parent, Color color, bool raycast)
        {
            var rect = Rect(name, parent); Stretch(rect); var img = rect.gameObject.AddComponent<Image>(); img.color = color; img.raycastTarget = raycast; return rect;
        }
        Text Label(string name, Transform parent, string content, int size, Vector2 anchor, Vector2 position, Vector2 dimensions, Color? color = null)
        {
            var rect = Rect(name, parent); Place(rect, anchor, position, dimensions);
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.text = content; label.fontSize = size;
            label.color = color ?? Cream; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        Button Button(string name, Transform parent, string title, Vector2 anchor, Vector2 position, Vector2 size, UnityAction action, bool primary = false)
        {
            var rect = Panel(name, parent, primary ? Sage : new Color(.19f, .27f, .22f, .96f), true); Place(rect, anchor, position, size);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
            var colors = button.colors; colors.highlightedColor = new Color(.85f, .93f, .75f); colors.pressedColor = new Color(.65f, .8f, .52f); button.colors = colors;
            var label = Label("Label", rect, title, size.y > 75 ? 25 : 22, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(14, 0), primary ? Forest : Cream);
            return button;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        Sprite Disc()
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(64 - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64)))));
            texture.Apply(); disc = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f)); return disc;
        }
        void OnDestroy() { if (disc) { Destroy(disc.texture); Destroy(disc); } }
    }
}
