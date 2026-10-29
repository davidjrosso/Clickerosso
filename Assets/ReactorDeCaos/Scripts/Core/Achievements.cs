using System;
using System.Collections.Generic;

namespace ReactorDeCaos
{
    public enum AchievementGoal { TotalEnergy, GeneratorLevel, Ascensions }

    [Serializable]
    public sealed class AchievementDefinition
    {
        public string id, title, description, generatorId;
        public AchievementGoal goal;
        public double target, energyReward;
        public AchievementDefinition Copy() => (AchievementDefinition)MemberwiseClone();
        public static AchievementDefinition[] Defaults() => new[]
        {
            New("first_spark", "Primera chispa", "Produce 100 E en total", AchievementGoal.TotalEnergy, 100, 25),
            New("first_machine", "Manos libres", "Instala un microgenerador", AchievementGoal.GeneratorLevel, 1, 25, "coil"),
            New("micro_network", "Red de energía", "Microgenerador de nivel 5", AchievementGoal.GeneratorLevel, 5, 100, "coil"),
            New("plasma_online", "Plasma estable", "Instala una bobina de plasma", AchievementGoal.GeneratorLevel, 1, 250, "plasma"),
            New("quantum_online", "Salto cuántico", "Instala una turbina cuántica", AchievementGoal.GeneratorLevel, 1, 1000, "quantum"),
            New("ten_thousand", "Energía sin límites", "Produce 10K E en total", AchievementGoal.TotalEnergy, 10000, 1000),
            New("million", "Potencia estelar", "Produce 1M E en total", AchievementGoal.TotalEnergy, 1000000, 10000),
            New("first_ascent", "Un nuevo comienzo", "Asciende por primera vez", AchievementGoal.Ascensions, 1, 100),
            New("three_ascents", "Más allá del núcleo", "Completa 3 ascensiones", AchievementGoal.Ascensions, 3, 500)
        };
        private static AchievementDefinition New(string id, string title, string description, AchievementGoal goal,
            double target, double reward, string generator = "") => new AchievementDefinition
            { id = id, title = title, description = description, goal = goal, target = target, energyReward = reward, generatorId = generator };
        internal static void ValidateCatalog(AchievementDefinition[] goals, EconomyConfig config)
        {
            if (goals == null || goals.Length > 64) throw new ArgumentException("Invalid achievement catalog.");
            var ids = new HashSet<string>();
            foreach (var entry in goals)
            {
                if (entry == null || !EconomyConfig.ValidId(entry.id) || !ids.Add(entry.id)
                    || string.IsNullOrWhiteSpace(entry.title) || entry.title.Length > 80
                    || string.IsNullOrWhiteSpace(entry.description) || entry.description.Length > 160
                    || entry.goal < AchievementGoal.TotalEnergy || entry.goal > AchievementGoal.Ascensions)
                    throw new ArgumentException("Invalid achievement definition.");
                EconomyConfig.Positive(entry.target); EconomyConfig.Positive(entry.energyReward);
                if (entry.goal == AchievementGoal.GeneratorLevel)
                {
                    config.Generator(entry.generatorId);
                    if (entry.target > EconomyConfig.MaxLevel || entry.target != Math.Floor(entry.target))
                        throw new ArgumentException("Unattainable generator achievement.");
                }
                if (entry.goal == AchievementGoal.Ascensions && (entry.target > RebirthProgress.MaxRebirths || entry.target != Math.Floor(entry.target)))
                    throw new ArgumentException("Unattainable ascension achievement.");
            }
        }
    }

    public sealed partial class GameEngine
    {
        private bool claiming;
        public int AchievementCount => config.achievements.Length;
        public AchievementDefinition AchievementAt(int index) => config.achievements[index].Copy();
        public bool AchievementUnlocked(string id) => Array.IndexOf(state.unlockedAchievements, id) >= 0;
        public bool AchievementClaimed(string id) => Array.IndexOf(state.claimedAchievements, id) >= 0;
        public int AvailableAchievements
        {
            get { int count = 0; foreach (var goal in config.achievements) if (CanClaimAchievement(goal.id)) count++; return count; }
        }
        public double AchievementProgress(AchievementDefinition goal)
        {
            if (AchievementUnlocked(goal.id)) return goal.target;
            switch (goal.goal)
            {
                case AchievementGoal.GeneratorLevel: return Math.Min(goal.target, Level(goal.generatorId));
                case AchievementGoal.Ascensions: return Math.Min(goal.target, RebirthCount);
                default: return Math.Min(goal.target, AllTimeEnergy);
            }
        }
        private void EvaluateAchievements()
        {
            foreach (var goal in config.achievements)
            {
                if (AchievementUnlocked(goal.id) || AchievementProgress(goal) < goal.target) continue;
                if (state.unlockedAchievements.Length >= 128) throw new InvalidOperationException("Achievement ledger full.");
                Array.Resize(ref state.unlockedAchievements, state.unlockedAchievements.Length + 1);
                state.unlockedAchievements[state.unlockedAchievements.Length - 1] = goal.id;
            }
        }
        public bool CanClaimAchievement(string id)
        {
            var goal = Array.Find(config.achievements, x => x.id == id);
            return goal != null && CampaignAchievementVisible(goal) && AchievementUnlocked(id) && !AchievementClaimed(id)
                && (EconomyConfig.MaxEnergy - Energy) - state.energyRemainder >= goal.energyReward
                && (EconomyConfig.MaxEnergy - Lifetime) - state.lifetimeRemainder >= goal.energyReward;
        }
        // Persistence accepts a detached candidate before the live balance changes. Combo and
        // overload timers belong to this instance and are never reset by claiming a reward.
        public bool TryClaimAchievement(string id, Func<GameState, bool> persist)
        {
            if (claiming || writingStation || writingStory || persist == null || !CanClaimAchievement(id)) return false;
            claiming = true;
            try
            {
                var candidate = new GameEngine(Snapshot(), config);
                var goal = Array.Find(config.achievements, x => x.id == id);
                Array.Resize(ref candidate.state.claimedAchievements, candidate.state.claimedAchievements.Length + 1);
                candidate.state.claimedAchievements[candidate.state.claimedAchievements.Length - 1] = id;
                candidate.Credit(goal.energyReward);
                if (!persist(candidate.Snapshot())) return false;
                state.energy = candidate.state.energy; state.energyRemainder = candidate.state.energyRemainder;
                state.lifetimeEnergy = candidate.state.lifetimeEnergy; state.lifetimeRemainder = candidate.state.lifetimeRemainder;
                state.unlockedAchievements = candidate.state.unlockedAchievements;
                state.claimedAchievements = candidate.state.claimedAchievements;
                EvaluateMilestones(true);
                return true;
            }
            finally { claiming = false; }
        }
    }
}
