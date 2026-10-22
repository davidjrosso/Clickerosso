using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class RebirthPanel
    {
        private readonly ReactorApp app;
        private readonly GameObject root;
        private readonly GameObject[] pages = new GameObject[3];
        private readonly RectTransform[] pageContents = new RectTransform[3];
        private readonly TextMeshProUGUI summary, cycle, currentBranch, armLabel, message, confirmationText;
        private readonly TextMeshProUGUI[] branchLabels = new TextMeshProUGUI[3], masteryLabels = new TextMeshProUGUI[3], skinLabels = new TextMeshProUGUI[4];
        private readonly Button[] branches = new Button[3], skins = new Button[4];
        private readonly GameObject confirmation;
        private readonly ScrollRect confirmationScroll;
        private readonly RectTransform requirementFill;
        private readonly Button[] tabs = new Button[3];
        private Specialization selected = Specialization.Tap;
        private int quotedReward, quotedCycle;
        public Button ArmButton { get; }
        public Button ConfirmButton { get; }
        public Button[] MasteryButtons { get; } = new Button[3];
        public bool IsOpen => root.activeSelf;
        public bool IsConfirming => confirmation.activeSelf;
        public RebirthPanel(ReactorHud hud, Transform parent, ReactorApp owner)
        {
            app = owner;
            var blocker = ReactorHud.Button("ProgressionOverlay", parent, new Color(.02f, .035f, .06f, 1), .035f, .145f, .965f, .815f);
            root = blocker.gameObject; blocker.transition = Selectable.Transition.None;

            var panel = ReactorHud.Button("ProgressionPanel", blocker.transform, new Color(.025f, .045f, .075f), 0, 0, 1, 1);
            panel.transition = Selectable.Transition.None;
            hud.Label("ProgressionTitle", panel.transform, "ASCENSIÓN", 36, Color.white, .04f, .925f, .96f, .98f);
            summary = hud.Label("PermanentSummary", panel.transform, "", 26, ReactorHud.Warm, .04f, .84f, .96f, .925f);
            string[] names = { "ASCENDER", "MAESTRÍAS", "SKINS" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                var tab = ReactorHud.Button("ProgressionTab" + i, panel.transform, new Color(.10f, .08f, .22f), .035f + i * .32f, .70f, .325f + i * .32f, .835f);
                tabs[i] = tab;
                hud.Label("TabLabel", tab.transform, names[i], 27, Color.white, .02f, 0, .98f, 1);
                tab.onClick.AddListener(() => ShowPage(page));
                var viewport = ReactorHud.Rect("ProgressionViewport" + i, panel.transform, .035f, .25f, .965f, .68f);
                viewport.gameObject.AddComponent<RectMask2D>();
                var hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .01f);
                var content = ReactorHud.Rect("ProgressionPage" + i, viewport, 0, 1, 1, 1);
                content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 1800);
                var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
                pages[i] = viewport.gameObject;
                pageContents[i] = content;
            }
            cycle = hud.Label("CyclePreview", pageContents[0], "", 29, Color.white, .02f, .71f, .98f, 1);
            var track = ReactorHud.Image("AscensionRequirementTrack", pageContents[0], new Color(.08f, .13f, .19f), .04f, .68f, .96f, .695f);
            requirementFill = ReactorHud.Image("AscensionRequirementProgress", track.transform, ReactorHud.Warm, 0, 0, 0, 1).rectTransform;
            currentBranch = hud.Label("BranchExplanation", pageContents[0], "", 25, ReactorHud.Cyan, .02f, .51f, .98f, .68f);
            for (int i = 0; i < branches.Length; i++)
            {
                var branch = (Specialization)(i + 1);
                branches[i] = ReactorHud.Button("ChooseBranch" + i, pageContents[0], new Color(.06f, .09f, .16f), .02f, .36f - i * .12f, .98f, .47f - i * .12f);
                branchLabels[i] = hud.Label("BranchLabel", branches[i].transform, "", 28, Color.white, .02f, 0, .98f, 1);
                branches[i].onClick.AddListener(() => { selected = branch; Refresh(); });
            }
            ArmButton = ReactorHud.Button("PrepareRebirth", pageContents[0], new Color(.23f, .06f, .19f), .02f, 0, .98f, .10f);
            armLabel = hud.Label("PrepareRebirthText", ArmButton.transform, "", 29, ReactorHud.Warm, .02f, 0, .98f, 1);
            ArmButton.onClick.AddListener(Arm);
            hud.Label("MasteryHelp", pageContents[1],
                "Mejoras permanentes para todos los ciclos.\nGastar esencia conserva la bonificación global.", 29, ReactorHud.Cyan, .03f, .79f, .97f, 1);
            for (int i = 0; i < 3; i++)
            {
                var branch = (Specialization)(i + 1);
                MasteryButtons[i] = ReactorHud.Button("BuyMastery" + i, pageContents[1], new Color(.08f, .07f, .18f), .02f, .54f - i * .25f, .98f, .77f - i * .25f);
                masteryLabels[i] = hud.Label("MasteryDescription", MasteryButtons[i].transform, "", 29, Color.white, .04f, .03f, .96f, .97f);
                MasteryButtons[i].onClick.AddListener(() => { app.BuyMastery(branch); Refresh(); });
            }
            hud.Label("SkinHelp", pageContents[2], "Colección permanente · solo apariencia\nLa forma del núcleo evoluciona con cada ciclo.", 28, ReactorHud.Cyan, .02f, .83f, .98f, 1);
            for (int i = 0; i < skins.Length; i++)
            {
                int skin = i;
                skins[i] = ReactorHud.Button("SelectSkin" + i, pageContents[2], new Color(.06f, .09f, .16f), .02f, .61f - i * .20f, .98f, .79f - i * .20f);
                var crown = ReactorHud.Rect("SkinSilhouette", skins[i].transform, .02f, .05f, .20f, .95f).gameObject.AddComponent<CrownGraphic>();
                crown.Configure(3, skin); crown.color = ReactorAssembly.SkinColor(skin); crown.raycastTarget = false;
                skinLabels[i] = hud.Label("SkinDescription", skins[i].transform, "", 28, Color.white, .22f, .04f, .97f, .96f);
                skins[i].onClick.AddListener(() => { app.SelectSkin(skin); Refresh(); });
            }
            message = hud.Label("ProgressionMessage", panel.transform, "", 25, ReactorHud.Warm, .04f, .16f, .96f, .24f);
            var close = ReactorHud.Button("CloseProgression", panel.transform, new Color(.06f, .09f, .16f), .2f, .02f, .8f, .15f);
            hud.Label("CloseProgressionText", close.transform, "VOLVER AL REACTOR", 28, Color.white, .02f, 0, .98f, 1);
            close.onClick.AddListener(() => hud.SelectTab(0));
            // Confirmation is a separate blocker; changing tabs cannot accidentally confirm a reset.
            var confirmPanel = ReactorHud.Button("ConfirmRebirthPanel", panel.transform, new Color(.035f, .025f, .065f, 1), .02f, 0, .98f, 1);
            confirmation = confirmPanel.gameObject; confirmPanel.transition = Selectable.Transition.None;
            var confirmViewport = ReactorHud.Rect("ConfirmationViewport", confirmPanel.transform, .06f, .34f, .94f, .96f);
            confirmViewport.gameObject.AddComponent<RectMask2D>();
            var confirmHit = confirmViewport.gameObject.AddComponent<Image>(); confirmHit.color = new Color(0, 0, 0, .01f);
            var confirmContent = ReactorHud.Rect("ConfirmationContent", confirmViewport, 0, 1, 1, 1);
            confirmContent.pivot = new Vector2(.5f, 1); confirmContent.sizeDelta = new Vector2(0, 1300);
            confirmationScroll = confirmViewport.gameObject.AddComponent<ScrollRect>();
            confirmationScroll.viewport = confirmViewport; confirmationScroll.content = confirmContent; confirmationScroll.horizontal = false;
            confirmationScroll.movementType = ScrollRect.MovementType.Clamped;
            confirmationText = hud.Label("ConfirmRebirthDescription", confirmContent, "", 31, Color.white, 0, 0, 1, 1);
            ConfirmButton = ReactorHud.Button("ConfirmRebirth", confirmPanel.transform, new Color(.28f, .06f, .18f), .06f, .17f, .94f, .31f);
            hud.Label("ConfirmRebirthText", ConfirmButton.transform, "CONFIRMAR ASCENSIÓN", 29, ReactorHud.Warm, .02f, 0, .98f, 1);
            ConfirmButton.onClick.AddListener(() =>
            {
                bool committed = app.Rebirth(selected, quotedReward, quotedCycle);
                confirmation.SetActive(false);
                if (committed) Close(); else Refresh();
            });
            var cancel = ReactorHud.Button("CancelRebirth", confirmPanel.transform, new Color(.07f, .10f, .17f), .17f, .02f, .83f, .15f);
            hud.Label("CancelRebirthText", cancel.transform, "CONSERVAR MI REACTOR", 27, Color.white, .02f, 0, .98f, 1);
            cancel.onClick.AddListener(CancelConfirmation);
            confirmation.SetActive(false); root.SetActive(false);
        }
        public void Open(int page = 0)
        {
            app.Hud.ClosePanels();
            selected = app.Game.Specialization == Specialization.None ? Specialization.Tap : app.Game.Specialization;
            message.text = "Desliza para elegir rama y ascender. Reinicia el ciclo a cambio de poder permanente."; root.SetActive(true); ShowPage(page); Refresh();
        }
        public void Close() { confirmation.SetActive(false); root.SetActive(false); }
        public void CancelConfirmation() { confirmation.SetActive(false); }
        public void ShowMessage(string text) { message.text = text; }
        private void ShowPage(int page)
        {
            confirmation.SetActive(false);
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == page);
                tabs[i].image.color = i == page ? new Color(.12f, .23f, .28f) : new Color(.055f, .075f, .13f);
            }
            Refresh();
        }
        private void Arm()
        {
            if (!app.Game.CanRebirth) return;
            GameEngine game = app.Game;
            quotedReward = game.RebirthReward; quotedCycle = game.RebirthCount;
            confirmationText.text = "ASCENDER AL SIGUIENTE CICLO\nDesliza para revisar todos los cambios.\n\nRecibirás " + quotedReward + " ESENCIAS\nEspecialización: " + RebirthCatalog.BranchName(selected)
                + "\nPotencia permanente: ×" + NumberFormat.Stat(game.PermanentMultiplier) + " a ×" + NumberFormat.Stat(game.ProjectedPermanentMultiplier)
                + (game.RebirthSkinUnlock >= 0 ? "\nDesbloqueas y equipas: " + RebirthCatalog.SkinName(game.RebirthSkinUnlock) : "")
                + "\n\nSe reinician energía, compras, hitos del ciclo, combo y sobrecarga."
                + "\n\nConservas historia, planos, sintonía, logros, recompensas pendientes, esencia, maestrías, skins y ajustes."
                + "\n\nComienzas con " + game.StarterCoils + " microgenerador y potencia NV. " + game.StarterTapLevel + ".";
            confirmation.SetActive(true);
            Canvas.ForceUpdateCanvases(); confirmationScroll.verticalNormalizedPosition = 1;
        }
        public void Refresh()
        {
            if (!IsOpen) return;
            GameEngine game = app.Game;
            summary.text = "ASCENSIONES " + game.RebirthCount + "  ·  ESENCIA " + game.Essence
                + "\nPotencia permanente ×" + NumberFormat.Stat(game.PermanentMultiplier) + " · Histórico " + NumberFormat.Compact(game.AllTimeEnergy) + " E"
                + "\nEspecialización actual: " + RebirthCatalog.BranchName(game.Specialization);
            requirementFill.anchorMax = new Vector2(Mathf.Clamp01((float)(game.Lifetime / game.RebirthThreshold)), 1);
            cycle.text = "REQUISITO · " + NumberFormat.Compact(game.Lifetime) + " / " + NumberFormat.Compact(game.RebirthThreshold) + " E del ciclo\n"
                + (game.CanRebirth ? "LISTO · Recibes " + game.RebirthReward + " ESENCIAS" : game.Lifetime >= game.RebirthThreshold ? "Límite de ascensiones o esencia alcanzado"
                    : "Faltan " + NumberFormat.Compact(game.RebirthThreshold - game.Lifetime) + " E · Comprar no reduce el progreso")
                + "\n" + (game.RebirthSkinUnlock >= 0 ? "Próxima skin: " + RebirthCatalog.SkinName(game.RebirthSkinUnlock)
                    : "Potencia permanente tras ascender: ×" + NumberFormat.Stat(game.ProjectedPermanentMultiplier))
                + (game.CanRebirth && !double.IsPositiveInfinity(game.NextEssenceTarget)
                    ? "\nSiguiente esencia a " + NumberFormat.Compact(game.NextEssenceTarget) + " E" : "");
            currentBranch.text = "ELIGE TU ESPECIALIZACIÓN"
                + "\nPróximo ciclo: ×" + NumberFormat.Stat(game.SpecializationBonus(selected)) + " en la rama elegida."
                + (selected == Specialization.Tap ? "\nToques añade " + NumberFormat.Stat(game.TapProductionFraction * 100) + "% de producción a la base de cada toque." : "");
            for (int i = 0; i < 3; i++)
            {
                var branch = (Specialization)(i + 1);
                branchLabels[i].text = (selected == branch ? "[X] " : "[ ] ") + RebirthCatalog.BranchName(branch) + (branch == Specialization.Tap ? " · enlace " + NumberFormat.Stat(game.TapProductionFraction * 100) + "%" : "");
                branches[i].image.color = selected == branch ? new Color(.18f, .10f, .30f) : new Color(.06f, .09f, .16f);
                int level = game.MasteryLevel(branch);
                masteryLabels[i].text = RebirthCatalog.BranchName(branch) + " · NV. " + level + " / " + RebirthProgress.MaxMastery
                    + "\n×" + NumberFormat.Stat(game.MasteryMultiplier(branch, level))
                    + (level == RebirthProgress.MaxMastery ? " · MÁXIMO" : " a ×" + NumberFormat.Stat(game.MasteryMultiplier(branch, level + 1))
                        + "\nCOMPRAR · " + game.MasteryCost(branch) + " ESENCIA");
                MasteryButtons[i].interactable = game.CanBuyMastery(branch);
            }
            ArmButton.interactable = game.CanRebirth;
            armLabel.text = game.CanRebirth ? "ASCENDER · +" + game.RebirthReward + " ESENCIAS" : "ASCENSIÓN BLOQUEADA · COMPLETA EL REQUISITO";
            for (int i = 0; i < skins.Length; i++)
            {
                bool unlocked = RebirthCatalog.SkinUnlocked(i, game.RebirthCount);
                skins[i].interactable = unlocked && game.Skin != i;
                skinLabels[i].text = RebirthCatalog.SkinName(i) + "\n" + (game.Skin == i ? "EQUIPADA" : unlocked ? "EQUIPAR" : "Requiere " + RebirthCatalog.SkinRequirement(i) + " ascensiones");
            }
        }
    }
}
