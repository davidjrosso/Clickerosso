using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class ReactorAssembly
    {
        private sealed class Module
        {
            public string id;
            public bool generator;
            public Button button;
            public Image icon;
            public RingGraphic frame;
            public TextMeshProUGUI label;
            public ConduitGraphic link;
            public int level = -1;
            public bool unlocked;
            public float installation;
        }
        private readonly List<Module> modules = new List<Module>();
        private readonly ReactorHud hud;
        private readonly CrownGraphic crown;
        private readonly ComboSegmentsGraphic combo;
        private float tapPulse;
        public int ModuleCount => modules.Count;
        public Button ButtonFor(string id) { return modules.Find(x => x.id == id)?.button; }
        public static Color SkinColor(int skin)
        {
            switch (skin)
            {
                case 1: return ReactorHud.Warm;
                case 2: return ReactorHud.Magenta;
                case 3: return new Color(.45f, 1, .65f);
                default: return ReactorHud.Cyan;
            }
        }
        public ReactorAssembly(ReactorHud view, Transform viewport, GameEngine game)
        {
            hud = view;
            crown = ReactorHud.Rect("EvolutionCrown", hud.CoreVisual, -.13f, -.13f, 1.13f, 1.13f).gameObject.AddComponent<CrownGraphic>();
            crown.raycastTarget = false;
            combo = ReactorHud.Rect("ComboSegments", hud.CoreVisual, -.02f, -.02f, 1.02f, 1.02f).gameObject.AddComponent<ComboSegmentsGraphic>();
            combo.raycastTarget = false;
            var generators = new List<ShopEntry>();
            foreach (var item in game.Shop) if (item.IsGenerator) generators.Add(item);
            for (int i = 0; i < generators.Count; i++)
            {
                // Two side columns; catalog additions reuse this component instead of new scripts.
                int row = i / 2, rows = (generators.Count + 1) / 2;
                float y = rows == 1 ? .6f : Mathf.Lerp(.77f, .33f, row / (float)(rows - 1));
                Add(generators[i].Id, true, viewport, new Vector2(i % 2 == 0 ? .13f : .87f, y), .105f, .16f);
            }
            Add("tap", false, viewport, new Vector2(.31f, .10f), .065f, .095f);
            Add("combo", false, viewport, new Vector2(.50f, .10f), .065f, .095f);
            Add("overload", false, viewport, new Vector2(.69f, .10f), .065f, .095f);
        }
        private void Add(string id, bool generator, Transform parent, Vector2 position, float halfWidth, float halfHeight)
        {
            var link = ReactorHud.Rect("Conduit_" + id, parent, 0, 0, 1, 1).gameObject.AddComponent<ConduitGraphic>();
            link.raycastTarget = false; link.source = position;
            // Place conduits behind the central core and its transparent tap target.
            link.transform.SetAsFirstSibling();
            Button button = ReactorHud.Button("Module_" + id, parent, new Color(.025f, .045f, .09f, .9f),
                position.x - halfWidth, position.y - halfHeight, position.x + halfWidth, position.y + halfHeight);
            button.onClick.AddListener(() => hud.OpenModule(id));
            Image icon = ReactorHud.Image("ModuleIcon", button.transform, Color.white, .13f, .26f, .87f, .94f);
            icon.sprite = hud.Icon(id); icon.preserveAspect = true;
            var frame = ReactorHud.Ring("ModuleHousing", button.transform, ReactorHud.Cyan);
            frame.thickness = 2; frame.fraction = .78f;
            var label = hud.Label("ModuleLevel", button.transform, "", generator ? 21 : 18, Color.white, .01f, .01f, .99f, .25f);
            modules.Add(new Module { id = id, generator = generator, button = button, icon = icon, frame = frame, label = label, link = link });
        }
        public void Install(string id)
        {
            foreach (var module in modules) if (module.id == id) module.installation = 1;
        }
        public void Tap() { tapPulse = 1; }
        public void Reset()
        {
            tapPulse = 0;
            foreach (var module in modules) { module.installation = 0; module.level = -1; module.button.transform.localScale = Vector3.one; }
        }
        public void Animate(GameEngine game)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            tapPulse = Mathf.MoveTowards(tapPulse, 0, dt * 4);
            Color skin = SkinColor(game.Skin);
            crown.Configure(game.Stage, game.Skin); crown.color = skin;
            combo.SetCombo(game.ComboCap <= 1 ? 0 : (float)((game.ComboMultiplier - 1) / (game.ComboCap - 1)));
            combo.color = game.Combo.Decaying ? ReactorHud.Violet : skin;
            hud.CoreImage.rectTransform.localScale = Vector3.one * (.82f + game.Stage * .045f);
            int index = 0;
            foreach (var module in modules)
            {
                int level = game.Level(module.id);
                bool unlocked = game.Unlocked(module.id);
                if (level != module.level || unlocked != module.unlocked)
                {
                    module.level = level; module.unlocked = unlocked;
                    module.label.text = !unlocked ? "BLOQUEADO" : level == 0 ? "INSTALAR" : "NV. " + level;
                }
                module.installation = Mathf.MoveTowards(module.installation, 0, dt * 1.6f);
                bool running = module.generator ? level > 0 : module.id == "tap" ? tapPulse > 0
                    : module.id == "combo" ? game.Combo.Count > 0 : game.Overload.Active || game.Overload.Ready;
                Color color = game.Overload.Active ? ReactorHud.Warm : module.generator ? skin : ReactorHud.Violet;
                module.icon.color = level > 0 || running ? Color.white : new Color(.45f, .5f, .6f, .55f);
                Color frame = color; frame.a = level > 0 || running ? .85f : .25f;
                module.frame.color = frame;
                module.link.color = frame;
                bool motion = !hud.Preferences.reducedMotion;
                float phase = motion && !hud.Preferences.limitedEffects ? Mathf.Repeat(Time.unscaledTime * (game.Overload.Active ? 1.4f : .35f)
                    + (game.Overload.Active ? 0 : index * .17f), 1) : .5f;
                module.link.Configure(phase, level, running && !hud.Preferences.limitedEffects);
                module.button.transform.localScale = Vector3.one * (1 + (motion ? module.installation * .12f : 0));
                if (module.generator && level > 0 && motion && !hud.Preferences.limitedEffects)
                    module.frame.rectTransform.localEulerAngles += Vector3.forward * dt * (game.Overload.Active ? 35 : 4 + Mathf.Min(level, 30));
                index++;
            }
        }
    }
}
