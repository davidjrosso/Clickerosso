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
        private TextMeshProUGUI cycle, tapGain, instruction, teaser, missionCtaLabel;
        private Image comboChip, missionAvatar, missionCta;
        private RingGraphic missionAvatarRing;
        private RectTransform missionFill;
        private Image[] tabIcons;
        private GameObject[] tabBadges;
        private TextMeshProUGUI[] tabBadgeLabels;
        private Sprite roundSprite;
        private Texture2D roundTexture;
        private bool ownsDisplayFont;
        private float cycleGlitchAt = 9;
        private int lastMission = -1;
        private NarrativeCatalog narrativeTeasers;
        public TMP_FontAsset DisplayFont { get; private set; }
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
            DisplayFont = Resources.Load<TMP_FontAsset>("Fonts/ReactorDisplayFont");
            if (DisplayFont == null && Resources.Load<Font>(ReactorFont.DisplaySource) != null)
            {
                try { DisplayFont = ReactorFont.CreateRuntimeFont(ReactorFont.DisplaySource, "ReactorDisplayFont Runtime"); ownsDisplayFont = true; }
                catch (InvalidOperationException) { DisplayFont = null; }
            }
            CreateRoundSprite();
            var teaserData = Resources.Load<TextAsset>("Narrative");
            if (teaserData != null) narrativeTeasers = JsonUtility.FromJson<NarrativeCatalog>(teaserData.text);
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
            Image("Header", safe, new Color(.01f, .02f, .04f, .62f), 0, .812f, 1, 1);
            Image("HeaderRule", safe, new Color(.18f, .92f, 1, .25f), .065f, .811f, .935f, .813f);
            Round(Image("CycleChipBack", safe, new Color(1, .24f, .67f, .12f), .045f, .948f, .36f, .984f));
            cycle = Label("CycleChip", safe, "CICLO 01", 22, Magenta, .045f, .948f, .36f, .984f);
            energy = Label("Energy", safe, "0", 96, Color.white, .20f, .862f, .80f, .944f);
            Round(Image("RateChip", safe, new Color(.18f, .92f, 1, .10f), .06f, .818f, .36f, .857f));
            rate = Label("Rate", safe, "0 / s", 26, Cyan, .06f, .818f, .36f, .857f);
            comboChip = Image("ComboChip", safe, new Color(.68f, .39f, 1, .14f), .38f, .818f, .94f, .857f);
            Round(comboChip);
            combo = Label("Combo", safe, "COMBO ×1", 26, Violet, .38f, .818f, .94f, .857f);
            reactorPage = Rect("ReactorPage", safe, 0, .145f, 1, .815f);
            shopPage = Rect("EngineeringPage", safe, .035f, .145f, .965f, .815f);
            Round(Image("EngineeringSurface", shopPage, new Color(.02f, .035f, .06f, .90f), 0, 0, 1, 1));
            stage = Label("Stage", reactorPage, "", 22, Warm, .045f, .955f, .45f, .995f, TextAlignmentOptions.Left);
            tapGain = Label("TapGain", reactorPage, "", 22, Muted, .55f, .955f, .955f, .995f, TextAlignmentOptions.Right);

            RectTransform reactorViewport = Rect("ReactorViewport", reactorPage, 0, .36f, 1, .995f);
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
            instruction = Label("ReactorInstruction", reactorViewport, "TOCA EL NÚCLEO", 23, Color.white, .22f, .245f, .78f, .30f);
            EffectsRoot = Rect("Effects", reactorViewport, 0, 0, 1, 1);
            // Mission card: who speaks, what is missing, the next corrupted line and how close it is.
            StoryButton = Button("OpenStory", reactorPage, new Color(.035f, .06f, .10f, .96f), .03f, .015f, .97f, .222f);
            Round(StoryButton.image);
            StoryButton.onClick.AddListener(app.CampaignAction);
            missionAvatarRing = Ring("MissionAvatarRing", StoryButton.transform, Warm);
            missionAvatarRing.rectTransform.anchorMin = new Vector2(.02f, .46f); missionAvatarRing.rectTransform.anchorMax = new Vector2(.16f, .96f);
            missionAvatarRing.thickness = 3; missionAvatarRing.fraction = 1;
            missionAvatar = Image("MissionAvatar", missionAvatarRing.transform, Color.white, .08f, .08f, .92f, .92f);
            missionAvatar.preserveAspect = true;
            tapHint = Label("TapHint", StoryButton.transform, "NARA · VER HISTORIA", 22, Muted, .035f, .48f, .965f, .95f);
            milestone = Label("Milestone", StoryButton.transform, "", 26, Color.white, .035f, .05f, .965f, .48f);
            teaser = Label("MissionTeaser", StoryButton.transform, "", 26, Warm, .035f, .05f, .965f, .48f);
            var missionViewport = Rect("MissionViewport", StoryButton.transform, .18f, .30f, .975f, .96f);
            missionViewport.gameObject.AddComponent<RectMask2D>();
            missionViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            var missionContent = Rect("MissionContent", missionViewport, 0, 1, 1, 1);
            missionContent.pivot = new Vector2(.5f, 1);
            var missionLayout = missionContent.gameObject.AddComponent<VerticalLayoutGroup>();
            missionLayout.spacing = 6;
            missionLayout.childControlWidth = missionLayout.childControlHeight = true;
            missionLayout.childForceExpandHeight = false;
            missionContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tapHint.transform.SetParent(missionContent, false); milestone.transform.SetParent(missionContent, false);
            teaser.transform.SetParent(missionContent, false);
            tapHint.enableAutoSizing = milestone.enableAutoSizing = teaser.enableAutoSizing = false;
            tapHint.alignment = milestone.alignment = teaser.alignment = TextAlignmentOptions.TopLeft;
            if (DisplayFont != null) tapHint.font = DisplayFont;
            tapHint.characterSpacing = 4;
            var missionScroll = missionViewport.gameObject.AddComponent<ScrollRect>();
            missionScroll.viewport = missionViewport; missionScroll.content = missionContent;
            missionScroll.horizontal = false; missionScroll.movementType = ScrollRect.MovementType.Clamped;
            Round(Image("MissionTrack", StoryButton.transform, new Color(.06f, .09f, .15f), .03f, .215f, .97f, .255f));
            missionFill = Image("MissionFill", StoryButton.transform, Cyan, .03f, .215f, .03f, .255f).rectTransform;
            Round(missionFill.GetComponent<Image>());
            missionCta = Image("MissionCta", StoryButton.transform, new Color(.07f, .10f, .17f), .03f, .03f, .97f, .19f);
            Round(missionCta);
            missionCtaLabel = Label("MissionCtaLabel", missionCta.transform, "", 24, Muted, .02f, .02f, .98f, .98f);
            if (DisplayFont != null) missionCtaLabel.font = DisplayFont;
            OverloadButton = Button("ActivateOverload", reactorPage, new Color(.10f, .05f, .16f, .98f), .045f, .23f, .955f, .358f);
            Round(OverloadButton.image);
            OverloadButton.onClick.AddListener(app.ActivateOverload);
            Image track = Image("ChargeTrack", OverloadButton.transform, new Color(0, 0, 0, 0), 0, 0, 1, 1);
            chargeFill = Image("Charge", track.transform, new Color(1, .24f, .67f, .32f), 0, 0, 0, 1).rectTransform;
            Round(chargeFill.GetComponent<Image>());
            var overloadIcon = Image("OverloadIcon", OverloadButton.transform, Color.white, .015f, .08f, .11f, .92f);
            overloadIcon.sprite = sprites.Icon("overload"); overloadIcon.preserveAspect = true;
            overloadLabel = Label("OverloadLabel", OverloadButton.transform, "", 29, Color.white, .12f, .1f, .98f, .94f);
            if (DisplayFont != null) overloadLabel.font = DisplayFont;
            Label("ShopTitle", shopPage, "MEJORAS", 38, Color.white, .04f, .925f, .96f, .985f, TextAlignmentOptions.Left).font = DisplayFont ?? Font;
            ScrollableLabel("ShopHelp", shopPage, "Toca una tarjeta para ver detalles. Solo el precio compra.", 26, Muted, .04f, .84f, .96f, .925f);
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
                var rowImage = Image("ShopRow_" + entry.Id, content, Panel, 0, 1, 1, 1);
                Round(rowImage);
                var row = rowImage.rectTransform;
                shopRows[i] = row; row.pivot = new Vector2(.5f, 1);
                row.sizeDelta = new Vector2(0, 260);
                var details = Button("Details_" + entry.Id, row, Color.clear, 0, 0, 1, 1);
                details.onClick.AddListener(() => { SelectTab(0); OpenModule(entry.Id); });
                var socket = Rect("IconSocket", row, .015f, .5f, .175f, .5f).gameObject.AddComponent<SocketGraphic>();
                socket.raycastTarget = false;
                socket.rectTransform.sizeDelta = new Vector2(0, 170);
                Image icon = Image("Icon", row, Color.white, .03f, .5f, .16f, .5f);
                icon.rectTransform.sizeDelta = new Vector2(0, 130);
                icon.sprite = sprites.Icon(entry.Id); icon.preserveAspect = true;
                shopText[i] = Label("Description", row, "", 34, Color.white, .19f, 1, .70f, 1, TextAlignmentOptions.TopLeft);
                shopText[i].enableAutoSizing = false;
                shopText[i].rectTransform.pivot = new Vector2(.5f, 1);
                shopText[i].rectTransform.anchoredPosition = new Vector2(0, -34);
                Button button = Button("Buy" + entry.Id, row, new Color(.08f, .15f, .20f), .715f, .5f, .975f, .5f);
                Round(button.image);
                var buttonRect = (RectTransform)button.transform;
                buttonRect.sizeDelta = new Vector2(0, 164);
                button.onClick.AddListener(() => app.Buy(entry.Id));
                ShopButtons[i] = button;
                shopPrices[i] = Label("PurchasePrice", button.transform, "", 34, Warm, .04f, .06f, .96f, .94f);
                if (DisplayFont != null) shopPrices[i].font = DisplayFont;
            }
            notice = ScrollableLabel("Notice", safe, "", 20, Warm, .045f, .108f, .955f, .14f);
            Round(Image("NavigationBar", safe, new Color(.02f, .035f, .065f, .94f), .02f, .008f, .98f, .112f));
            NavigationButtons = new Button[5]; tabIndicators = new Image[5]; tabIcons = new Image[5]; tabBadges = new GameObject[5]; tabBadgeLabels = new TextMeshProUGUI[5];
            string[] tabNames = { "Núcleo", "Mejoras", "Logros", "Ciclo", "Archivo" };
            string[] tabIconIds = { "tap", "coil", "reward", "combo", "memory" };
            for (int i = 0; i < 5; i++)
            {
                int tab = i;
                int column = i < 2 ? i : i == 4 ? 2 : i + 1;
                var button = Button("Navigation" + i, safe, new Color(.06f, .12f, .19f, 0), .035f + column * .186f, .015f, .211f + column * .186f, .105f);
                Round(button.image);
                NavigationButtons[i] = button;
                tabIcons[i] = Image("NavigationIcon", button.transform, Color.white, .25f, .44f, .75f, .93f);
                tabIcons[i].sprite = i == 4 ? NarrativeArt.Portrait("memory") : sprites.Icon(tabIconIds[i]);
                tabIcons[i].preserveAspect = true;
                var text = Label("NavigationLabel", button.transform, tabNames[i], 20, Color.white, .01f, .03f, .99f, .44f);
                if (DisplayFont != null) text.font = DisplayFont;
                tabIndicators[i] = Image("SelectedTab", button.transform, Cyan, .25f, .95f, .75f, 1);
                var badge = Image("NavigationBadge", button.transform, Magenta, .62f, .66f, .92f, .98f);
                Round(badge); tabBadges[i] = badge.gameObject;
                tabBadgeLabels[i] = Label("NavigationBadgeLabel", badge.transform, "", 18, Color.white, 0, 0, 1, 1);
                badge.gameObject.SetActive(false);
                button.onClick.AddListener(() => SelectTab(tab == 4 && owner.Game.CampaignTabAvailable(5) ? 5 : tab));
                if (i == 2) achievementsLabel = text;
                if (i == 4) storyLabel = text;
                if (i == 3) { ProgressionButton = button; progressionLabel = text; }
            }
            Image card = Image("RewardCard", reactorPage, Panel, .03f, .015f, .97f, .222f);
            Round(card);
            rewardCard = card.gameObject.AddComponent<CanvasGroup>();
            rewardCard.blocksRaycasts = false;
            Image rewardIcon = Image("RewardIcon", card.transform, Color.white, .015f, .04f, .14f, .96f);
            rewardIcon.sprite = sprites.Icon("reward"); rewardIcon.preserveAspect = true;
            rewardText = Label("RewardText", card.transform, "", 26, Warm, .16f, .03f, .98f, .97f, TextAlignmentOptions.Left);
            rewardCard.gameObject.SetActive(false);
            SettingsButton = Button("Settings", safe, Panel, .80f, .893f, .965f, .985f);
            Round(SettingsButton.image);
            Image settingsIcon = Image("SettingsIcon", SettingsButton.transform, Color.white, .2f, .14f, .8f, .86f);
            settingsIcon.sprite = sprites.Icon("settings"); settingsIcon.preserveAspect = true;
            if (DisplayFont != null) { energy.font = DisplayFont; cycle.font = DisplayFont; }
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
            if (ownsDisplayFont) ReactorFont.Release(DisplayFont);
            if (roundSprite != null) Destroy(roundSprite);
            if (roundTexture != null) Destroy(roundTexture);
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
                bool selected = i == (tab == 5 ? 4 : tab);
                tabIndicators[i].gameObject.SetActive(selected);
                NavigationButtons[i].image.color = selected ? new Color(.06f, .14f, .21f, .95f) : new Color(.06f, .12f, .19f, 0);
                tabIcons[i].color = selected ? Color.white : new Color(1, 1, 1, .5f);
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

        // Before NARA's confession the counter briefly shows the real attempt; reduced flashes keep it steady.
        private string CycleText(GameEngine game)
        {
            bool known = game.StoryCompleted >= 5 || game.RebirthCount > 0;
            int number = known ? 47 + game.RebirthCount : 1;
            if (!known && game.NarrativeIntroSeen && !Preferences.reducedFlashes && Time.unscaledTime >= cycleGlitchAt)
            {
                if (Time.unscaledTime < cycleGlitchAt + .14f) return "CICLO 47";
                cycleGlitchAt = Time.unscaledTime + 11 + (Time.frameCount % 5);
            }
            return "CICLO " + number.ToString("00");
        }
        private void Update()
        {
            if (cycle != null && owner != null) cycle.text = CycleText(owner.Game);
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
            ((RectTransform)StoryButton.transform).anchorMax = new Vector2(.97f, game.OverloadAvailable ? .222f : .345f);
            ((RectTransform)rewardCard.transform).anchorMax = ((RectTransform)StoryButton.transform).anchorMax;
            if (game.Overload.Ready && !game.Overload.Active && game.OverloadAvailable && !Preferences.reducedFlashes)
                OverloadButton.image.color = Color.Lerp(new Color(.16f, .05f, .2f, .98f), new Color(.36f, .07f, .3f, .98f), .5f + .5f * Mathf.Sin(Time.unscaledTime * 4));
            else OverloadButton.image.color = game.Overload.Active ? new Color(.36f, .07f, .3f, .98f) : new Color(.10f, .05f, .16f, .98f);
            combo.gameObject.SetActive(game.ComboAvailable); comboChip.gameObject.SetActive(game.ComboAvailable);
            instruction.gameObject.SetActive(game.Lifetime < 30);
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
            int affordableCount = 0;
            for (int i = 0; i < ShopButtons.Length; i++)
            {
                bool can = game.CanBuy(game.Shop[i].Id);
                ShopButtons[i].interactable = can;
                ShopButtons[i].image.color = can ? Warm : new Color(.08f, .12f, .19f);
                shopPrices[i].color = can ? new Color(.12f, .06f, 0) : Warm;
                if (can && shopRows[i].gameObject.activeSelf) affordableCount++;
            }
            SetBadge(1, affordableCount > 0 ? affordableCount.ToString() : null);
            SetBadge(2, game.AvailableAchievements > 0 ? game.AvailableAchievements.ToString() : null);
            SetBadge(3, game.CanRebirth ? "!" : null);
            SetBadge(4, (game.CampaignTabAvailable(5) ? game.ExpeditionReady || game.PendingOfflineEnergy > 0 || game.PendingStationMemory >= 0 : game.UnreadStory > 0) ? "!" : null);
            if (!force && Time.unscaledTime < nextTextUpdate) return;
            nextTextUpdate = Time.unscaledTime + .08f;
            energy.text = game.EnergyLimitReached ? "LÍMITE · " + NumberFormat.Compact(game.Energy) : NumberFormat.Compact(game.Energy);
            rate.text = NumberFormat.Stat(game.Production) + " / s";
            tapGain.text = "+" + NumberFormat.Stat(game.CurrentTapGain) + " / toque";
            combo.text = "COMBO ×" + NumberFormat.Stat(game.ComboMultiplier)
                + (game.Combo.Decaying ? " · BAJANDO" : game.ComboMultiplier >= game.ComboCap ? " · MÁX." : " · " + game.Combo.Count);
            combo.color = game.Combo.Decaying ? Muted : Cyan;
            if (game.Stage != lastStage)
            {
                lastStage = game.Stage;
                stage.text = shortStages[Mathf.Clamp(game.Stage, 0, shortStages.Length - 1)].ToUpperInvariant();
            }
            progressionLabel.text = "Ciclo";
            achievementsLabel.text = "Logros";
            storyLabel.text = game.CampaignTabAvailable(5) ? "Orfeo" : "Archivo";
            ModulePanel.Refresh(); Progression.Refresh(); Achievements.Refresh(); Story.Refresh(); Station.Refresh();
            tapHint.text = game.UnreadStory > 0 ? "NARA · NUEVA TRANSMISIÓN" : "MISIÓN · " + NarrativeMission(game.StoryCompleted);
            milestone.text = game.StoryCompleted == StoryCatalog.Count ? "Archivo recuperado · " + game.StoryRead + " / 6 leídos"
                : NumberFormat.Compact(game.AllTimeEnergy) + " E · " + (game.StoryProgressFraction(game.CurrentStoryChapter) * 100).ToString("0") + "% · VER OBJETIVO";
            RefreshMission(game);
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
                double current = game.EffectAtLevel(item, level);
                double next = game.EffectAtLevel(item, Math.Min(EconomyConfig.MaxLevel, level + 1));
                string effect = NumberFormat.Stat(current) + " a " + NumberFormat.Stat(next) + " " + entry.Unit;
                if (level >= EconomyConfig.MaxLevel) effect = NumberFormat.Stat(current) + " " + entry.Unit + " · MÁXIMO";
                // Compact card: name, level and before/after effect. The full description lives in the module sheet.
                shopText[i].text = entry.Name + " <color=#74E6F2>· NV. " + level + "</color>"
                    + "\n<color=#7CF2B0>" + (unlocked ? effect : "Requiere " + game.UnlockDescription(item)) + "</color>";
                double wait = game.Production > 0 && cost > game.Energy ? (cost - game.Energy) / game.Production : -1;
                shopPrices[i].text = !unlocked ? "—" : level >= EconomyConfig.MaxLevel ? "MÁXIMO"
                    : affordable ? price : price + "\n<size=70%>" + (wait > 0 && wait < 360000 ? "en " + NarrativeText.Duration(wait) : "falta energía") + "</size>";

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
                float height = Mathf.Max(150, shopText[i].GetPreferredValues(shopText[i].text,
                    Mathf.Max(1, shopText[i].rectTransform.rect.width), Mathf.Infinity).y);
                shopText[i].rectTransform.sizeDelta = new Vector2(0, height);
                row.sizeDelta = new Vector2(0, Mathf.Max(230, height + 68));
                row.anchoredPosition = new Vector2(0, -top);
                top += row.sizeDelta.y + 18;
            }
            if (shopRows.Length > 0) ((RectTransform)shopRows[0].parent).sizeDelta = new Vector2(0, top);
        }

        private static readonly string[] missionNames = { "SOPORTE VITAL", "COMUNICACIONES", "SECTOR ROJO", "CAJA NEGRA", "ENLACE DE MEMORIA", "EL ÚLTIMO ENCENDIDO", "ORFEO RECUERDA" };
        private static readonly string[] missionActs = { "ACTO I", "ACTO I", "ACTO II", "ACTO II", "ACTO III", "ACTO III", "EPÍLOGO" };
        private static readonly string[] missionVoices = { "nara", "mara", "mara", "nara", "nara", "nara", "mara" };
        private void SetBadge(int tab, string text)
        {
            tabBadges[tab].SetActive(text != null && NavigationButtons[tab].gameObject.activeSelf);
            if (text != null) tabBadgeLabels[tab].text = text;
        }
        private void RefreshMission(GameEngine game)
        {
            int mission = Mathf.Clamp(game.StoryCompleted, 0, missionNames.Length - 1);
            if (mission != lastMission)
            {
                lastMission = mission;
                missionAvatar.sprite = NarrativeArt.Portrait(missionVoices[mission]);
                missionAvatarRing.color = missionVoices[mission] == "mara" ? Warm : Cyan;
            }
            double progress = game.StoryProgressFraction(game.CurrentStoryChapter);
            if (game.StoryCompleted >= StoryCatalog.Count) progress = 1;
            missionFill.anchorMax = new Vector2(.03f + .94f * (float)Math.Max(0, Math.Min(1, progress)), .255f);
            string act = missionActs[mission];
            if (!game.CampaignActive)
            {
                tapHint.text = act + " · " + tapHint.text; teaser.text = "";
                missionCtaLabel.text = "VER HISTORIA"; missionCta.color = new Color(.07f, .10f, .17f); missionCtaLabel.color = Muted;
                return;
            }
            bool ready = game.CampaignRequirementsMet || game.CampaignMissionReady;
            tapHint.text = "MISIÓN " + (mission + 1) + " · " + game.CampaignAction;
            milestone.text = ready ? "<color=#FFB045>Todo preparado.</color>" : game.CampaignHint;
            NarrativeScene next = game.StoryCompleted < StoryCatalog.Count ? narrativeTeasers?.Find("chapter_" + game.StoryCompleted) : null;
            teaser.text = next != null && !string.IsNullOrEmpty(next.teaser) && game.NarrativeIntroSeen ? NarrativeText.Corrupted(next.teaser, ready ? 1 : progress) : "";
            teaser.gameObject.SetActive(teaser.text.Length > 0);
            string eta = "";
            if (!ready && game.StoryCompleted < StoryCatalog.Count && game.Production > 0)
            {
                double remaining = StoryCatalog.At(game.StoryCompleted).Energy - game.AllTimeEnergy;
                if (remaining > 0) eta = "PRÓXIMA TRANSMISIÓN EN " + NarrativeText.Duration(remaining / game.Production);
            }
            missionCtaLabel.text = ready ? game.CampaignAction + " · TOCA" : eta.Length > 0 ? eta : (progress * 100).ToString("0") + "% · " + missionNames[mission];
            missionCta.color = ready ? Warm : new Color(.07f, .10f, .17f);
            missionCtaLabel.color = ready ? new Color(.12f, .06f, 0) : Muted;
            StoryButton.image.color = ready ? new Color(.06f, .09f, .12f, .97f) : new Color(.035f, .06f, .10f, .96f);
        }
        // One 9-sliced rounded texture shared by chips, cards and buttons; created once per HUD.
        private void CreateRoundSprite()
        {
            const int size = 64; const float radius = 30;
            roundTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "RoundedPanel" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x - .5f, x + .5f - (size - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y - .5f, y + .5f - (size - radius)));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            roundTexture.SetPixels32(pixels); roundTexture.Apply(false, true);
            roundSprite = Sprite.Create(roundTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(31, 31, 31, 31));
            roundSprite.name = "RoundedPanel";
        }
        internal Image Round(Image image, float radius = 1)
        {
            if (image == null || roundSprite == null) return image;
            image.sprite = roundSprite; image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.6f / radius;
            return image;
        }
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
