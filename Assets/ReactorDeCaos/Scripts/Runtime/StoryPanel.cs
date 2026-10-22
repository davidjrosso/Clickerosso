using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Reading never pauses production. No typewriter delays or blocking chapter popups.
    public sealed class StoryPanel
    {
        private readonly ReactorApp app;
        private readonly GameObject root;
        private readonly TextMeshProUGUI heading, status, objectives, narrative, reward, actionText, message, choiceText;
        private readonly ScrollRect scroll;
        private readonly RectTransform progress;
        private readonly GameObject choices;
        public Button PreviousButton { get; }
        public Button NextButton { get; }
        public Button ActionButton { get; }
        public Button NaraButton { get; }
        public Button CoreButton { get; }
        public bool IsOpen => root.activeSelf;
        public int Chapter { get; private set; }
        private int actionFrame = -1;
        private int LastPage => app.Game.StoryEpilogueAvailable ? StoryCatalog.Count : app.Game.CurrentStoryChapter;

        public StoryPanel(ReactorHud hud, Transform parent, ReactorApp owner)
        {
            app = owner;
            var surface = ReactorHud.Image("StoryPage", parent, new Color(.02f, .035f, .06f, .98f), .035f, .145f, .965f, .815f);
            surface.raycastTarget = true; root = surface.gameObject;
            heading = hud.Label("StoryHeading", surface.transform, "ARCHIVO DE NARA", 34, Color.white, .04f, .925f, .96f, .985f, TextAlignmentOptions.Left);
            status = hud.Label("StoryStatus", surface.transform, "", 26, ReactorHud.Cyan, .04f, .865f, .96f, .925f, TextAlignmentOptions.Left);
            var viewport = ReactorHud.Rect("StoryViewport", surface.transform, .025f, .33f, .975f, .855f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            var content = ReactorHud.Rect("StoryContent", viewport, 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 12, 32); layout.spacing = 28;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;

            var banner = ReactorHud.Rect("NaraTransmission", content, 0, 0, 1, 1);
            banner.gameObject.AddComponent<LayoutElement>().preferredHeight = 420;
            banner.gameObject.AddComponent<RectMask2D>();
            var illustration = ReactorHud.Rect("StoryIllustration", banner, 0, 0, 1, 1);
            var image = illustration.gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("Art/StorySignal"); image.raycastTarget = false;
            var fit = illustration.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 1.5f;
            ReactorHud.Image("TransmissionShade", banner, new Color(.01f, .02f, .04f, .72f), 0, 0, 1, .26f);
            hud.Label("TransmissionCaption", banner, "ORFEO · ECOS DEL PRIMER ENCENDIDO", 26, ReactorHud.Cyan, .04f, .025f, .96f, .24f, TextAlignmentOptions.Left);
            objectives = Text(hud, "StoryObjectives", content, ReactorHud.Cyan);
            var track = ReactorHud.Image("StoryTrack", content, new Color(.08f, .13f, .19f), 0, 0, 1, 1);
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 8;
            progress = ReactorHud.Image("StoryProgress", track.transform, ReactorHud.Cyan, 0, 0, 0, 1).rectTransform;
            narrative = Text(hud, "StoryNarrative", content, new Color(.87f, .92f, .98f));
            reward = Text(hud, "StoryReward", content, ReactorHud.Warm);
            choices = ReactorHud.Rect("StoryChoices", content, 0, 0, 1, 1).gameObject;
            choices.AddComponent<LayoutElement>().preferredHeight = 720;
            choiceText = hud.Label("StoryChoiceResponse", choices.transform, "", 30, Color.white, .025f, .52f, .975f, 1, TextAlignmentOptions.Left);
            NaraButton = ReactorHud.Button("StoryChooseNara", choices.transform, new Color(.045f, .18f, .23f), .02f, .26f, .98f, .49f);
            hud.Label("NaraChoiceLabel", NaraButton.transform, "SINTONIZAR CON NARA\nLuz cian · mismos beneficios", 28, Color.white, .02f, .08f, .98f, .92f);
            CoreButton = ReactorHud.Button("StoryChooseCore", choices.transform, new Color(.19f, .08f, .24f), .02f, 0, .98f, .23f);
            hud.Label("CoreChoiceLabel", CoreButton.transform, "ESCUCHAR AL NÚCLEO\nLuz violeta · mismos beneficios", 28, Color.white, .02f, .08f, .98f, .92f);
            NaraButton.onClick.AddListener(() => app.ChooseStory(1)); CoreButton.onClick.AddListener(() => app.ChooseStory(2));
            message = Text(hud, "StoryMessage", content, new Color(.64f, .73f, .83f));
            PreviousButton = ReactorHud.Button("StoryPrevious", surface.transform, new Color(.05f, .10f, .16f), .04f, .175f, .48f, .315f);
            NextButton = ReactorHud.Button("StoryNext", surface.transform, new Color(.05f, .10f, .16f), .52f, .175f, .96f, .315f);
            hud.Label("StoryPreviousLabel", PreviousButton.transform, "ANTERIOR", 28, Color.white, .02f, .08f, .98f, .92f);
            hud.Label("StoryNextLabel", NextButton.transform, "SIGUIENTE", 28, Color.white, .02f, .08f, .98f, .92f);
            PreviousButton.onClick.AddListener(() => ShowChapter(Chapter - 1));
            NextButton.onClick.AddListener(() => ShowChapter(Chapter + 1));
            ActionButton = ReactorHud.Button("StoryAction", surface.transform, new Color(.06f, .20f, .24f), .04f, .02f, .96f, .16f);
            actionText = hud.Label("StoryActionLabel", ActionButton.transform, "", 29, Color.white, .025f, .08f, .975f, .92f);
            ActionButton.onClick.AddListener(Act);
            root.SetActive(false);
        }
        private static TextMeshProUGUI Text(ReactorHud hud, string name, Transform parent, Color color)
        {
            var text = hud.Label(name, parent, "", 30, color, 0, 0, 1, 1, TextAlignmentOptions.TopLeft);
            text.enableAutoSizing = false; text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
        public void Open()
        {
            root.SetActive(true);
            ShowChapter(Mathf.Min(app.Game.StoryRead, LastPage));
        }
        public void Close() { root.SetActive(false); }
        public void ShowMessage(string value) { message.text = value; }
        public void ShowChapter(int chapter)
        {
            Chapter = Mathf.Clamp(chapter, 0, LastPage);
            message.text = "Desliza para leer. La producción continúa.\nLos objetivos se completan jugando; leer es opcional.\nLos planos conservan el acceso, no las máquinas. Tras ascender, empieza por módulos económicos.";
            Refresh(); Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        }
        private void Act()
        {
            if (actionFrame == Time.frameCount) return;
            actionFrame = Time.frameCount;
            var game = app.Game;
            if (Chapter > game.StoryRead) { ShowChapter(game.StoryRead); return; }
            if (Chapter == StoryCatalog.Count)
            {
                if (!game.StoryEpilogueRead) app.ReadStory(Chapter);
                else app.Hud.SelectTab(0);
            }
            else if (Chapter == game.StoryRead && Chapter < game.StoryCompleted)
            {
                if (app.ReadStory(Chapter)) ShowChapter(Mathf.Min(Chapter + 1, LastPage));
            }
            else if (game.StoryRead == StoryCatalog.Count) app.Hud.SelectTab(3);
            else app.Hud.SelectTab(0);
        }
        public void Refresh()
        {
            if (!IsOpen) return;
            var game = app.Game;
            bool epilogue = Chapter == StoryCatalog.Count;
            bool done = Chapter < game.StoryCompleted;
            heading.text = epilogue ? "DESPUÉS DEL RENACIMIENTO" : "ARCHIVO DE NARA · " + (Chapter + 1) + " / " + StoryCatalog.Count;
            status.text = epilogue ? "EPÍLOGO · MEMORIA RESTAURADA" : (done ? "COMPLETADO" : "EN CURSO") + " · DESLIZA PARA LEER";
            if (epilogue)
            {
                objectives.text = "ECOS DEL PRIMER ENCENDIDO\nArco completado · tu memoria ha sobrevivido";
                narrative.text = StoryCatalog.Epilogue(game.StoryChoice);
                reward.text = "Los planos, el archivo y la sintonía permanecen contigo en cada ciclo.";
            }
            else
            {
                var chapter = StoryCatalog.At(Chapter);
                objectives.text = chapter.Title.ToUpperInvariant() + "\n\n" + game.StoryObjective(Chapter);
                narrative.text = chapter.Opening + (done ? "\n\nREGISTRO RECUPERADO\n\n" + chapter.Resolution : "\n\nLa siguiente transmisión se recuperará al completar los objetivos.");
                reward.text = (done ? "RECOMPENSA OBTENIDA\n" : "AL COMPLETAR\n") + chapter.Reward;
            }
            progress.anchorMax = new Vector2(epilogue ? 1 : (float)game.StoryProgressFraction(Chapter), 1);
            choices.SetActive(Chapter == 4 && done);
            choiceText.text = (game.StoryChoice == 0 ? "SINTONÍA OPCIONAL\n" : "SINTONÍA GUARDADA · puedes cambiarla\n") + StoryCatalog.ChoiceResponse(game.StoryChoice);
            NaraButton.interactable = game.StoryChoice != 1; CoreButton.interactable = game.StoryChoice != 2;
            PreviousButton.interactable = Chapter > 0; NextButton.interactable = Chapter < LastPage;
            actionText.text = Chapter > game.StoryRead ? "LEER REGISTRO PENDIENTE"
                : epilogue ? game.StoryEpilogueRead ? "VOLVER AL REACTOR" : "ARCHIVAR EPÍLOGO"
                : Chapter == game.StoryRead && done ? "ARCHIVAR Y CONTINUAR"
                : game.StoryRead == StoryCatalog.Count ? "VER ASCENSIÓN" : "VOLVER AL REACTOR";
        }
    }
}
