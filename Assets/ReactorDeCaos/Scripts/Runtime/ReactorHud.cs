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
        public static readonly Color Magenta = new Color(1, .24f, .67f);
        public Image CoreImage { get; private set; }
        public GlowGraphic CoreGlow { get; private set; }
        public GlowGraphic CoreFlash { get; private set; }
        public PresentationPreferences Preferences { get; private set; }
        public ReactorAssembly Assembly { get; private set; }
        public ReactorModulePanel ModulePanel { get; private set; }
        public RebirthPanel Progression { get; private set; }
        public Button ProgressionButton { get; private set; }
        public int Skin { get; private set; }
        private TextMeshProUGUI progressionLabel, achievementsLabel;
        public AchievementPanel Achievements { get; private set; }
        public Button[] NavigationButtons { get; private set; }
        public int SelectedTab { get; private set; }
        private RectTransform reactorPage, shopPage;
        private TextMeshProUGUI[] shopPrices;
        private Image[] tabIndicators;
        public Sprite Icon(string id) { return sprites.Icon(id); }
        public PresentationMenu Menu { get; private set; }
        public Button SettingsButton { get; private set; }
        private PresentationSprites sprites;
        private CanvasGroup rewardCard;
        private TextMeshProUGUI rewardText, tapHint;
        private float rewardUntil;
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
            Preferences = app.Preferences;
            sprites = new PresentationSprites(Resources.Load<PresentationAssets>("Presentation"));
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
            Image("Header", safe, new Color(.025f, .045f, .075f, .85f), .035f, .825f, .965f, .98f);
            Image("HeaderRule", safe, new Color(.18f, .92f, 1, .4f), .065f, .824f, .935f, .826f);
            Label("Brand", safe, "REACTOR / DE CAOS", 26, Muted, .065f, .945f, .80f, .97f, TextAlignmentOptions.Left);
            energy = Label("Energy", safe, "0", 78, Color.white, .06f, .871f, .78f, .941f, TextAlignmentOptions.Left);
            Label("Unit", safe, "ENERGÍA", 22, Cyan, .68f, .878f, .94f, .899f);
            rate = Label("Rate", safe, "0 / s", 27, Cyan, .065f, .836f, .61f, .867f, TextAlignmentOptions.Left);
            stage = Label("Stage", safe, "", 21, Warm, .55f, .836f, .94f, .867f, TextAlignmentOptions.Right);
            reactorPage = Rect("ReactorPage", safe, 0, .105f, 1, .815f);
            shopPage = Rect("EngineeringPage", safe, .035f, .105f, .965f, .815f);
            Image("EngineeringSurface", shopPage, new Color(.02f, .035f, .06f, .96f), 0, 0, 1, 1);
            combo = Label("Combo", reactorPage, "COMBO ×1", 29, Cyan, .16f, .95f, .84f, .995f);

            RectTransform reactorViewport = Rect("ReactorViewport", reactorPage, .025f, .235f, .975f, .94f);
            reactorViewport.gameObject.AddComponent<RectMask2D>();
            var mount = Rect("CoreMount", reactorViewport, .23f, .30f, .77f, .84f).gameObject.AddComponent<SocketGraphic>();
            mount.raycastTarget = false;
            CoreVisual = Rect("CoreVisual", reactorViewport, .26f, .33f, .74f, .81f);
            CoreGlow = Rect("CoreGlow", CoreVisual, -.06f, -.06f, 1.06f, 1.06f).gameObject.AddComponent<GlowGraphic>();
            CoreGlow.raycastTarget = false;
            CoreImage = Image("CoreSprite", CoreVisual, Color.white, .02f, .02f, .98f, .98f);
            CoreImage.sprite = sprites.Core;
            CoreImage.preserveAspect = true;
            CoreFlash = Rect("CoreFlash", CoreVisual, .24f, .24f, .76f, .76f).gameObject.AddComponent<GlowGraphic>();
            CoreFlash.raycastTarget = false;
            CoreFlash.color = Color.clear;
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
            TapButton = Button("TapReactor", reactorViewport, new Color(0, 0, 0, 0), .26f, .33f, .74f, .81f);
            TapButton.onClick.AddListener(app.Tap);
            Assembly = new ReactorAssembly(this, reactorViewport, app.Game);
            EffectsRoot = Rect("Effects", reactorViewport, 0, 0, 1, 1);
            tapHint = Label("TapHint", reactorPage, "TOCA EL NÚCLEO PARA GENERAR\nToca un módulo para instalarlo o mejorarlo.", 24, Color.white, .05f, .19f, .95f, .235f);
            milestone = Label("Milestone", reactorPage, "", 24, Muted, .05f, .135f, .95f, .185f);
            OverloadButton = Button("ActivateOverload", reactorPage, new Color(.13f, .07f, .23f, .98f), .055f, .025f, .945f, .115f);
            OverloadButton.onClick.AddListener(app.ActivateOverload);
            overloadLabel = Label("OverloadLabel", OverloadButton.transform, "", 29, Color.white, .02f, .1f, .98f, .94f);
            Image track = Image("ChargeTrack", reactorPage, new Color(.12f, .12f, .2f), .055f, .015f, .945f, .022f);
            chargeFill = Image("Charge", track.transform, Violet, 0, 0, 0, 1).rectTransform;
            Label("ShopTitle", shopPage, "MEJORAS", 38, Color.white, .04f, .925f, .96f, .985f, TextAlignmentOptions.Left);
            Label("ShopHelp", shopPage, "Mejora tus toques o genera energía automáticamente.\nDesliza para explorar los módulos.", 26, Muted, .04f, .84f, .96f, .925f, TextAlignmentOptions.Left);
            RectTransform viewport = Rect("ShopViewport", shopPage, .025f, .025f, .975f, .825f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, .01f);
            RectTransform content = Rect("ShopContent", viewport, 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1);
            int itemCount = app.Game.Shop.Count;
            content.sizeDelta = new Vector2(0, itemCount * 234);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            ShopButtons = new Button[itemCount];
            shopText = new TextMeshProUGUI[itemCount];
            shopPrices = new TextMeshProUGUI[itemCount];
            lastCosts = new double[itemCount]; lastLevels = new int[itemCount];
            lastAffordable = new bool[itemCount]; lastUnlocked = new bool[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                lastLevels[i] = -1;
                ShopEntry entry = app.Game.Shop[i];
                Button button = Button("Buy" + entry.Id, content, Panel, 0, 1, 1, 1);
                var buttonRect = (RectTransform)button.transform;
                buttonRect.pivot = new Vector2(.5f, 1);
                buttonRect.sizeDelta = new Vector2(0, 218);
                buttonRect.anchoredPosition = new Vector2(0, -i * 234);
                button.onClick.AddListener(() => app.Buy(entry.Id));
                ShopButtons[i] = button;
                Image("PurchaseSurface", button.transform, new Color(.08f, .15f, .20f), .73f, .10f, .98f, .90f);
                shopPrices[i] = Label("PurchasePrice", button.transform, "", 28, Warm, .74f, .12f, .97f, .88f);
                Image("Accent", button.transform, entry.IsGenerator ? Violet : Cyan, 0, 0, .006f, 1);
                Image icon = Image("Icon", button.transform, Color.white, .015f, .17f, .145f, .83f);
                icon.sprite = sprites.Icon(entry.Id);
                icon.preserveAspect = true;
                shopText[i] = Label("Description", button.transform, "", 30, Color.white, .16f, .07f, .71f, .93f, TextAlignmentOptions.Left);
            }
            notice = Label("Notice", safe, "", 20, Warm, .045f, .003f, .955f, .031f);
            NavigationButtons = new Button[4]; tabIndicators = new Image[4];
            string[] tabNames = { "REACTOR", "MEJORAS", "LOGROS", "ASCENSIÓN" };
            for (int i = 0; i < 4; i++)
            {
                int tab = i;
                var button = Button("Navigation" + i, safe, new Color(.025f, .045f, .075f, .98f), .035f + i * .235f, .038f, .26f + i * .235f, .097f);
                NavigationButtons[i] = button;
                var text = Label("NavigationLabel", button.transform, tabNames[i], 26, Color.white, .01f, .06f, .99f, .94f);
                tabIndicators[i] = Image("SelectedTab", button.transform, Cyan, .08f, .96f, .92f, 1);
                button.onClick.AddListener(() => SelectTab(tab));
                if (i == 2) achievementsLabel = text;
                if (i == 3) { ProgressionButton = button; progressionLabel = text; }
            }
            Image card = Image("RewardCard", reactorPage, Panel, .05f, .13f, .95f, .235f);
            rewardCard = card.gameObject.AddComponent<CanvasGroup>();
            rewardCard.blocksRaycasts = false;
            Image rewardIcon = Image("RewardIcon", card.transform, Color.white, .015f, .04f, .14f, .96f);
            rewardIcon.sprite = sprites.Icon("reward"); rewardIcon.preserveAspect = true;
            rewardText = Label("RewardText", card.transform, "", 26, Warm, .16f, .03f, .98f, .97f, TextAlignmentOptions.Left);
            rewardCard.gameObject.SetActive(false);
            SettingsButton = Button("Settings", safe, Panel, .81f, .9f, .955f, .98f);
            Image settingsIcon = Image("SettingsIcon", SettingsButton.transform, Color.white, .27f, .4f, .73f, .94f);
            settingsIcon.sprite = sprites.Icon("settings"); settingsIcon.preserveAspect = true;
            Label("SettingsLabel", SettingsButton.transform, "AJUSTES", 24, Cyan, .02f, .03f, .98f, .36f);
            Menu = new PresentationMenu(this, safe, SettingsButton, Preferences);
            ModulePanel = new ReactorModulePanel(this, safe, app);
            Progression = new RebirthPanel(this, safe, app);
            Achievements = new AchievementPanel(this, safe, app);
            SelectTab(0);
            Refresh(app.Game, true);
        }

        private void OnDestroy()
        {
            sprites?.Dispose();
            if (ownsRuntimeFont) ReactorFont.Release(Font);
        }

        public void ClosePanels()
        {
            if (Menu != null && Menu.IsOpen) Menu.SetOpen(false);
            ModulePanel?.Close(); Progression?.Close(); Achievements?.Close();
        }
        public void SelectTab(int tab)
        {
            if (tab < 0 || tab > 3) return;
            ClosePanels();
            SelectedTab = tab;
            reactorPage.gameObject.SetActive(tab == 0);
            shopPage.gameObject.SetActive(tab == 1);
            for (int i = 0; i < tabIndicators.Length; i++)
            {
                tabIndicators[i].gameObject.SetActive(i == tab);
                NavigationButtons[i].image.color = i == tab ? new Color(.055f, .12f, .17f) : new Color(.025f, .045f, .075f, .98f);
            }
            if (tab == 2) Achievements.Open();
            if (tab == 3) Progression.Open();
        }
        public void OpenModule(string id) { ModulePanel.Open(id); }
        public void Back()
        {
            if (Menu.IsOpen) Menu.SetOpen(false);
            else if (Progression.IsConfirming) Progression.CancelConfirmation();
            else if (Progression.IsOpen || Achievements.IsOpen || SelectedTab != 0) SelectTab(0);
            else if (ModulePanel.IsOpen) ModulePanel.Close();
            else Menu.SetOpen(!Menu.IsOpen);
        }
        public void InvalidateEconomy()
        {
            for (int i = 0; i < lastLevels.Length; i++) lastLevels[i] = -1;
            nextTextUpdate = 0;
        }
        public void ResetCycle()
        {
            SelectTab(0); InvalidateEconomy(); lastStage = -1;
            Assembly.Reset();
            rewardCard.gameObject.SetActive(false);
            tapHint.gameObject.SetActive(true); milestone.gameObject.SetActive(true);
        }

        public void ShowNotice(string text)
        {
            if (notice == null) return;
            notice.text = text;
            noticeUntil = Time.unscaledTime + 4;
        }

        public void ShowReward(MilestoneDefinition reward)
        {
            rewardText.text = "HITO · " + reward.title + "\n<size=80%>" + reward.rewardDescription + "</size>";
            rewardUntil = Time.unscaledTime + 3.5f;
            rewardCard.gameObject.SetActive(true);
            rewardCard.alpha = 1;
            tapHint.gameObject.SetActive(false);
            milestone.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (rewardCard == null || !rewardCard.gameObject.activeSelf) return;
            float remaining = rewardUntil - Time.unscaledTime;
            if (remaining <= 0)
            {
                rewardCard.gameObject.SetActive(false);
                tapHint.gameObject.SetActive(true);
                milestone.gameObject.SetActive(true);
                return;
            }
            rewardCard.alpha = Mathf.Clamp01(remaining / .4f);
            float entry = Mathf.Clamp01((3.5f - remaining) / .3f);
            rewardCard.transform.localScale = Vector3.one * (Preferences.reducedMotion ? 1 : .96f + .04f * entry);
        }

        public void Refresh(GameEngine game, bool force = false)
        {
            ApplySafeArea();
            Skin = game.Skin;
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
                stage.text = RebirthCatalog.FormName(game.Stage);
            }
            progressionLabel.text = game.CanRebirth ? "ASCENSIÓN\nDISPONIBLE" : "ASCENSIÓN";
            achievementsLabel.text = "LOGROS" + (game.AvailableAchievements > 0 ? "\n" + game.AvailableAchievements + " POR RECLAMAR" : "");
            ModulePanel.Refresh(); Progression.Refresh(); Achievements.Refresh();
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
                shopText[i].text = entry.Name + " <color=#74E6F2>· NV. " + level + "</color>"
                    + "\n<size=80%><color=#BBCADD>" + description + "\n" + effect + "</color></size>";
                shopPrices[i].text = !unlocked ? price : level >= EconomyConfig.MaxLevel ? "MÁXIMO"
                    : (affordable ? "COMPRAR" : "NECESITAS") + "\n" + price;

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

        internal static Image Image(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var image = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        internal static Button Button(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
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
