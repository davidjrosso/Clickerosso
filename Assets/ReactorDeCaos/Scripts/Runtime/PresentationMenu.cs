using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class PresentationMenu
    {
        private readonly GameObject root;
        private readonly PresentationPreferences settings;
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[5];
        private readonly Button opener, close;
        public Button[] Options { get; } = new Button[5];
        public bool IsOpen { get { return root.activeSelf; } }

        public PresentationMenu(ReactorHud hud, Transform parent, Button openButton, PresentationPreferences preferences)
        {
            settings = preferences;
            opener = openButton;
            Button blocker = ReactorHud.Button("SettingsOverlay", parent, new Color(0, .01f, .025f, .88f), 0, 0, 1, 1);
            blocker.transition = Selectable.Transition.None;
            blocker.onClick.AddListener(() => SetOpen(false));
            root = blocker.gameObject;
            // A button consumes panel clicks so they cannot bubble to the dismiss backdrop.
            Button panel = ReactorHud.Button("SettingsPanel", blocker.transform,
                new Color(.025f, .045f, .09f, 1), .055f, .14f, .945f, .86f);
            panel.transition = Selectable.Transition.None;
            hud.Label("SettingsTitle", panel.transform, "AJUSTES", 42, Color.white, .07f, .86f, .93f, .95f);
            hud.Label("SettingsHelp", panel.transform, "Personaliza la intensidad y el sonido.", 25, ReactorHud.Cyan,
                .07f, .79f, .93f, .86f);
            for (int i = 0; i < Options.Length; i++)
            {
                int option = i;
                float top = .78f - i * .12f;
                Button button = ReactorHud.Button("PresentationOption" + i, panel.transform,
                    new Color(.06f, .09f, .16f), .06f, top - .10f, .94f, top);
                button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                button.onClick.AddListener(() => Toggle(option));
                Options[i] = button;
                labels[i] = hud.Label("SettingValue", button.transform, "", 29, Color.white, .04f, .05f, .96f, .95f);
            }
            close = ReactorHud.Button("CloseSettings", panel.transform, new Color(.12f, .08f, .24f), .15f, .06f, .85f, .15f);
            close.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            hud.Label("CloseSettingsLabel", close.transform, "VOLVER AL REACTOR", 29, ReactorHud.Cyan, .02f, .05f, .98f, .95f);
            close.onClick.AddListener(() => SetOpen(false));
            opener.onClick.AddListener(() => SetOpen(true));
            Refresh();
            root.SetActive(false);
        }

        public void SetOpen(bool value)
        {
            root.SetActive(value);
            Refresh();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(value ? close.gameObject : opener.gameObject);
        }

        private void Toggle(int option)
        {
            switch (option)
            {
                case 0: settings.sound = !settings.sound; break;
                case 1: settings.reducedFlashes = !settings.reducedFlashes; break;
                case 2: settings.reducedMotion = !settings.reducedMotion; break;
                case 3: settings.limitedEffects = !settings.limitedEffects; break;
                case 4: settings.vibration = !settings.vibration; break;
            }
            settings.SaveAndNotify();
            Refresh();
        }

        private void Refresh()
        {
            labels[0].text = "SONIDO · " + (settings.sound ? "ACTIVADO" : "DESACTIVADO");
            labels[1].text = "DESTELLOS · " + (settings.reducedFlashes ? "REDUCIDOS" : "NORMALES");
            labels[2].text = "MOVIMIENTO · " + (settings.reducedMotion ? "REDUCIDO" : "NORMAL");
            labels[3].text = "EFECTOS · " + (settings.limitedEffects ? "SUAVES" : "COMPLETOS");
            labels[4].text = "VIBRACIÓN · " + (settings.vibration ? "EVENTOS IMPORTANTES" : "DESACTIVADA");
        }
    }
}
