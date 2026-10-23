using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class AchievementPanel
    {
        private readonly ReactorApp app;
        private readonly GameObject root;
        private readonly TextMeshProUGUI summary, message;
        private readonly TextMeshProUGUI[] descriptions, actions;
        private readonly RectTransform[] fills;
        public Button[] ClaimButtons { get; }
        public bool IsOpen => root.activeSelf;
        public AchievementPanel(ReactorHud hud, Transform parent, ReactorApp owner)
        {
            app = owner;
            var surface = ReactorHud.Image("AchievementsPage", parent, new Color(.02f, .035f, .06f, .98f), .035f, .145f, .965f, .815f);
            surface.raycastTarget = true; root = surface.gameObject;
            hud.Label("AchievementsTitle", surface.transform, "LOGROS", 38, Color.white, .04f, .925f, .96f, .985f, TextAlignmentOptions.Left);
            summary = hud.Label("AchievementsSummary", surface.transform, "", 26, ReactorHud.Cyan, .04f, .84f, .96f, .925f, TextAlignmentOptions.Left);
            var viewport = ReactorHud.Rect("AchievementsViewport", surface.transform, .025f, .08f, .975f, .825f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .01f);
            var content = ReactorHud.Rect("AchievementsContent", viewport, 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1);
            int count = app.Game.AchievementCount;
            content.sizeDelta = new Vector2(0, count * 238);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            ClaimButtons = new Button[count]; descriptions = new TextMeshProUGUI[count];
            actions = new TextMeshProUGUI[count]; fills = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                var goal = app.Game.AchievementAt(i);
                var card = ReactorHud.Image("Achievement_" + goal.id, content, new Color(.045f, .075f, .11f), 0, 1, 1, 1);
                card.rectTransform.pivot = new Vector2(.5f, 1);
                card.rectTransform.sizeDelta = new Vector2(0, 222);
                card.rectTransform.anchoredPosition = new Vector2(0, -i * 238);
                descriptions[i] = hud.Label("AchievementDescription", card.transform, "", 30, Color.white, .035f, .19f, .70f, .94f, TextAlignmentOptions.Left);
                var track = ReactorHud.Image("AchievementTrack", card.transform, new Color(.08f, .13f, .19f), .035f, .08f, .69f, .11f);
                fills[i] = ReactorHud.Image("AchievementProgress", track.transform, ReactorHud.Cyan, 0, 0, 0, 1).rectTransform;
                var button = ReactorHud.Button("Claim_" + goal.id, card.transform, new Color(.10f, .19f, .22f), .73f, .1f, .975f, .90f);
                ClaimButtons[i] = button;
                actions[i] = hud.Label("ClaimText", button.transform, "", 27, ReactorHud.Warm, .02f, .04f, .98f, .96f);
                button.onClick.AddListener(() => app.ClaimAchievement(goal.id));
            }
            message = hud.Label("AchievementsMessage", surface.transform, "", 24, ReactorHud.Warm, .035f, .005f, .965f, .075f);
            root.SetActive(false);
        }
        public void Open() { root.SetActive(true); message.text = "Las recompensas se conservan pendientes al ascender."; Refresh(); }
        public void Close() { root.SetActive(false); }
        public void ShowMessage(string text) { message.text = text; }
        public void Refresh()
        {
            if (!IsOpen) return;
            GameEngine game = app.Game;
            int claimed = 0, visible = 0;
            for (int i = 0; i < ClaimButtons.Length; i++)
            {
                var goal = game.AchievementAt(i);
                var card = (RectTransform)ClaimButtons[i].transform.parent;
                bool revealed = game.CampaignAchievementVisible(goal);
                card.gameObject.SetActive(revealed);
                if (!revealed) continue;
                card.anchoredPosition = new Vector2(0, -visible++ * 238);
                bool done = game.AchievementClaimed(goal.id), unlocked = game.AchievementUnlocked(goal.id);
                if (done) claimed++;
                double progress = game.AchievementProgress(goal);
                descriptions[i].text = goal.title + "\n<size=80%><color=#BBCADD>" + goal.description
                    + "\n" + NumberFormat.Compact(progress) + " / " + NumberFormat.Compact(goal.target) + "</color></size>";
                fills[i].anchorMax = new Vector2(Mathf.Clamp01((float)(progress / goal.target)), 1);
                ClaimButtons[i].interactable = game.CanClaimAchievement(goal.id);
                actions[i].text = done ? "RECLAMADO" : (unlocked ? game.CanClaimAchievement(goal.id) ? "RECLAMAR" : "LÍMITE DE ENERGÍA" : "EN CURSO")
                    + "\n+" + NumberFormat.Compact(goal.energyReward) + " E";
            }
            if (ClaimButtons.Length > 0) ((RectTransform)ClaimButtons[0].transform.parent.parent).sizeDelta = new Vector2(0, visible * 238);
            summary.text = claimed + " / " + visible + " reclamados · " + game.AvailableAchievements + " disponibles"
                + "\nCada recompensa se entrega una sola vez.";
        }
    }
}
