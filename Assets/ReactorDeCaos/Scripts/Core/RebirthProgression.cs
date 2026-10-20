using System;

namespace ReactorDeCaos
{
    public enum Specialization { None, Tap, Automation, Overload }

    [Serializable]
    public sealed class RebirthConfig
    {
        public double threshold = 1000000;
        public int baseReward = 3;
        public int rewardCap = 100000;
        public double essencePower = .25;
        public double masteryBonus = .05;
        public double specializationMultiplier = 1.5;
        public double overloadSpecializationMultiplier = 1.8;
        public double tapProductionFraction = .08;
        public int starterCoils = 1;
        public int starterTapLevel = 1;
        public RebirthConfig Copy() { return (RebirthConfig)MemberwiseClone(); }
        public void Validate()
        {
            EconomyConfig.Positive(threshold);
            EconomyConfig.Positive(essencePower);
            EconomyConfig.Positive(masteryBonus);
            EconomyConfig.Positive(specializationMultiplier);
            EconomyConfig.Positive(overloadSpecializationMultiplier);
            if (threshold < 1000 || baseReward < 1 || baseReward > 100 || rewardCap < baseReward || rewardCap > 1000000
                || overloadSpecializationMultiplier < 1 || overloadSpecializationMultiplier > 5
                || essencePower > 2 || masteryBonus > 1 || specializationMultiplier < 1 || specializationMultiplier > 5
                || double.IsNaN(tapProductionFraction) || tapProductionFraction < 0 || tapProductionFraction > .5
                || starterCoils < 0 || starterCoils > 5 || starterTapLevel < 0 || starterTapLevel > 5)
                throw new ArgumentException("Invalid rebirth configuration.");
        }
    }

    [Serializable]
    public sealed class RebirthProgress
    {
        public const int MaxEssence = 1000000000;
        public const int MaxMastery = 20;
        public const int MaxRebirths = 1000000;
        public int rebirths, essence, earnedEssence;
        public int tapMastery, automationMastery, overloadMastery;
        public Specialization specialization;
        public int skin;
        public double archivedEnergy, archivedRemainder;
        public RebirthProgress Copy() { return (RebirthProgress)MemberwiseClone(); }
        public int Level(Specialization branch)
        {
            switch (branch)
            {
                case Specialization.Tap: return tapMastery;
                case Specialization.Automation: return automationMastery;
                case Specialization.Overload: return overloadMastery;
                default: throw new ArgumentOutOfRangeException(nameof(branch));
            }
        }
        public void Increase(Specialization branch)
        {
            switch (branch)
            {
                case Specialization.Tap: tapMastery++; break;
                case Specialization.Automation: automationMastery++; break;
                case Specialization.Overload: overloadMastery++; break;
                default: throw new ArgumentOutOfRangeException(nameof(branch));
            }
        }
        public bool IsValid()
        {
            if (rebirths < 0 || rebirths > MaxRebirths || essence < 0 || earnedEssence < essence
                || earnedEssence > MaxEssence || rebirths > earnedEssence
                || (rebirths == 0 && (earnedEssence != 0 || specialization != Specialization.None))
                || (rebirths > 0 && !RebirthCatalog.ValidBranch(specialization))
                || !ValidLevel(tapMastery) || !ValidLevel(automationMastery) || !ValidLevel(overloadMastery)
                || !RebirthCatalog.SkinUnlocked(skin, rebirths)
                || double.IsNaN(archivedEnergy) || double.IsInfinity(archivedEnergy) || archivedEnergy < 0
                || archivedEnergy > EconomyConfig.MaxEnergy || !CompensatedSum.IsValid(archivedEnergy, archivedRemainder)) return false;
            int spent = TotalCost(tapMastery) + TotalCost(automationMastery) + TotalCost(overloadMastery);
            return essence + spent == earnedEssence && (rebirths != 0 || archivedEnergy == 0);
        }
        private static bool ValidLevel(int value) { return value >= 0 && value <= MaxMastery; }
        private static int TotalCost(int level) { return level * (level + 1) / 2; }
    }

    public static class RebirthCatalog
    {
        // Persisted indices: append new skins or migrate saves; never reorder these entries.
        public const int SkinCount = 4;
        private static readonly string[] skins = { "AURORA", "CORONA SOLAR", "VACÍO VIOLETA", "PRISMA" };
        private static readonly int[] skinRebirths = { 0, 1, 3, 5 };
        private static readonly string[] forms = { "SEMILLA", "PLASMA", "CÁMARA CUÁNTICA", "ESTRELLA ARTIFICIAL", "SINGULARIDAD" };
        public static bool ValidBranch(Specialization branch) { return branch >= Specialization.Tap && branch <= Specialization.Overload; }
        public static bool SkinUnlocked(int skin, int rebirths) { return skin >= 0 && skin < SkinCount && rebirths >= skinRebirths[skin]; }
        public static string SkinName(int skin) { return skins[skin]; }
        public static int SkinRequirement(int skin) { return skinRebirths[skin]; }
        public static string FormName(int stage) { return forms[Math.Max(0, Math.Min(4, stage))]; }
        public static string BranchName(Specialization branch)
        {
            switch (branch)
            {
                case Specialization.Tap: return "TOQUES";
                case Specialization.Automation: return "AUTOMATIZACIÓN";
                case Specialization.Overload: return "SOBRECARGA";
                default: return "SIN ESPECIALIZACIÓN";
            }
        }
    }
}
