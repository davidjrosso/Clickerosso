using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class ReactorModulePanel
    {
        private readonly ReactorApp app;
        private readonly GameObject root;
        private readonly TextMeshProUGUI title, detail, buyLabel;
        private readonly Image icon;
        public Button BuyButton { get; }
        public string SelectedId { get; private set; }
        public bool IsOpen => root.activeSelf;
        public ReactorModulePanel(ReactorHud hud, Transform parent, ReactorApp owner)
        {
            app = owner;
            var blocker = ReactorHud.Button("ModuleDetailsOverlay", parent, new Color(0, .01f, .025f, .92f), 0, 0, 1, 1);
            root = blocker.gameObject; blocker.transition = Selectable.Transition.None;
            blocker.onClick.AddListener(Close);
            var panel = ReactorHud.Button("ModuleDetails", blocker.transform, new Color(.025f, .045f, .09f), .055f, .19f, .945f, .81f);
            panel.transition = Selectable.Transition.None;
            icon = ReactorHud.Image("ModulePortrait", panel.transform, Color.white, .36f, .72f, .64f, .94f);
            icon.preserveAspect = true;
            title = hud.Label("ModuleTitle", panel.transform, "", 38, Color.white, .05f, .62f, .95f, .72f);
            detail = hud.Label("ModuleDetailsText", panel.transform, "", 31, ReactorHud.Cyan, .07f, .35f, .93f, .62f);
            BuyButton = ReactorHud.Button("BuyModule", panel.transform, new Color(.11f, .08f, .25f), .08f, .185f, .92f, .335f);
            buyLabel = hud.Label("BuyModuleText", BuyButton.transform, "", 30, Color.white, .02f, 0, .98f, 1);
            BuyButton.onClick.AddListener(() => { app.Buy(SelectedId); Refresh(); });
            var close = ReactorHud.Button("CloseModuleDetails", panel.transform, new Color(.06f, .09f, .16f), .2f, .02f, .8f, .16f);
            hud.Label("CloseModuleText", close.transform, "VOLVER", 29, Color.white, 0, 0, 1, 1);
            close.onClick.AddListener(Close);
            root.SetActive(false);
        }
        public void Open(string id)
        {
            app.Hud.ClosePanels(); SelectedId = id;
            icon.sprite = app.Hud.Icon(id);
            root.SetActive(true); Refresh();
        }
        public void Close() { root.SetActive(false); }
        public void Refresh()
        {
            if (!IsOpen) return;
            GameEngine game = app.Game;
            foreach (var item in game.Shop)
            {
                if (item.Id != SelectedId) continue;
                int level = game.Level(item.Id);
                title.text = item.Name + " · NV. " + level;
                string next = level >= EconomyConfig.MaxLevel ? "NIVEL MÁXIMO"
                    : "Siguiente: " + NumberFormat.Stat(game.EffectAtLevel(item.Id, level + 1)) + " " + item.Unit;
                string production = item.IsGenerator ? "\nProducción actual: " + NumberFormat.Stat(game.ProductionOf(item.Id)) + " E / s" : "";
                detail.text = item.Description + "\n\nActual: " + NumberFormat.Stat(game.EffectAtLevel(item.Id, level)) + " " + item.Unit
                    + "\n" + next + production + (game.Unlocked(item.Id) ? "" : "\nRequiere " + game.UnlockDescription(item.Id));
                buyLabel.text = level >= EconomyConfig.MaxLevel ? "COMPLETADO" : (level == 0 ? "INSTALAR · " : "MEJORAR · ")
                    + NumberFormat.Compact(game.Cost(item.Id)) + " E";
                BuyButton.interactable = game.CanBuy(item.Id);
                break;
            }
        }
    }
}
