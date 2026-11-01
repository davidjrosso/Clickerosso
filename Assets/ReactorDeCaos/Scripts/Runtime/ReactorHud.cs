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
        public StoryPanel Story { get; private set; }
        public StationPanel Station { get; private set; }
        public NarrativeSprites NarrativeArt { get; private set; }
        public RectTransform SafeRoot => safe;
        private ReactorApp owner;
        private string transmission;
        private Image memoryCrystal, chamberVeil;
        public Button StoryButton { get; private set; }
        private TextMeshProUGUI storyLabel;
        private StationRestorationGraphic station;
        public Button[] NavigationButtons { get; private set; }
        public int SelectedTab { get; private set; }
        private RectTransform reactorPage, shopPage;
        private RectTransform[] shopRows;
        public const float MinimumFontSize = 48;
        private bool shopLayoutDirty = true;
        private float shopLayoutWidth = -1;
        private GameObject saveWarning;
        public Button RetrySaveButton { get; private set; }
        public bool SaveFailed { get; private set; }
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
        private static readonly string[] shortStages = { "Semilla", "Plasma", "Cuántico", "Estrella", "Singularidad" };
        private double[] lastCosts;
        private int[] lastLevels;
        private bool[] lastAffordable, lastUnlocked;

        public void Build(ReactorApp app)
        {
            owner = app; Preferences = app.Preferences;
            NarrativeArt = new NarrativeSprites();
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
            scaler.matchWidthOrHeight = 0; // Stable text/touch scale across portrait aspect ratios.
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
            chamberVeil = Image("Veil", canvas, new Color(0, .015f, .04f, .15f), 0, 0, 1, 1);
            safe = Rect("SafeArea", canvas, 0, 0, 1, 1);
            ApplySafeArea();
            Image("Header", safe, new Color(.025f, .045f, .075f, .85f), .035f, .825f, .965f, .98f);
            Image("HeaderRule", safe, new Color(.18f, .92f, 1, .4f), .065f, .824f, .935f, .826f);
            Label("Brand", safe, "Reactor de Caos", 26, Muted, .065f, .955f, .60f, .98f, TextAlignmentOptions.Left);
            energy = Label("Energy", safe, "0", 78, Color.white, .06f, .895f, .78f, .95f, TextAlignmentOptions.Left);
            Label("Unit", safe, "E", 22, Cyan, .62f, .955f, .77f, .98f);
            rate = Label("Rate", safe, "0 / s", 32, Cyan, .065f, .827f, .66f, .890f, TextAlignmentOptions.Left);
            stage = Label("Stage", safe, "", 32, Warm, .68f, .827f, .955f, .887f, TextAlignmentOptions.Right);
            reactorPage = Rect("ReactorPage", safe, 0, .145f, 1, .815f);
            shopPage = Rect("EngineeringPage", safe, .035f, .145f, .965f, .815f);
            Image("EngineeringSurface", shopPage, new Color(.02f, .035f, .06f, .96f), 0, 0, 1, 1);
            combo = Label("Combo", reactorPage, "COMBO ×1", 29, Cyan, .16f, .95f, .84f, .995f);

            RectTransform reactorViewport = Rect("ReactorViewport", reactorPage, .025f, .305f, .975f, .94f);
            reactorViewport.gameObject.AddComponent<RectMask2D>();
            station = Rect("StationRestoration", reactorViewport, 0, 0, 1, 1).gameObject.AddComponent<StationRestorationGraphic>();
            station.raycastTarget = false;
            var mount = Rect("CoreMount", reactorViewport, .23f, .30f, .77f, .84f).gameObject.AddComponent<SocketGraphic>();
            mount.raycastTarget = false;
            CoreVisual = Rect("CoreVisual", reactorViewport, .26f, .33f, .74f, .81f);
            CoreGlow = Rect("CoreGlow", CoreVisual, -.06f, -.06f, 1.06f, 1.06f).gameObject.AddComponent<GlowGraphic>();
            CoreGlow.raycastTarget = false;
            CoreImage = Image("CoreSprite", CoreVisual, Color.white, .02f, .02f, .98f, .98f);
            CoreImage.sprite = NarrativeArt.Core;
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
            memoryCrystal = Image("RecoveredMemory", reactorViewport, Color.white, .445f, .84f, .555f, .96f);
            memoryCrystal.sprite = NarrativeArt.Portrait("memory"); memoryCrystal.preserveAspect = true;
            Assembly = new ReactorAssembly(this, reactorViewport, app.Game);
            Label("ReactorInstruction", reactorViewport, "TOCA EL NÚCLEO", 23, Color.white, .22f, .245f, .78f, .30f);
            EffectsRoot = Rect("Effects", reactorViewport, 0, 0, 1, 1);
            StoryButton = Button("OpenStory", reactorPage, new Color(.035f, .08f, .12f, .94f), .05f, .17f, .95f, .30f);
            StoryButton.onClick.AddListener(app.CampaignAction);
            Image("StoryAccent", StoryButton.transform, Cyan, 0, 0, .006f, 1);
            tapHint = Label("TapHint", StoryButton.transform, "NARA · VER HISTORIA", 26, Cyan, .035f, .48f, .965f, .95f);
            milestone = Label("Milestone", StoryButton.transform, "", 24, Color.white, .035f, .05f, .965f, .48f);
            var missionViewport = Rect("MissionViewport", StoryButton.transform, .025f, .04f, .975f, .96f);
            missionViewport.gameObject.AddComponent<RectMask2D>();
            missionViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            var missionContent = Rect("MissionContent", missionViewport, 0, 1, 1, 1);
            missionContent.pivot = new Vector2(.5f, 1);
            var missionLayout = missionContent.gameObject.AddComponent<VerticalLayoutGroup>();
            missionLayout.spacing = 12;
            missionLayout.childControlWidth = missionLayout.childControlHeight = true;
            missionLayout.childForceExpandHeight = false;
            missionContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tapHint.transform.SetParent(missionContent, false); milestone.transform.SetParent(missionContent, false);
            tapHint.enableAutoSizing = milestone.enableAutoSizing = false;
            tapHint.alignment = milestone.alignment = TextAlignmentOptions.TopLeft;
            var missionScroll = missionViewport.gameObject.AddComponent<ScrollRect>();
            missionScroll.viewport = missionViewport; missionScroll.content = missionContent;
            missionScroll.horizontal = false; missionScroll.movementType = ScrollRect.MovementType.Clamped;
            OverloadButton = Button("ActivateOverload", reactorPage, new Color(.13f, .07f, .23f, .98f), .055f, .02f, .945f, .16f);
            OverloadButton.onClick.AddListener(app.ActivateOverload);
            overloadLabel = Label("OverloadLabel", OverloadButton.transform, "", 29, Color.white, .02f, .1f, .98f, .94f);
            Image track = Image("ChargeTrack", reactorPage, new Color(.12f, .12f, .2f), .055f, .006f, .945f, .014f);
            chargeFill = Image("Charge", track.transform, Violet, 0, 0, 0, 1).rectTransform;
            Label("ShopTitle", shopPage, "MEJORAS", 38, Color.white, .04f, .925f, .96f, .985f, TextAlignmentOptions.Left);
            ScrollableLabel("ShopHelp", shopPage, "Toca una tarjeta para ver detalles.\nSolo COMPRAR gasta energía. Desliza para explorar.", 26, Muted, .04f, .84f, .96f, .925f);
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
            ShopButtons = new Button[itemCount]; shopRows = new RectTransform[itemCount];
            shopText = new TextMeshProUGUI[itemCount];
            shopPrices = new TextMeshProUGUI[itemCount];
            lastCosts = new double[itemCount]; lastLevels = new int[itemCount];
            lastAffordable = new bool[itemCount]; lastUnlocked = new bool[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                lastLevels[i] = -1;
                ShopEntry entry = app.Game.Shop[i];
                var row = Image("ShopRow_" + entry.Id, content, Panel, 0, 1, 1, 1).rectTransform;
                shopRows[i] = row; row.pivot = new Vector2(.5f, 1);
                row.sizeDelta = new Vector2(0, 440);
                var details = Button("Details_" + entry.Id, row, Color.clear, 0, 0, 1, 1);
                details.onClick.AddListener(() => { SelectTab(0); OpenModule(entry.Id); });
                Image("Accent", row, entry.IsGenerator ? Violet : Cyan, 0, 0, .006f, 1);
                Image icon = Image("Icon", row, Color.white, .015f, 1, .145f, 1);
                icon.rectTransform.pivot = new Vector2(.5f, 1);
                icon.rectTransform.sizeDelta = new Vector2(0, 140);
                icon.rectTransform.anchoredPosition = new Vector2(0, -24);
                icon.sprite = sprites.Icon(entry.Id); icon.preserveAspect = true;
                shopText[i] = Label("Description", row, "", 36, Color.white, .16f, 1, .97f, 1, TextAlignmentOptions.TopLeft);
                shopText[i].enableAutoSizing = false;
                shopText[i].rectTransform.pivot = new Vector2(.5f, 1);
                shopText[i].rectTransform.anchoredPosition = new Vector2(0, -24);
                Button button = Button("Buy" + entry.Id, row, new Color(.08f, .15f, .20f), .16f, 0, .97f, 0);
                var buttonRect = (RectTransform)button.transform;
                buttonRect.pivot = new Vector2(.5f, 0);
                buttonRect.sizeDelta = new Vector2(0, 164);
                buttonRect.anchoredPosition = new Vector2(0, 20);
                button.onClick.AddListener(() => app.Buy(entry.Id));
                ShopButtons[i] = button;
                shopPrices[i] = Label("PurchasePrice", button.transform, "", 36, Warm, .03f, .08f, .97f, .92f);

            }
            notice = ScrollableLabel("Notice", safe, "", 20, Warm, .045f, .108f, .955f, .14f);
            NavigationButtons = new Button[5]; tabIndicators = new Image[5];
            string[] tabNames = { "Núcleo", "Mejoras", "Logros", "Ciclo", "Archivo" };
            for (int i = 0; i < 5; i++)
            {
                int tab = i;
                int column = i < 2 ? i : i == 4 ? 2 : i + 1;
                var button = Button("Navigation" + i, safe, new Color(.025f, .045f, .075f, .98f), .035f + column * .186f, .015f, .211f + column * .186f, .105f);
                NavigationButtons[i] = button;
                var text = Label("NavigationLabel", button.transform, tabNames[i], 24, Color.white, .01f, .06f, .99f, .94f);
                tabIndicators[i] = Image("SelectedTab", button.transform, Cyan, .08f, .96f, .92f, 1);
                button.onClick.AddListener(() => SelectTab(tab == 4 && owner.Game.CampaignTabAvailable(5) ? 5 : tab));
                if (i == 2) achievementsLabel = text;
                if (i == 4) storyLabel = text;
                if (i == 3) { ProgressionButton = button; progressionLabel = text; }
            }
            Image card = Image("RewardCard", reactorPage, Panel, .05f, .175f, .95f, .30f);
            rewardCard = card.gameObject.AddComponent<CanvasGroup>();
            rewardCard.blocksRaycasts = false;
            Image rewardIcon = Image("RewardIcon", card.transform, Color.white, .015f, .04f, .14f, .96f);
            rewardIcon.sprite = sprites.Icon("reward"); rewardIcon.preserveAspect = true;
            rewardText = Label("RewardText", card.transform, "", 26, Warm, .16f, .03f, .98f, .97f, TextAlignmentOptions.Left);
            rewardCard.gameObject.SetActive(false);
            SettingsButton = Button("Settings", safe, Panel, .79f, .89f, .955f, .98f);
            Image settingsIcon = Image("SettingsIcon", SettingsButton.transform, Color.white, .27f, .4f, .73f, .94f);
            settingsIcon.sprite = sprites.Icon("settings"); settingsIcon.preserveAspect = true;
            Label("SettingsLabel", SettingsButton.transform, "Ajustes", 24, Cyan, .02f, .03f, .98f, .36f);
            Menu = new PresentationMenu(this, safe, SettingsButton, Preferences);
            ModulePanel = new ReactorModulePanel(this, safe, app);
            Progression = new RebirthPanel(this, safe, app);
            Achievements = new AchievementPanel(this, safe, app);
            Story = new StoryPanel(this, safe, app);
            Station = new StationPanel(this, safe, app);
            var warning = Image("SaveWarning", safe, new Color(.18f, .045f, .025f, 1), .025f, .82f, .975f, .99f);
            saveWarning = warning.gameObject; warning.raycastTarget = true;
            ScrollableLabel("SaveWarningText", warning.transform,
                "Cambios sin guardar. Revisa el espacio disponible. Se reintentará automáticamente.",
                36, Color.white, .03f, .06f, .67f, .94f);
            RetrySaveButton = Button("RetrySave", warning.transform, new Color(.35f, .12f, .04f), .70f, .10f, .97f, .90f);
            Label("RetrySaveLabel", RetrySaveButton.transform, "REINTENTAR", 32, Color.white, .02f, .05f, .98f, .95f);
            RetrySaveButton.onClick.AddListener(app.RetrySave);
            saveWarning.SetActive(false);
            SelectTab(0);
            Refresh(app.Game, true);
        }

        private void OnDestroy()
        {
            sprites?.Dispose(); NarrativeArt?.Dispose();
            if (ownsRuntimeFont) ReactorFont.Release(Font);
        }

        public void ClosePanels()
        {
            if (Menu != null && Menu.IsOpen) Menu.SetOpen(false);
            ModulePanel?.Close(); Progression?.Close(); Achievements?.Close(); Story?.Close(); Station?.Close();
        }
        public void SelectTab(int tab)
        {
            if (tab < 0 || tab > 5 || !owner.Game.CampaignTabAvailable(tab) || (owner.Narrative?.IsBlocking ?? false)) return;
            ClosePanels();
            SelectedTab = tab;
            shopLayoutDirty = true;
            reactorPage.gameObject.SetActive(tab == 0);
            shopPage.gameObject.SetActive(tab == 1);
            for (int i = 0; i < tabIndicators.Length; i++)
            {
                tabIndicators[i].gameObject.SetActive(i == (tab == 5 ? 4 : tab));
                NavigationButtons[i].image.color = i == (tab == 5 ? 4 : tab) ? new Color(.055f, .12f, .17f) : new Color(.025f, .045f, .075f, .98f);
            }
            if (tab == 2) Achievements.Open();
            if (tab == 3) Progression.Open();
            if (tab == 4) Story.Open();
            if (tab == 5) Station.Open();
        }
        public void OpenModule(string id) { if (owner.Game.Unlocked(id)) ModulePanel.Open(id); }
        public void Back()
        {
            if (Menu.IsOpen) Menu.SetOpen(false);
            else if (owner.Narrative?.IsBlocking ?? false) owner.Narrative.Skip();
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
            StoryButton.gameObject.SetActive(true);
        }

        public void SetSaveFailed(bool failed)
        {
            SaveFailed = failed;
            if (saveWarning == null) return;
            saveWarning.SetActive(failed);
            if (failed) saveWarning.transform.SetAsLastSibling();
        }

        private void LateUpdate()
        {
            if (SaveFailed && saveWarning != null) saveWarning.transform.SetAsLastSibling();
            if (shopRows != null && shopPage.gameObject.activeInHierarchy) LayoutShop();
        }

        public void ShowNotice(string text)
        {
            if (notice == null) return;
            notice.text = text;
            noticeUntil = Time.unscaledTime + 4;
        }

        public void ShowReward(MilestoneDefinition reward)
        {
            if (owner.Game.CampaignActive)
            {
                ShowNotice(string.IsNullOrEmpty(reward.id) ? reward.title + " · " + reward.rewardDescription
                    : "HITO DE PRODUCCIÓN · " + NumberFormat.Compact(owner.Game.Lifetime) + " E producidas");
                return;
            }
            if (transmission != null) { ShowNotice("HITO · " + reward.title); return; }
            rewardText.text = "HITO · " + reward.title + "\n" + reward.rewardDescription + "";
            rewardUntil = Time.unscaledTime + 3.5f;
            rewardCard.gameObject.SetActive(true);
            rewardCard.alpha = 1;
            StoryButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (rewardCard == null || !rewardCard.gameObject.activeSelf) return;
            float remaining = rewardUntil - Time.unscaledTime;
            if (remaining <= 0)
            {
                rewardCard.gameObject.SetActive(false);
                StoryButton.gameObject.SetActive(true);
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
            station.Present(game.StoryCompleted, game.StoryChoice);
            chamberVeil.color = new Color(0, .015f, .04f, game.StoryCompleted == 0 ? .46f : .15f);
            memoryCrystal.gameObject.SetActive(game.StoryCompleted >= 4);
            memoryCrystal.color = game.StoryChoice == 2 ? Violet : Color.white;
            float progress = game.Overload.Active
                ? (float)(game.Overload.Remaining / game.Overload.ActiveDuration)
                : (float)game.Overload.ChargeFraction;
            chargeFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            OverloadButton.interactable = game.OverloadAvailable && game.Overload.Ready;
            OverloadButton.gameObject.SetActive(game.OverloadAvailable);
            combo.gameObject.SetActive(game.ComboAvailable);
            int tabCount = 0;
            for (int i = 0; i < NavigationButtons.Length; i++) if (game.CampaignTabAvailable(i)) tabCount++;
            int column = 0;
            // Stable order, with wider touch targets while the opening has fewer systems.
            for (int n = 0; n < NavigationButtons.Length; n++)
            {
                int i = n < 2 ? n : n == 2 ? 4 : n - 1;
                bool available = game.CampaignTabAvailable(i);
                NavigationButtons[i].gameObject.SetActive(available);
                NavigationButtons[i].interactable = available;
                if (available)
                {
                    var rect = (RectTransform)NavigationButtons[i].transform;
                    float width = .93f / tabCount;
                    rect.anchorMin = new Vector2(.035f + column * width, .015f);
                    rect.anchorMax = new Vector2(.035f + (column + 1) * width - .01f, .105f);
                    column++;
                }
            }
            for (int i = 0; i < shopRows.Length; i++)
                shopRows[i].gameObject.SetActive(!game.CampaignActive || game.Unlocked(game.Shop[i].Id));
            for (int i = 0; i < ShopButtons.Length; i++) ShopButtons[i].interactable = game.CanBuy(game.Shop[i].Id);
            if (!force && Time.unscaledTime < nextTextUpdate) return;
            nextTextUpdate = Time.unscaledTime + .08f;
            energy.text = game.EnergyLimitReached ? "LÍMITE · " + NumberFormat.Compact(game.Energy) : NumberFormat.Compact(game.Energy);
            rate.text = NumberFormat.Stat(game.Production) + " / s\n" + NumberFormat.Stat(game.CurrentTapGain) + " / toque";
            combo.text = "COMBO ×" + NumberFormat.Stat(game.ComboMultiplier)
                + (game.Combo.Decaying ? " · BAJANDO" : game.ComboMultiplier >= game.ComboCap ? " · MÁXIMO" : " · " + game.Combo.Count + " TOQUES");
            combo.color = game.Combo.Decaying ? Muted : Cyan;
            if (game.Stage != lastStage)
            {
                lastStage = game.Stage;
                stage.text = shortStages[Mathf.Clamp(game.Stage, 0, shortStages.Length - 1)];
            }
            progressionLabel.text = game.CanRebirth ? "Ciclo\nListo" : "Ciclo";
            achievementsLabel.text = "Logros" + (game.AvailableAchievements > 0 ? "\n" + game.AvailableAchievements : "");
            storyLabel.text = game.CampaignTabAvailable(5) ? "Orfeo" + (game.ExpeditionReady || game.PendingOfflineEnergy > 0 || game.PendingStationMemory >= 0 ? "\nListo" : "")
                : "Archivo" + (game.UnreadStory > 0 ? "\nNuevo" : "");
            ModulePanel.Refresh(); Progression.Refresh(); Achievements.Refresh(); Story.Refresh(); Station.Refresh();
            tapHint.text = game.UnreadStory > 0 ? "NARA · NUEVA TRANSMISIÓN" : "MISIÓN · " + NarrativeMission(game.StoryCompleted);
            milestone.text = game.StoryCompleted == StoryCatalog.Count ? "Archivo recuperado · " + game.StoryRead + " / 6 leídos"
                : NumberFormat.Compact(game.AllTimeEnergy) + " E · " + (game.StoryProgressFraction(game.CurrentStoryChapter) * 100).ToString("0") + "% · VER OBJETIVO";
            if (game.CampaignActive)
            {
                tapHint.text = (game.CampaignRequirementsMet ? "LISTO · " : "MISIÓN · ") + game.CampaignAction;
                milestone.text = game.CampaignHint;
                StoryButton.image.color = game.CampaignRequirementsMet ? new Color(.035f, .19f, .21f, .97f) : new Color(.035f, .08f, .12f, .94f);
            }
            if (transmission != null) milestone.text = transmission;
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
                shopLayoutDirty = true;
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
                    + "\n<color=#BBCADD>" + description + "\n" + effect + "</color>";
                shopPrices[i].text = !unlocked ? price : level >= EconomyConfig.MaxLevel ? "MÁXIMO"
                    : (affordable ? "COMPRAR" : "NECESITAS") + "\n" + price;

            }
            if (Time.unscaledTime > noticeUntil) notice.text = SaveFailed ? "GUARDADO PENDIENTE" : "JUEGO SIN CONEXIÓN";
        }

        private void LayoutShop()
        {
            float width = shopPage.rect.width;
            if (!shopLayoutDirty && Mathf.Approximately(width, shopLayoutWidth)) return;
            shopLayoutDirty = false; shopLayoutWidth = width;
            float top = 0;
            for (int i = 0; i < shopRows.Length; i++)
            {
                var row = shopRows[i];
                if (!row.gameObject.activeSelf) continue;
                float height = Mathf.Max(180, shopText[i].GetPreferredValues(shopText[i].text,
                    Mathf.Max(1, shopText[i].rectTransform.rect.width), Mathf.Infinity).y);
                shopText[i].rectTransform.sizeDelta = new Vector2(0, height);
                row.sizeDelta = new Vector2(0, height + 224);
                row.anchoredPosition = new Vector2(0, -top);
                top += row.sizeDelta.y + 24;
            }
            if (shopRows.Length > 0) ((RectTransform)shopRows[0].parent).sizeDelta = new Vector2(0, top);
        }

        private static readonly string[] missionNames = { "SOPORTE VITAL", "COMUNICACIONES", "SECTOR ROJO", "CAJA NEGRA", "ENLACE DE MEMORIA", "NUEVO ENCENDIDO", "ORFEO RECUERDA" };
        public static string NarrativeMission(int chapter) => missionNames[Mathf.Clamp(chapter, 0, missionNames.Length - 1)];
        public void ShowTransmission(string name, string text, Sprite avatar)
        {
            transmission = "<color=#74E6F2>" + name + " · </color>" + text;
            rewardCard.gameObject.SetActive(false); StoryButton.gameObject.SetActive(true);
            tapHint.gameObject.SetActive(false);
            milestone.text = transmission;
        }
        public void ClearTransmission()
        {
            transmission = null;
            if (tapHint == null) return;
            tapHint.gameObject.SetActive(true);
            nextTextUpdate = 0;
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
            if (size <= 42) size *= 1.35f;
            size = Mathf.Max(MinimumFontSize, size);
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = MinimumFontSize;
            label.fontSizeMax = size;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        // Long text grows inside its own bounded viewport instead of shrinking or hiding words.
        public TextMeshProUGUI ScrollableLabel(string name, Transform parent, string text, float size, Color color,
            float x0, float y0, float x1, float y1)
        {
            var viewport = Rect(name + "Viewport", parent, x0, y0, x1, y1);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            var label = Label(name, viewport, text, size, color, 0, 1, 1, 1, TextAlignmentOptions.TopLeft);
            label.enableAutoSizing = false;
            label.rectTransform.pivot = new Vector2(.5f, 1);
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = label.rectTransform;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            label.rectTransform.offsetMax = new Vector2(-28, label.rectTransform.offsetMax.y);
            var track = Image(name + "ScrollTrack", viewport, new Color(.08f, .13f, .19f), 1, 0, 1, 1);
            track.rectTransform.pivot = new Vector2(1, .5f);
            track.rectTransform.sizeDelta = new Vector2(18, 0);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            var handle = Image("Handle", track.transform, Cyan, 0, 0, 1, 1);
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
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
