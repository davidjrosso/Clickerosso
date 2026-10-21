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
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[6];
        private readonly Button opener, close;
        public Button[] Options { get; } = new Button[6];
        public Slider EffectsVolume { get; }
        public Slider MusicVolume { get; }
        private readonly TextMeshProUGUI effectsValue, musicValue;
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
                new Color(.025f, .045f, .09f, 1), .055f, .08f, .945f, .94f);
            panel.transition = Selectable.Transition.None;
            hud.Label("SettingsTitle", panel.transform, "AJUSTES", 42, Color.white, .07f, .86f, .93f, .95f);
            hud.Label("SettingsHelp", panel.transform, "Sonido y accesibilidad · desliza para ver más.", 25, ReactorHud.Cyan,
                .07f, .79f, .93f, .86f);
            var viewport = ReactorHud.Rect("SettingsViewport", panel.transform, .05f, .17f, .95f, .78f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .01f);
            var content = ReactorHud.Rect("SettingsContent", viewport, 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 1436);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            for (int i = 0; i < Options.Length; i++)
            {
                int option = i;

                Button button = ReactorHud.Button("PresentationOption" + i, content,
                    new Color(.06f, .09f, .16f), 0, 1, 1, 1);
                var rect = (RectTransform)button.transform;
                rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 164);
                rect.anchoredPosition = new Vector2(0, -i * 176);
                button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                button.onClick.AddListener(() => Toggle(option));
                Options[i] = button;
                labels[i] = hud.Label("SettingValue", button.transform, "", 29, Color.white, .04f, .05f, .96f, .95f);
            }
            EffectsVolume = VolumeSlider(hud, content, "EffectsVolume", 1056, out effectsValue);
            MusicVolume = VolumeSlider(hud, content, "MusicVolume", 1246, out musicValue);
            EffectsVolume.onValueChanged.AddListener(value => { settings.effectsVolume = value; settings.PreviewChanges(); Refresh(); });
            MusicVolume.onValueChanged.AddListener(value => { settings.musicVolume = value; settings.PreviewChanges(); Refresh(); });
            close = ReactorHud.Button("CloseSettings", panel.transform, new Color(.12f, .08f, .24f), .15f, .035f, .85f, .145f);
            close.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            hud.Label("CloseSettingsLabel", close.transform, "VOLVER", 29, ReactorHud.Cyan, .02f, .05f, .98f, .95f);
            close.onClick.AddListener(() => SetOpen(false));
            opener.onClick.AddListener(() => SetOpen(true));
            Refresh();
            root.SetActive(false);
        }

        public void SetOpen(bool value)
        {
            if (value) root.transform.SetAsLastSibling();
            else settings.SaveAndNotify(); // Persist once after slider interaction, not on every drag sample.
            root.SetActive(value);
            Refresh();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(value ? close.gameObject : opener.gameObject);
        }

        private static Slider VolumeSlider(ReactorHud hud, Transform parent, string name, float top, out TextMeshProUGUI label)
        {
            var rect = ReactorHud.Rect(name, parent, 0, 1, 1, 1);
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 180);
            rect.anchoredPosition = new Vector2(0, -top);
            var hit = rect.gameObject.AddComponent<Image>(); hit.color = new Color(.04f, .07f, .12f);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
            label = hud.Label(name + "Value", rect, "", 28, ReactorHud.Cyan, .03f, .67f, .97f, .99f);
            ReactorHud.Image("Track", rect, new Color(.10f, .19f, .26f), .06f, .28f, .94f, .34f);
            var handleArea = ReactorHud.Rect("HandleArea", rect, .08f, .05f, .92f, .64f);
            var handle = ReactorHud.Image("Handle", handleArea, ReactorHud.Cyan, 0, 0, 0, 1);
            handle.rectTransform.sizeDelta = new Vector2(54, 0);
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            return slider;
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
                case 5: settings.music = !settings.music; break;
            }
            settings.SaveAndNotify();
            Refresh();
        }

        private void Refresh()
        {
            labels[0].text = "EFECTOS DE SONIDO · " + (settings.sound ? "ACTIVADO" : "DESACTIVADO");
            labels[1].text = "DESTELLOS · " + (settings.reducedFlashes ? "REDUCIDOS" : "NORMALES");
            labels[2].text = "MOVIMIENTO · " + (settings.reducedMotion ? "REDUCIDO" : "NORMAL");
            labels[3].text = "EFECTOS · " + (settings.limitedEffects ? "SUAVES" : "COMPLETOS");
            labels[5].text = "MÚSICA · " + (settings.music ? "ACTIVADA" : "DESACTIVADA");
            EffectsVolume.SetValueWithoutNotify(settings.effectsVolume);
            MusicVolume.SetValueWithoutNotify(settings.musicVolume);
            effectsValue.text = "VOLUMEN DE EFECTOS · " + Mathf.RoundToInt(settings.effectsVolume * 100) + "%";
            musicValue.text = "VOLUMEN DE MÚSICA · " + Mathf.RoundToInt(settings.musicVolume * 100) + "%";
            labels[4].text = "VIBRACIÓN · " + (settings.vibration ? "EVENTOS IMPORTANTES" : "DESACTIVADA");
        }
    }
}
