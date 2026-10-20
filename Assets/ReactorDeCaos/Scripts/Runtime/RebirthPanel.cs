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
        private readonly TextMeshProUGUI summary, cycle, currentBranch, armLabel, message, confirmationText;
        private readonly TextMeshProUGUI[] branchLabels = new TextMeshProUGUI[3], masteryLabels = new TextMeshProUGUI[3], skinLabels = new TextMeshProUGUI[4];
        private readonly Button[] branches = new Button[3], skins = new Button[4];
        private readonly GameObject confirmation;
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
            var blocker = ReactorHud.Button("ProgressionOverlay", parent, new Color(0, .01f, .025f, .96f), 0, 0, 1, 1);
            root = blocker.gameObject; blocker.transition = Selectable.Transition.None;
            blocker.onClick.AddListener(Close);
            var panel = ReactorHud.Button("ProgressionPanel", blocker.transform, new Color(.025f, .045f, .09f), .035f, .04f, .965f, .96f);
            panel.transition = Selectable.Transition.None;
            hud.Label("ProgressionTitle", panel.transform, "EVOLUCIÓN DEL REACTOR", 36, Color.white, .04f, .925f, .96f, .98f);
            summary = hud.Label("PermanentSummary", panel.transform, "", 26, ReactorHud.Warm, .04f, .84f, .96f, .925f);
            string[] names = { "RENACER", "MAESTRÍAS", "SKINS" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                var tab = ReactorHud.Button("ProgressionTab" + i, panel.transform, new Color(.10f, .08f, .22f), .035f + i * .32f, .77f, .325f + i * .32f, .835f);
                hud.Label("TabLabel", tab.transform, names[i], 27, Color.white, .02f, 0, .98f, 1);
                tab.onClick.AddListener(() => ShowPage(page));
                pages[i] = ReactorHud.Rect("ProgressionPage" + i, panel.transform, .035f, .18f, .965f, .75f).gameObject;
            }
            cycle = hud.Label("CyclePreview", pages[0].transform, "", 30, Color.white, .02f, .68f, .98f, 1);
            currentBranch = hud.Label("BranchExplanation", pages[0].transform, "", 25, ReactorHud.Cyan, .02f, .51f, .98f, .68f);
            for (int i = 0; i < branches.Length; i++)
            {
                var branch = (Specialization)(i + 1);
                branches[i] = ReactorHud.Button("ChooseBranch" + i, pages[0].transform, new Color(.06f, .09f, .16f), .02f, .36f - i * .12f, .98f, .47f - i * .12f);
                branchLabels[i] = hud.Label("BranchLabel", branches[i].transform, "", 28, Color.white, .02f, 0, .98f, 1);
                branches[i].onClick.AddListener(() => { selected = branch; Refresh(); });
            }
            ArmButton = ReactorHud.Button("PrepareRebirth", pages[0].transform, new Color(.23f, .06f, .19f), .02f, 0, .98f, .10f);
            armLabel = hud.Label("PrepareRebirthText", ArmButton.transform, "", 29, ReactorHud.Warm, .02f, 0, .98f, 1);
            ArmButton.onClick.AddListener(Arm);
            hud.Label("MasteryHelp", pages[1].transform,
                "Mejoras permanentes para todos los ciclos.\nGastar esencia conserva la bonificación global.", 29, ReactorHud.Cyan, .03f, .79f, .97f, 1);
            for (int i = 0; i < 3; i++)
            {
                var branch = (Specialization)(i + 1);
                MasteryButtons[i] = ReactorHud.Button("BuyMastery" + i, pages[1].transform, new Color(.08f, .07f, .18f), .02f, .54f - i * .25f, .98f, .77f - i * .25f);
                masteryLabels[i] = hud.Label("MasteryDescription", MasteryButtons[i].transform, "", 29, Color.white, .04f, .03f, .96f, .97f);
                MasteryButtons[i].onClick.AddListener(() => { app.BuyMastery(branch); Refresh(); });
            }
            hud.Label("SkinHelp", pages[2].transform, "Colección permanente · solo apariencia\nLa forma del núcleo evoluciona con cada ciclo.", 28, ReactorHud.Cyan, .02f, .83f, .98f, 1);
            for (int i = 0; i < skins.Length; i++)
            {
                int skin = i;
                skins[i] = ReactorHud.Button("SelectSkin" + i, pages[2].transform, new Color(.06f, .09f, .16f), .02f, .61f - i * .20f, .98f, .79f - i * .20f);
                var crown = ReactorHud.Rect("SkinSilhouette", skins[i].transform, .02f, .05f, .20f, .95f).gameObject.AddComponent<CrownGraphic>();
                crown.Configure(3, skin); crown.color = ReactorAssembly.SkinColor(skin); crown.raycastTarget = false;
                skinLabels[i] = hud.Label("SkinDescription", skins[i].transform, "", 28, Color.white, .22f, .04f, .97f, .96f);
                skins[i].onClick.AddListener(() => { app.SelectSkin(skin); Refresh(); });
            }
            message = hud.Label("ProgressionMessage", panel.transform, "", 25, ReactorHud.Warm, .04f, .11f, .96f, .175f);
            var close = ReactorHud.Button("CloseProgression", panel.transform, new Color(.06f, .09f, .16f), .2f, .025f, .8f, .10f);
            hud.Label("CloseProgressionText", close.transform, "VOLVER AL REACTOR", 28, Color.white, .02f, 0, .98f, 1);
            close.onClick.AddListener(Close);
            // Confirmation is a separate blocker; changing tabs cannot accidentally confirm a reset.
            var confirmPanel = ReactorHud.Button("ConfirmRebirthPanel", panel.transform, new Color(.035f, .025f, .065f, 1), .02f, .18f, .98f, .84f);
            confirmation = confirmPanel.gameObject; confirmPanel.transition = Selectable.Transition.None;
            confirmationText = hud.Label("ConfirmRebirthDescription", confirmPanel.transform, "", 31, Color.white, .06f, .32f, .94f, .96f);
            ConfirmButton = ReactorHud.Button("ConfirmRebirth", confirmPanel.transform, new Color(.28f, .06f, .18f), .06f, .17f, .94f, .29f);
            hud.Label("ConfirmRebirthText", ConfirmButton.transform, "CONFIRMAR COLAPSO", 29, ReactorHud.Warm, .02f, 0, .98f, 1);
            ConfirmButton.onClick.AddListener(() =>
            {
                bool committed = app.Rebirth(selected, quotedReward, quotedCycle);
                confirmation.SetActive(false);
                if (committed) Close(); else Refresh();
            });
            var cancel = ReactorHud.Button("CancelRebirth", confirmPanel.transform, new Color(.07f, .10f, .17f), .17f, .025f, .83f, .14f);
            hud.Label("CancelRebirthText", cancel.transform, "CONSERVAR MI REACTOR", 27, Color.white, .02f, 0, .98f, 1);
            cancel.onClick.AddListener(CancelConfirmation);
            confirmation.SetActive(false); root.SetActive(false);
        }
        public void Open(int page = 0)
        {
            app.Hud.ClosePanels();
            selected = app.Game.Specialization == Specialization.None ? Specialization.Tap : app.Game.Specialization;
            message.text = ""; root.SetActive(true); ShowPage(page); Refresh();
        }
        public void Close() { confirmation.SetActive(false); root.SetActive(false); }
        public void CancelConfirmation() { confirmation.SetActive(false); }
        public void ShowMessage(string text) { message.text = text; }
        private void ShowPage(int page)
        {
            confirmation.SetActive(false);
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == page);
            Refresh();
        }
        private void Arm()
        {
            if (!app.Game.CanRebirth) return;
            GameEngine game = app.Game;
            quotedReward = game.RebirthReward; quotedCycle = game.RebirthCount;
            confirmationText.text = "COLAPSO CONTROLADO\n\nRecibirás " + quotedReward + " ESENCIAS\nEspecialización: " + RebirthCatalog.BranchName(selected)
                + "\nPotencia permanente: ×" + NumberFormat.Stat(game.PermanentMultiplier) + " a ×" + NumberFormat.Stat(game.ProjectedPermanentMultiplier)
                + (game.RebirthSkinUnlock >= 0 ? "\nDesbloqueas y equipas: " + RebirthCatalog.SkinName(game.RebirthSkinUnlock) : "")
                + "\n\nSe reinician energía, compras, hitos del ciclo, combo y sobrecarga."
                + "\n\nConservas esencia, maestrías, skins y ajustes."
                + "\n\nComienzas con " + game.StarterCoils + " microgenerador y potencia NV. " + game.StarterTapLevel + ".";
            confirmation.SetActive(true);
        }
        public void Refresh()
        {
            if (!IsOpen) return;
            GameEngine game = app.Game;
            summary.text = "ESENCIA " + game.Essence + "  ·  RENACIMIENTOS " + game.RebirthCount
                + "\nPotencia permanente ×" + NumberFormat.Stat(game.PermanentMultiplier) + " · Histórico " + NumberFormat.Compact(game.AllTimeEnergy) + " E";
            cycle.text = "CICLO · " + NumberFormat.Compact(game.Lifetime) + " / " + NumberFormat.Compact(game.RebirthThreshold) + " E\n"
                + (game.CanRebirth ? "Renacer ahora: +" + game.RebirthReward + " ESENCIAS" : "Genera energía para habilitar el colapso.")
                + "\n" + (double.IsPositiveInfinity(game.NextEssenceTarget) ? "Límite de recompensa alcanzado"
                    : "Próxima recompensa: " + NumberFormat.Compact(game.NextEssenceTarget) + " E acumulados")
                + "\nLas compras no reducen esta recompensa.";
            currentBranch.text = "Actual: " + RebirthCatalog.BranchName(game.Specialization)
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
            armLabel.text = game.CanRebirth ? "REVISAR COLAPSO · +" + game.RebirthReward + " ESENCIAS" : "COLAPSO TODAVÍA BLOQUEADO";
            for (int i = 0; i < skins.Length; i++)
            {
                bool unlocked = RebirthCatalog.SkinUnlocked(i, game.RebirthCount);
                skins[i].interactable = unlocked && game.Skin != i;
                skinLabels[i].text = RebirthCatalog.SkinName(i) + "\n" + (game.Skin == i ? "EQUIPADA" : unlocked ? "EQUIPAR" : "Requiere " + RebirthCatalog.SkinRequirement(i) + " renacimientos");
            }
        }
    }
}
