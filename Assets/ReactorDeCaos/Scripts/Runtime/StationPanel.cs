using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class StationPanel
    {
        private readonly ReactorApp app;
        private readonly GameObject root;
        private readonly TextMeshProUGUI summary, next, offline, expedition, message, memory;
        private readonly TextMeshProUGUI[] roomText, roomAction;
        private readonly StationVistaGraphic vista;
        private readonly Button[] memories;
        public Button[] RoomButtons { get; }
        public Button OfflineButton { get; }
        public Button ExpeditionButton { get; }
        public Button MemoryButton { get; }
        public Button ArchiveButton { get; }
        public bool IsOpen => root.activeSelf;
        public StationPanel(ReactorHud hud, Transform parent, ReactorApp owner)
        {
            app = owner;
            var surface = ReactorHud.Image("StationPage", parent, new Color(.018f, .03f, .05f, .98f), .035f, .145f, .965f, .815f);
            surface.raycastTarget = true; root = surface.gameObject;
            hud.Label("StationHeading", surface.transform, "ORFEO · UN HOGAR", 32, Color.white, .04f, .92f, .65f, .99f, TextAlignmentOptions.Left);
            summary = hud.Label("StationSummary", surface.transform, "", 24, ReactorHud.Cyan, .04f, .85f, .65f, .925f, TextAlignmentOptions.Left);
            ArchiveButton = ReactorHud.Button("StationArchive", surface.transform, new Color(.035f, .14f, .19f), .68f, .845f, .97f, .99f);
            hud.Label("StationArchiveLabel", ArchiveButton.transform, "ARCHIVO", 26, Color.white, .025f, .05f, .975f, .95f);
            ArchiveButton.onClick.AddListener(() => hud.SelectTab(4));
            var viewport = ReactorHud.Rect("StationViewport", surface.transform, .025f, .025f, .975f, .83f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            var content = ReactorHud.Rect("StationContent", viewport, 0, 1, 1, 1); content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 36); layout.spacing = 24;
            layout.childControlHeight = layout.childControlWidth = layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            next = Text(hud, "StationNext", content, ReactorHud.Cyan);
            var illustration = ReactorHud.Rect("StationVista", content, 0, 0, 1, 1);
            illustration.gameObject.AddComponent<LayoutElement>().preferredHeight = 480;
            vista = illustration.gameObject.AddComponent<StationVistaGraphic>(); vista.raycastTarget = false;
            offline = Text(hud, "StationAbsence", content, Color.white);
            OfflineButton = Action(hud, "ClaimAbsence", content, "RECLAMAR PRODUCCIÓN", app.ClaimOffline);
            expedition = Text(hud, "StationExpedition", content, Color.white);
            ExpeditionButton = Action(hud, "ExpeditionAction", content, "ENVIAR DRON", () => {
                if (app.Game.ExpeditionReady) app.ClaimExpedition(); else app.StartExpedition();
            });
            roomText = new TextMeshProUGUI[3]; roomAction = new TextMeshProUGUI[3]; RoomButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int room = i;
                roomText[i] = Text(hud, "StationRoom" + i, content, Color.white);
                RoomButtons[i] = Action(hud, "RestoreRoom" + i, content, "RESTAURAR", () => app.RestoreRoom(room));
                roomAction[i] = RoomButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            }
            memory = Text(hud, "StationMemory", content, ReactorHud.Warm);
            MemoryButton = Action(hud, "StationContinueMemory", content, "ESCUCHAR A MARA", () => app.Narrative.PresentStationMemory(app.Game.PendingStationMemory));
            memories = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                memories[i] = Action(hud, "StationReplay" + i, content, "REPETIR · " + StationCatalog.Memory(i), () => app.Narrative.PresentStationMemory(index, true));
            }
            message = Text(hud, "StationMessage", content, ReactorHud.Warm);
            root.SetActive(false);
        }
        private static TextMeshProUGUI Text(ReactorHud hud, string name, Transform parent, Color color)
        {
            var label = hud.Label(name, parent, "", 30, color, 0, 0, 1, 1, TextAlignmentOptions.TopLeft);
            label.enableAutoSizing = false; label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }
        private static Button Action(ReactorHud hud, string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var button = ReactorHud.Button(name, parent, new Color(.035f, .16f, .19f), 0, 0, 1, 1);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 180;
            hud.Label(name + "Label", button.transform, label, 28, Color.white, .025f, .05f, .975f, .95f);
            button.onClick.AddListener(action); return button;
        }
        public void Open() { root.SetActive(true); message.text = "Las salas y los recuerdos permanecen al ascender. Nada caduca."; Refresh(); }
        public void Close() { root.SetActive(false); }
        public void ShowMessage(string text) { message.text = text; }
        public static string Duration(double seconds)
        {
            int total = Mathf.CeilToInt((float)System.Math.Min(seconds, 2147480000));
            return total >= 3600 ? (total / 3600) + " h " + (total % 3600 / 60) + " min" : (total / 60) + " min " + (total % 60) + " s";
        }
        public void Refresh()
        {
            if (!IsOpen) return;
            var game = app.Game;
            summary.text = game.RestoredRooms + " / 3 salas · producción +" + ((game.StationProductionMultiplier - 1) * 100).ToString("0") + "%";
            next.text = "SIGUIENTE PASO\n" + game.StationNextStep;
            vista.Present(game.RestoredRooms, game.StationMemories == 4, game.ExpeditionActive, hudMotion());
            offline.text = game.PendingOfflineEnergy > 0 ? "MIENTRAS NO ESTABAS\n+" + NumberFormat.Compact(game.PendingOfflineEnergy)
                + " E · " + Duration(game.PendingOfflineSeconds) + " de producción\nLa reserva espera hasta que la reclames."
                : "PRODUCCIÓN EN AUSENCIA\nTus generadores trabajan hasta 8 horas por ausencia, sin combo ni sobrecarga. Reclama al volver, cuando quieras.";
            OfflineButton.gameObject.SetActive(game.PendingOfflineEnergy > 0);
            OfflineButton.interactable = game.CanReceiveStationEnergy(game.PendingOfflineEnergy);
            expedition.text = "EXPEDICIÓN · JARDÍN EXTERIOR\n" + (game.ExpeditionReady ? "El dron regresó. " + (game.ExpeditionClaims == 0 ? "Trae una cápsula de semillas y " : "Trae suministros y ")
                + NumberFormat.Compact(game.ExpeditionReward) + " E."
                : game.ExpeditionActive ? "En ruta · " + Duration(game.ExpeditionRemaining) + " restantes\nRecompensa confirmada: " + NumberFormat.Compact(game.ExpeditionReward) + " E"
                : game.CanStartExpedition ? "Viaje de " + Duration(StationCatalog.ExpeditionSeconds) + ". Recompensa: " + NumberFormat.Compact(game.ProjectedExpeditionReward) + " E.\nEl primer viaje recupera una cápsula para Mara. Los siguientes traen suministros."
                : "Restaura el invernadero y escucha la transmisión de Mara.")
                + "\nUn dron a la vez. Continúa abierto o cerrado; el resultado no caduca.";
            ExpeditionButton.interactable = game.CanStartExpedition || (game.ExpeditionReady && game.CanReceiveStationEnergy(game.ExpeditionReward));
            ExpeditionButton.GetComponentInChildren<TextMeshProUGUI>().text = game.ExpeditionReady ? "RECIBIR DRON · +" + NumberFormat.Compact(game.ExpeditionReward) + " E"
                : game.ExpeditionActive ? "DRON EN RUTA" : "ENVIAR DRON · SIN COSTE";
            for (int i = 0; i < 3; i++)
            {
                var room = StationCatalog.Room(i); bool restored = game.RestoredRooms > i;
                roomText[i].text = room.Name + (restored ? " · EN LÍNEA" : " · POR RECUPERAR") + "\n" + room.Purpose
                    + "\n+" + (room.ProductionBonus * 100).ToString("0") + "% de producción permanente" + (restored ? " · beneficio activo" : "\n" + game.RoomRequirement(i));
                RoomButtons[i].interactable = game.CanRestoreRoom(i);
                roomAction[i].text = restored ? "SALA EN LÍNEA" : "RESTAURAR · " + NumberFormat.Compact(room.Cost) + " E";
            }
            memory.text = "MARA · UNA FLOR PARA VOLVER\n" + game.StationMemories + " / 4 recuerdos"
                + (game.StationMemories == 4 ? "\nLa flor encontró su luz. Mara tiene un lugar al que volver. Historia completada." : "\nLos recuerdos se recuperan al restaurar las salas y recibir la cápsula.");
            MemoryButton.gameObject.SetActive(game.PendingStationMemory >= 0);
            for (int i = 0; i < memories.Length; i++) memories[i].gameObject.SetActive(i < game.StationMemories);
        }
        private bool hudMotion() => !app.Preferences.reducedMotion && !app.Preferences.limitedEffects;
    }
}
