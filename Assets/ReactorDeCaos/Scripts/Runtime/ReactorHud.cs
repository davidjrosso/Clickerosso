using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class ReactorHud : MonoBehaviour
    {
        public static readonly Color Cyan = new Color(.18f, .92f, 1);
        public static readonly Color Violet = new Color(.68f, .39f, 1);
        public static readonly Color Warm = new Color(1, .69f, .27f);
        private static readonly Color Panel = new Color(.025f, .045f, .09f, .96f);
        private static readonly Color Muted = new Color(.55f, .66f, .78f);
        public RectTransform EffectsRoot { get; private set; }
        public RectTransform CoreVisual { get; private set; }
        public RingGraphic Orbit { get; private set; }
        public RingGraphic[] EvolutionRings { get; private set; }
        public TMP_FontAsset Font { get; private set; }
        private bool ownsRuntimeFont;
        public Button TapButton { get; private set; }
        public Button OverloadButton { get; private set; }
        public Button[] ShopButtons { get; private set; }
        private RectTransform safe;
        private TextMeshProUGUI energy, rate, combo, stage, milestone, overloadLabel, notice;
        private TextMeshProUGUI[] shopText;
        private RectTransform chargeFill;
        private float noticeUntil, nextTextUpdate;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private int lastStage = -1;
        private double[] lastCosts;
        private int[] lastLevels;
        private bool[] lastAffordable, lastUnlocked;

        public void Build(ReactorApp app)
        {
            Font = Resources.Load<TMP_FontAsset>("Fonts/ReactorFont");
            if (Font == null)
            {
                Font = ReactorFont.CreateRuntimeFont();
                ownsRuntimeFont = true;
            }
            var cameraObject = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.01f, .02f, .04f);
            camera.cullingMask = 0;
            var canvasObject = new GameObject("ReactorCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            RectTransform canvas = (RectTransform)canvasObject.transform;
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform);
            }
            Image backdrop = Image("Backdrop", canvas, new Color(.01f, .02f, .04f), 0, 0, 1, 1);
            var art = new GameObject("ReactorArt", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            art.transform.SetParent(backdrop.transform, false);
            RectTransform artRect = (RectTransform)art.transform;
            artRect.anchorMin = Vector2.zero; artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero; artRect.offsetMax = Vector2.zero;
            art.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Art/ReactorBackdrop");
            art.GetComponent<RawImage>().raycastTarget = false;
            var fitter = art.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 2f / 3f;
            // Dim the illustration enough for UI, without obscuring the core.
            Image("Veil", canvas, new Color(0, .015f, .04f, .15f), 0, 0, 1, 1);
            safe = Rect("SafeArea", canvas, 0, 0, 1, 1);
            ApplySafeArea();
            Image("Header", safe, Panel, .035f, .825f, .965f, .98f);
            Label("Brand", safe, "REACTOR / DE CAOS", 26, Muted, .065f, .945f, .94f, .97f, TextAlignmentOptions.Left);
            energy = Label("Energy", safe, "0", 78, Color.white, .06f, .871f, .78f, .941f, TextAlignmentOptions.Left);
            Label("Unit", safe, "ENERGÍA", 22, Cyan, .68f, .89f, .94f, .925f);
            rate = Label("Rate", safe, "0 / s", 27, Cyan, .065f, .836f, .61f, .867f, TextAlignmentOptions.Left);
            stage = Label("Stage", safe, "", 21, Warm, .55f, .836f, .94f, .867f, TextAlignmentOptions.Right);
            combo = Label("Combo", safe, "COMBO ×1", 29, Cyan, .16f, .77f, .84f, .805f);

            CoreVisual = Rect("CoreVisual", safe, .27f, .535f, .73f, .77f);
            Orbit = Ring("Orbit", CoreVisual, Cyan);
            Orbit.thickness = 3;
            Orbit.fraction = .82f;
            EvolutionRings = new RingGraphic[4];
            for (int i = 0; i < EvolutionRings.Length; i++)
            {
                RingGraphic ring = Ring("EvolutionRing" + i, CoreVisual, i < 2 ? Violet : Warm);
                ring.rectTransform.localScale = Vector3.one * (.88f - i * .12f);
                ring.thickness = 2 + i;
                ring.fraction = .36f + i * .12f;
                ring.gameObject.SetActive(false);
                EvolutionRings[i] = ring;
            }
            TapButton = Button("TapReactor", safe, new Color(0, 0, 0, 0), .16f, .49f, .84f, .77f);
            TapButton.onClick.AddListener(app.Tap);
            Label("TapHint", safe, "TOCA PARA GENERAR", 23, Color.white, .15f, .48f, .85f, .51f);
            milestone = Label("Milestone", safe, "", 22, Muted, .05f, .447f, .95f, .479f);
            OverloadButton = Button("ActivateOverload", safe, new Color(.19f, .08f, .3f, .98f), .045f, .369f, .955f, .444f);
            OverloadButton.onClick.AddListener(app.ActivateOverload);
            overloadLabel = Label("OverloadLabel", OverloadButton.transform, "", 29, Color.white, .02f, .1f, .98f, .94f);
            Image track = Image("ChargeTrack", safe, new Color(.12f, .12f, .2f), .05f, .359f, .95f, .365f);
            chargeFill = Image("Charge", track.transform, Violet, 0, 0, 0, 1).rectTransform;
            Label("ShopTitle", safe, "INGENIERÍA  ·  DESLIZA PARA VER MÁS", 26, Muted, .05f, .332f, .95f, .358f, TextAlignmentOptions.Left);
            RectTransform viewport = Rect("ShopViewport", safe, .045f, .052f, .955f, .326f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, .01f);
            RectTransform content = Rect("ShopContent", viewport, 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1);
            int itemCount = app.Game.Shop.Count;
            content.sizeDelta = new Vector2(0, itemCount * 204);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            ShopButtons = new Button[itemCount];
            shopText = new TextMeshProUGUI[itemCount];
            lastCosts = new double[itemCount]; lastLevels = new int[itemCount];
            lastAffordable = new bool[itemCount]; lastUnlocked = new bool[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                lastLevels[i] = -1;
                ShopEntry entry = app.Game.Shop[i];
                Button button = Button("Buy" + entry.Id, content, Panel, 0, 1, 1, 1);
                var buttonRect = (RectTransform)button.transform;
                buttonRect.pivot = new Vector2(.5f, 1);
                buttonRect.sizeDelta = new Vector2(0, 192);
                buttonRect.anchoredPosition = new Vector2(0, -i * 204);
                button.onClick.AddListener(() => app.Buy(entry.Id));
                ShopButtons[i] = button;
                Image("Accent", button.transform, entry.IsGenerator ? Violet : Cyan, 0, 0, .006f, 1);
                shopText[i] = Label("Description", button.transform, "", 40, Color.white, .028f, .05f, .98f, .96f, TextAlignmentOptions.Left);
            }
            notice = Label("Notice", safe, "", 20, Warm, .045f, .007f, .955f, .044f);
            EffectsRoot = Rect("Effects", safe, 0, 0, 1, 1);
            Refresh(app.Game, true);
        }

        private void OnDestroy()
        {
            if (ownsRuntimeFont) ReactorFont.Release(Font);
        }

        public void ShowNotice(string text)
        {
            if (notice == null) return;
            notice.text = text;
            noticeUntil = Time.unscaledTime + 4;
        }

        public void Refresh(GameEngine game, bool force = false)
        {
            ApplySafeArea();
            float progress = game.Overload.Active
                ? (float)(game.Overload.Remaining / game.Overload.ActiveDuration)
                : (float)game.Overload.ChargeFraction;
            chargeFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            OverloadButton.interactable = game.Overload.Ready;
            for (int i = 0; i < ShopButtons.Length; i++) ShopButtons[i].interactable = game.CanBuy(game.Shop[i].Id);
            if (!force && Time.unscaledTime < nextTextUpdate) return;
            nextTextUpdate = Time.unscaledTime + .08f;
            energy.text = game.EnergyLimitReached ? "LÍMITE · " + NumberFormat.Compact(game.Energy) : NumberFormat.Compact(game.Energy);
            rate.text = NumberFormat.Stat(game.Production) + " / s   ·   " + NumberFormat.Stat(game.CurrentTapGain) + " / toque";
            combo.text = "COMBO ×" + NumberFormat.Stat(game.ComboMultiplier)
                + (game.Combo.Decaying ? " · BAJANDO" : game.ComboMultiplier >= game.ComboCap ? " · MÁXIMO" : " · " + game.Combo.Count + " TOQUES");
            combo.color = game.Combo.Decaying ? Muted : Cyan;
            if (game.Stage != lastStage)
            {
                lastStage = game.Stage;
                stage.text = new[] { "IGNICIÓN I", "PLASMA II", "CUÁNTICO III", "DOBLE NÚCLEO IV", "CAOS V" }[game.Stage];
            }
            MilestoneDefinition goal = game.NextGoal();
            milestone.text = goal == null ? "HITOS COMPLETADOS · sigue ampliando tu reactor"
                : goal.title + "  " + NumberFormat.Compact(game.GoalProgress(goal)) + " / " + NumberFormat.Compact(goal.target);
            overloadLabel.text = game.Overload.Active
                ? "SOBRECARGA ×" + NumberFormat.Stat(game.OverloadMultiplier) + "  ·  " + game.Overload.Remaining.ToString("0.0") + " s"
                : game.Overload.Ready ? "ACTIVAR SOBRECARGA  ×" + NumberFormat.Stat(game.OverloadMultiplier)
                : "CARGANDO SOBRECARGA  ·  " + (game.Overload.ChargeFraction * 100).ToString("0") + "%";
            for (int i = 0; i < ShopButtons.Length; i++)
            {
                ShopEntry entry = game.Shop[i];
                string item = entry.Id;
                int level = game.Level(item);
                double cost = game.Cost(item);
                bool affordable = game.CanBuy(item);
                bool unlocked = game.Unlocked(item);
                if (lastLevels[i] == level && lastCosts[i] == cost
                    && lastAffordable[i] == affordable && lastUnlocked[i] == unlocked) continue;
                lastLevels[i] = level; lastCosts[i] = cost;
                lastAffordable[i] = affordable; lastUnlocked[i] = unlocked;
                string price = level >= EconomyConfig.MaxLevel ? "MÁX."
                    : NumberFormat.Compact(cost) + " E";
                if (!unlocked) price += " · BLOQUEADO";
                double current = game.EffectAtLevel(item, level);
                double next = game.EffectAtLevel(item, Math.Min(EconomyConfig.MaxLevel, level + 1));
                string effect = NumberFormat.Stat(current) + " a " + NumberFormat.Stat(next) + " " + entry.Unit;
                if (level >= EconomyConfig.MaxLevel) effect = NumberFormat.Stat(current) + " " + entry.Unit + " · MÁXIMO";
                string description = unlocked ? entry.Description : "Requiere " + game.UnlockDescription(item);
                shopText[i].text = entry.Name + " <color=#74E6F2>· " + level + "</color>"
                    + "   <color=" + (affordable ? "#FFBF6C" : "#77869B") + ">" + price
                    + "</color>\n<size=65%><color=#A5B5CF>" + description + "\n" + effect + "</color></size>";
            }
            if (Time.unscaledTime > noticeUntil) notice.text = "GUARDADO LOCAL  ·  SIN CONEXIÓN";
        }

        private void ApplySafeArea()
        {
            Rect area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == null || size.x == 0 || size.y == 0 || (area == lastSafeArea && size == lastScreenSize)) return;
            lastSafeArea = area; lastScreenSize = size;
            safe.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safe.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        public static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image Image(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var image = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private static Button Button(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            Image image = Image(name, parent, color, x0, y0, x1, y1);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(.8f, 1, 1);
            colors.pressedColor = new Color(.5f, .8f, 1);
            colors.disabledColor = new Color(.58f, .63f, .72f, 1);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        public TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            float x0, float y0, float x1, float y1, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * .68f;
            label.fontSizeMax = size;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        public static RingGraphic Ring(string name, Transform parent, Color color)
        {
            var ring = Rect(name, parent, 0, 0, 1, 1).gameObject.AddComponent<RingGraphic>();
            ring.color = color;
            ring.raycastTarget = false;
            return ring;
        }
    }
}
