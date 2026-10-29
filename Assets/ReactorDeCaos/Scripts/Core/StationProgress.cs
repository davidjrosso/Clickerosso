using System;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class StationProgress
    {
        public int rooms, memories, expeditionClaims;
        public bool expeditionActive;
        public double expeditionRemaining, expeditionReward;
        public long lastUtcSeconds, pendingSeconds;
        public double offlineRate, pendingEnergy;
        public StationProgress Copy() => (StationProgress)MemberwiseClone();
        public bool IsValid() => rooms >= 0 && rooms <= 3 && memories >= 0 && memories <= 4
            && expeditionClaims >= 0 && expeditionClaims <= 1000000
            && (expeditionClaims == 0 || (rooms >= 1 && memories >= 1))
            && (rooms < 2 || (expeditionClaims > 0 && memories >= 2)) && (rooms < 3 || memories >= 3)
            && (memories == 0 || rooms >= 1) && (memories < 2 || expeditionClaims > 0)
            && (memories < 3 || rooms >= 2) && (memories < 4 || rooms == 3)
            && (!expeditionActive || (rooms >= 1 && memories >= 1 && expeditionReward > 0))
            && (expeditionActive || (expeditionRemaining == 0 && expeditionReward == 0))
            && Finite(expeditionRemaining, StationCatalog.ExpeditionSeconds)
            && Finite(expeditionReward, StationCatalog.MaxExpeditionReward)
            && Finite(offlineRate, EconomyConfig.MaxEnergy) && Finite(pendingEnergy, EconomyConfig.MaxEnergy)
            && lastUtcSeconds >= 0 && lastUtcSeconds <= StationCatalog.MaxUtcSeconds
            && pendingSeconds >= 0 && pendingSeconds <= StationCatalog.MaxUtcSeconds;
        private static bool Finite(double n, double max) => !double.IsNaN(n) && !double.IsInfinity(n) && n >= 0 && n <= max;
    }

    public sealed class StationRoom
    {
        public string Name { get; }
        public string Purpose { get; }
        public double Cost { get; }
        public int Chapter { get; }
        public double ProductionBonus { get; }
        internal StationRoom(string name, string purpose, double cost, int chapter, double bonus)
        { Name = name; Purpose = purpose; Cost = cost; Chapter = chapter; ProductionBonus = bonus; }
    }
    public static class StationCatalog
    {
        public const int RoomCount = 3, MemoryCount = 4;
        public const long MaxUtcSeconds = 253402300799L, OfflineCapSeconds = 8 * 60 * 60;
        public const double ExpeditionSeconds = 180, MinExpeditionReward = 200, MaxExpeditionReward = 50000;
        public const double ExpeditionProductionSeconds = 60;
        private static readonly StationRoom[] rooms = {
            new StationRoom("INVERNADERO", "Devuelve la luz al jardín de Mara y recupera su baliza exterior.", 150, 1, .05),
            new StationRoom("ENFERMERÍA", "Repara la cápsula de semillas con el equipo de conservación.", 2500, 3, .05),
            new StationRoom("OBSERVATORIO", "Orienta los espejos hacia la estrella de Mara y haz florecer el jardín.", 12000, 4, .05)
        };
        private static readonly string[] memories = { "El jardín apagado", "Una semilla entre los restos", "Lo que sigue vivo", "Una flor para volver" };
        public static StationRoom Room(int index) => rooms[index];
        public static string Memory(int index) => memories[index];
    }

    public sealed partial class GameEngine
    {
        private bool writingStation;
        public bool StationAvailable => CampaignActive && NarrativeIntroSeen && StoryCompleted >= 1;
        public int RestoredRooms => state.station.rooms;
        public int StationMemories => state.station.memories;
        public int ExpeditionClaims => state.station.expeditionClaims;
        public bool ExpeditionActive => state.station.expeditionActive;
        public double ExpeditionRemaining => state.station.expeditionRemaining;
        public double ExpeditionReward => state.station.expeditionReward;
        public double ProjectedExpeditionReward => Math.Max(StationCatalog.MinExpeditionReward,
            Math.Min(StationCatalog.MaxExpeditionReward, BaseProduction * StationCatalog.ExpeditionProductionSeconds));
        public bool ExpeditionReady => ExpeditionActive && ExpeditionRemaining <= 0;
        public double PendingOfflineEnergy => state.station.pendingEnergy;
        public long PendingOfflineSeconds => state.station.pendingSeconds;
        public double StationProductionMultiplier
        {
            get { double bonus = 1; for (int i = 0; i < RestoredRooms; i++) bonus += StationCatalog.Room(i).ProductionBonus; return bonus; }
        }
        public int PendingStationMemory => StationMemories == 0 && RestoredRooms >= 1 ? 0
            : StationMemories == 1 && ExpeditionClaims > 0 ? 1
            : StationMemories == 2 && RestoredRooms >= 2 ? 2
            : StationMemories == 3 && RestoredRooms >= 3 ? 3 : -1;
        public string StationNextStep => !StationAvailable ? "Restaura el soporte vital para abrir las salas" : PendingStationMemory >= 0 ? "Mara tiene algo que contarte"
            : RestoredRooms == 0 ? "Restaura el invernadero para recuperar el jardín de Mara"
            : ExpeditionClaims == 0 ? ExpeditionReady ? "El dron volvió · recoge la cápsula" : ExpeditionActive ? "El dron busca una cápsula en el jardín exterior" : "Envía el dron al jardín exterior"
            : RestoredRooms == 1 ? "Restaura la enfermería para reparar la cápsula"
            : RestoredRooms == 2 ? "Restaura el observatorio para iluminar el jardín" : "La flor de Mara vive · Orfeo vuelve a ser un hogar";
        public string RoomRequirement(int index)
        {
            var room = StationCatalog.Room(index);
            if (!StationAvailable || StoryCompleted < room.Chapter) return "Completa «" + StoryCatalog.At(room.Chapter - 1).Title + "»";
            if (index > RestoredRooms) return "Restaura primero " + StationCatalog.Room(index - 1).Name.ToLowerInvariant();
            if (index == 1 && (ExpeditionClaims == 0 || StationMemories < 2)) return "Recoge la cápsula del dron y escucha a Mara";
            if (index == 2 && StationMemories < 3) return "Escucha a Mara tras reparar la cápsula";
            return "";
        }
        private bool CanSpend(double amount) => Energy > amount || (Energy == amount && state.energyRemainder >= 0);
        public bool CanRestoreRoom(int index) => index >= 0 && index < StationCatalog.RoomCount && index == RestoredRooms
            && RoomRequirement(index).Length == 0 && CanSpend(StationCatalog.Room(index).Cost);
        public bool CanStartExpedition => StationAvailable && RestoredRooms >= 1 && StationMemories >= 1
            && !ExpeditionActive && ExpeditionClaims < 1000000;
        public bool CanReceiveStationEnergy(double amount) => amount > 0
            && (EconomyConfig.MaxEnergy - Energy) - state.energyRemainder >= amount
            && (EconomyConfig.MaxEnergy - Lifetime) - state.lifetimeRemainder >= amount;
        public bool TryRestoreRoom(int index, Func<GameState, bool> persist)
        {
            if (!CanRestoreRoom(index)) return false;
            return CommitStation(candidate => {
                CompensatedSum.Add(ref candidate.state.energy, ref candidate.state.energyRemainder, -StationCatalog.Room(index).Cost);
                candidate.state.station.rooms++; return true;
            }, persist);
        }
        public bool TryStartExpedition(Func<GameState, bool> persist)
        {
            if (!CanStartExpedition) return false;
            return CommitStation(candidate => {
                var s = candidate.state.station;
                s.expeditionActive = true; s.expeditionRemaining = StationCatalog.ExpeditionSeconds;
                s.expeditionReward = candidate.ProjectedExpeditionReward;
                return true;
            }, persist);
        }
        public bool TryClaimExpedition(Func<GameState, bool> persist)
        {
            if (!ExpeditionReady || !CanReceiveStationEnergy(ExpeditionReward)) return false;
            return CommitStation(candidate => {
                candidate.Credit(candidate.ExpeditionReward);
                var s = candidate.state.station; s.expeditionClaims++;
                s.expeditionActive = false; s.expeditionRemaining = s.expeditionReward = 0; return true;
            }, persist);
        }
        public bool TryFinishStationMemory(int index, Func<GameState, bool> persist)
        {
            if (index < 0 || index != PendingStationMemory) return false;
            return CommitStation(candidate => { candidate.state.station.memories++; return true; }, persist);
        }
        public bool TryClaimOffline(Func<GameState, bool> persist)
        {
            if (!CanReceiveStationEnergy(PendingOfflineEnergy)) return false;
            return CommitStation(candidate => {
                candidate.Credit(candidate.PendingOfflineEnergy);
                candidate.state.station.pendingEnergy = 0; candidate.state.station.pendingSeconds = 0;
                return true;
            }, persist);
        }
        // UTC high-water mark ignores backward jumps. Local clocks are not server-authoritative.
        // suspensionUtc is an in-session boundary, including when the pause checkpoint failed.
        public bool TryResumeStation(long now, Func<GameState, bool> persist, long suspensionUtc = 0)
        {
            ValidateStationTime(now); ValidateStationTime(suspensionUtc);
            return CommitStation(candidate => {
                var s = candidate.state.station;
                long start = Math.Max(s.lastUtcSeconds, suspensionUtc);
                long elapsed = start == 0 ? 0 : Math.Max(0, now - start);
                long creditedSeconds = Math.Min(StationCatalog.OfflineCapSeconds, elapsed);
                double rate = suspensionUtc > 0 ? BaseProduction : s.offlineRate;
                if (CampaignActive && NarrativeIntroSeen && creditedSeconds > 0 && rate > 0)
                {
                    double earned = Math.Min(EconomyConfig.MaxEnergy, rate * creditedSeconds);
                    s.pendingEnergy = Math.Min(EconomyConfig.MaxEnergy, s.pendingEnergy + earned);
                    s.pendingSeconds = Math.Min(StationCatalog.MaxUtcSeconds, s.pendingSeconds + creditedSeconds);
                }
                if (s.expeditionActive) s.expeditionRemaining = Math.Max(0, s.expeditionRemaining - elapsed);
                s.lastUtcSeconds = Math.Max(now, start);
                s.offlineRate = Math.Min(EconomyConfig.MaxEnergy, candidate.BaseProduction);
                return true;
            }, persist);
        }
        // Every persisted action records the matching rate. This also covers interrupted sessions.
        public bool TryCheckpointStation(long now, Func<GameState, bool> persist)
        {
            ValidateStationTime(now);
            return CommitStation(candidate => {
                candidate.state.station.lastUtcSeconds = Math.Max(now, candidate.state.station.lastUtcSeconds);
                candidate.state.station.offlineRate = Math.Min(EconomyConfig.MaxEnergy, candidate.BaseProduction);
                return true;
            }, persist);
        }
        private static void ValidateStationTime(long time)
        {
            if (time < 0 || time > StationCatalog.MaxUtcSeconds) throw new ArgumentOutOfRangeException(nameof(time));
        }
        private bool CommitStation(Func<GameEngine, bool> change, Func<GameState, bool> persist)
        {
            if (writingStation || writingStory || claiming || persist == null) return false;
            writingStation = true;
            try
            {
                var candidate = new GameEngine(Snapshot(), config);
                if (!change(candidate)) return false;
                candidate.state.station.offlineRate = Math.Min(EconomyConfig.MaxEnergy, candidate.BaseProduction);
                if (!persist(candidate.Snapshot())) return false;
                state.energy = candidate.state.energy; state.energyRemainder = candidate.state.energyRemainder;
                state.lifetimeEnergy = candidate.state.lifetimeEnergy; state.lifetimeRemainder = candidate.state.lifetimeRemainder;
                state.station = candidate.state.station.Copy();
                state.unlockedAchievements = (string[])candidate.state.unlockedAchievements.Clone();
                EvaluateMilestones(true);
                return true;
            }
            finally { writingStation = false; }
        }
    }
}
