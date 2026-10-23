using System;

namespace ReactorDeCaos
{
    public sealed partial class GameEngine
    {
        public int RebirthCount => state.rebirth.rebirths;
        public int Essence => state.rebirth.essence;
        public int EarnedEssence => state.rebirth.earnedEssence;
        public int Skin => state.rebirth.skin;
        public Specialization Specialization => state.rebirth.specialization;
        public double RebirthThreshold => config.rebirth.threshold;
        public double PermanentMultiplier => 1 + config.rebirth.essencePower * Math.Sqrt(EarnedEssence);
        public double SpecializationBonus(Specialization branch) => branch == Specialization.Overload
            ? config.rebirth.overloadSpecializationMultiplier : config.rebirth.specializationMultiplier;
        public double TapProductionFraction => config.rebirth.tapProductionFraction;
        private double TapPowerAt(int level) => ((config.baseTapPower + level * config.tapPowerPerLevel) * PermanentMultiplier
            + (Specialization == Specialization.Tap ? BaseProduction * TapProductionFraction : 0)) * BranchMultiplier(Specialization.Tap);
        public int StarterCoils => config.rebirth.starterCoils;
        public int StarterTapLevel => config.rebirth.starterTapLevel;
        public double AllTimeEnergy => Math.Min(EconomyConfig.MaxEnergy, state.rebirth.archivedEnergy + Lifetime);
        public int RebirthReward
        {
            get
            {
                if (Lifetime < RebirthThreshold || (Lifetime == RebirthThreshold && state.lifetimeRemainder < 0) || RebirthCount >= RebirthProgress.MaxRebirths) return 0;
                double raw = Math.Floor(config.rebirth.baseReward * Math.Sqrt(Lifetime / RebirthThreshold));
                return Math.Min(RebirthProgress.MaxEssence - EarnedEssence,
                    (int)Math.Min(config.rebirth.rewardCap, raw));
            }
        }
        public bool CanRebirth => CampaignAscensionAvailable && RebirthReward > 0;
        public double ProjectedPermanentMultiplier => 1 + config.rebirth.essencePower * Math.Sqrt(EarnedEssence + RebirthReward);
        public int RebirthSkinUnlock
        {
            get
            {
                for (int skin = 1; skin < RebirthCatalog.SkinCount; skin++)
                    if (!RebirthCatalog.SkinUnlocked(skin, RebirthCount) && RebirthCatalog.SkinUnlocked(skin, RebirthCount + 1)) return skin;
                return -1;
            }
        }
        public double NextEssenceTarget
        {
            get
            {
                if (Lifetime < RebirthThreshold) return RebirthThreshold;
                int reward = RebirthReward;
                if (reward >= config.rebirth.rewardCap || EarnedEssence + reward >= RebirthProgress.MaxEssence
                    || RebirthCount >= RebirthProgress.MaxRebirths) return double.PositiveInfinity;
                double factor = (reward + 1d) / config.rebirth.baseReward;
                return Math.Max(RebirthThreshold, RebirthThreshold * factor * factor);
            }
        }
        public double ProductionOf(string id)
        {
            RequireItem(id);
            if (!generators.ContainsKey(id)) throw new ArgumentException("Item is not a generator.");
            return EffectAtLevel(id, Level(id)) * (Overload.Active ? OverloadMultiplier : 1);
        }
        public int MasteryLevel(Specialization branch) { return state.rebirth.Level(branch); }
        public int MasteryCost(Specialization branch) { return MasteryLevel(branch) + 1; }
        public double MasteryMultiplier(Specialization branch, int level)
        {
            if (!RebirthCatalog.ValidBranch(branch) || level < 0 || level > RebirthProgress.MaxMastery)
                throw new ArgumentOutOfRangeException(nameof(level));
            return 1 + level * config.rebirth.masteryBonus;
        }
        public double BranchMultiplier(Specialization branch)
        {
            return MasteryMultiplier(branch, MasteryLevel(branch))
                * (Specialization == branch ? SpecializationBonus(branch) : 1);
        }
        public bool CanBuyMastery(Specialization branch)
        {
            return RebirthCatalog.ValidBranch(branch) && MasteryLevel(branch) < RebirthProgress.MaxMastery && Essence >= MasteryCost(branch);
        }
        public bool BuyMastery(Specialization branch)
        {
            if (!CanBuyMastery(branch)) return false;
            state.rebirth.essence -= MasteryCost(branch);
            state.rebirth.Increase(branch);
            return true;
        }
        public bool SelectSkin(int skin)
        {
            if (!RebirthCatalog.SkinUnlocked(skin, RebirthCount)) return false;
            state.rebirth.skin = skin;
            return true;
        }

        // A detached candidate: caller must save it successfully before replacing the live engine.
        // The displayed quote must still match; stale confirmations cannot silently reset a cycle.
        public bool TryPrepareRebirth(Specialization branch, int quotedReward, out GameEngine next)
        {
            next = null;
            if (!CanRebirth || !RebirthCatalog.ValidBranch(branch) || quotedReward != RebirthReward) return false;
            var permanent = state.rebirth.Copy();
            double archived = Math.Min(Lifetime, Math.Max(0,
                (EconomyConfig.MaxEnergy - permanent.archivedEnergy) - permanent.archivedRemainder));
            CompensatedSum.Add(ref permanent.archivedEnergy, ref permanent.archivedRemainder, archived);
            if (archived == Lifetime)
            {
                double low = state.lifetimeRemainder;
                if (low > 0) low = Math.Min(low, Math.Max(0, (EconomyConfig.MaxEnergy - permanent.archivedEnergy) - permanent.archivedRemainder));
                CompensatedSum.Add(ref permanent.archivedEnergy, ref permanent.archivedRemainder, low);
            }
            permanent.rebirths++;
            permanent.essence += quotedReward;
            permanent.earnedEssence += quotedReward;
            permanent.specialization = branch;
            for (int skin = 0; skin < RebirthCatalog.SkinCount; skin++)
                if (RebirthCatalog.SkinUnlocked(skin, permanent.rebirths) && !RebirthCatalog.SkinUnlocked(skin, RebirthCount)) permanent.skin = skin;
            next = new GameEngine(new GameState { version = 2, rebirth = permanent,
                coils = StarterCoils, tapLevel = StarterTapLevel, story = state.story.Copy(),
                unlockedAchievements = (string[])state.unlockedAchievements.Clone(),
                claimedAchievements = (string[])state.claimedAchievements.Clone() }, config);
            return true;
        }
    }
}
