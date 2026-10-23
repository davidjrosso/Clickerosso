using System;
using System.IO;
using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using CampaignCodec = ReactorDeCaos.UnityJsonCodec;
#else
using CampaignCodec = ReactorDeCaos.Tests.NetJsonCodec;
#endif

namespace ReactorDeCaos.Tests
{
    public sealed class CampaignTests
    {
        private static GameEngine Fresh() => new GameEngine(GameEngine.NewCampaignState());
        private static void Intro(GameEngine game)
        {
            Assert.That(game.IgniteCampaign(), Is.EqualTo(1));
            Assert.That(game.TryFinishNarrative(-1, true, _ => true), Is.True);
        }
        // A checkpoint fixture isolates each gate; the simulation exercises all six from zero.
        private static GameEngine Checkpoint(int chapter, double energy = 1000000)
        {
            var state = GameEngine.NewCampaignState();
            state.energy = state.lifetimeEnergy = energy;
            state.story.completed = state.story.read = state.story.narrative.scenesSeen = chapter;
            state.story.narrative.introSeen = state.story.narrative.briefingSeen = true;
            if (chapter == 6) state.story.choice = 1;
            return new GameEngine(state);
        }
        [Test]
        public void OpeningRequiresARealIgnitionAndOnlyAwardsItOnce()
        {
            var game = Fresh();
            Assert.That(game.Tap(), Is.Zero);
            Assert.That(game.TryFinishNarrative(-1, true, _ => true), Is.False);
            foreach (var item in game.Shop) Assert.That(game.Unlocked(item.Id), Is.False);
            Assert.That(game.CampaignTabAvailable(1), Is.False);
            Assert.That(game.IgniteCampaign(), Is.EqualTo(1));
            var resumed = new GameEngine(game.Snapshot());
            Assert.That(resumed.IgniteCampaign(), Is.Zero);
            Assert.That(resumed.TryFinishNarrative(-1, true, _ => false), Is.False);
            Assert.That(resumed.NarrativeIntroSeen, Is.False);
            Assert.That(resumed.TryFinishNarrative(-1, true, _ => true), Is.True);
            Assert.That(resumed.Unlocked("tap") && resumed.Unlocked("coil"), Is.True);
            Assert.That(resumed.ComboAvailable || resumed.OverloadAvailable, Is.False);
        }
        [Test]
        public void StoredEnergyMilestonesAndAchievementRewardsCannotBypassCampaign()
        {
            var game = Checkpoint(0);
            Assert.That(game.StoryCompleted, Is.Zero);
            Assert.That(game.Unlocked("plasma") || game.Unlocked("quantum") || game.Unlocked("auxiliary"), Is.False);
            Assert.That(game.Buy("plasma") || game.ActivateOverload() || game.CanRebirth, Is.False);
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.False);
            Assert.That(game.TryFinishNarrative(0, true, _ => true), Is.False);
            Assert.That(game.TryClaimAchievement("first_spark", _ => true), Is.False, "Achievements enter with communications.");
            Assert.That(game.StoryCompleted, Is.Zero);
        }
        [Test]
        public void FirstMissionRequiresBothRepairsAndPersistedResolution()
        {
            var game = Checkpoint(0, 200);
            Assert.That(game.Buy("coil"), Is.True);
            Assert.That(game.CampaignRequirementsMet, Is.False);
            Assert.That(game.Buy("tap"), Is.True);
            Assert.That(game.CampaignRequirementsMet, Is.True);
            Assert.That(game.TryBeginCampaignMission(_ => false), Is.False);
            Assert.That(game.CampaignMissionReady, Is.False);
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.True);
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.False);
            Assert.That(game.ComboAvailable, Is.False);
            game = new GameEngine(game.Snapshot());
            Assert.That(game.CampaignMissionReady, Is.True);
            Assert.That(game.TryFinishNarrative(0, true, _ => false), Is.False);
            Assert.That(game.StoryCompleted, Is.Zero);
            Assert.That(game.TryFinishNarrative(0, true, _ => true), Is.True);
            Assert.That(game.ComboAvailable, Is.True);
            Assert.That(game.StoryRead, Is.EqualTo(1));
            Assert.That(game.CampaignMissionReady, Is.False);
            Assert.That(game.TryFinishNarrative(0, true, _ => true), Is.False);
        }
        [TestCase(0, false, false, false, false)]
        [TestCase(1, true, false, false, false)]
        [TestCase(2, true, true, false, false)]
        [TestCase(3, true, true, true, false)]
        [TestCase(4, true, true, true, true)]
        public void TechnologiesFollowExperiencedChapters(int chapter, bool combo, bool plasma, bool quantum, bool auxiliary)
        {
            var game = Checkpoint(chapter);
            Assert.That(game.Unlocked("combo"), Is.EqualTo(combo));
            Assert.That(game.OverloadAvailable && game.Unlocked("plasma"), Is.EqualTo(plasma));
            Assert.That(game.Unlocked("quantum"), Is.EqualTo(quantum));
            Assert.That(game.Unlocked("auxiliary"), Is.EqualTo(auxiliary));
        }
        [Test]
        public void SignalCanBeRecoveredWithoutFreneticTaps()
        {
            var game = Checkpoint(1);
            for (int i = 0; i < 5; i++) Assert.That(game.Buy("coil"), Is.True);
            Assert.That(game.Combo.Count, Is.Zero);
            Assert.That(game.CampaignRequirementsMet, Is.True);
        }
        [Test]
        public void RedDoorRequiresInstalledPlasmaAndAnOverloadInTheActiveMission()
        {
            var game = Checkpoint(2);
            Assert.That(game.CampaignRequirementsMet, Is.False);
            game.Buy("plasma");
            Assert.That(game.CampaignRequirementsMet, Is.False);
            for (int i = 0; i < 25; i++) game.Tap();
            Assert.That(game.ActivateOverload(), Is.True);
            Assert.That(game.CampaignRequirementsMet, Is.True);
            game.Advance(100);
            game = new GameEngine(game.Snapshot());
            Assert.That(game.CampaignRequirementsMet, Is.True, "Charge is temporary; completed mission action persists.");
        }
        [TestCase(3, "quantum")]
        [TestCase(4, "auxiliary")]
        public void MemoryMissionsNeedTheirSpecificMachine(int chapter, string id)
        {
            var game = Checkpoint(chapter);
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.False);
            Assert.That(game.Buy(id), Is.True);
            Assert.That(game.CampaignRequirementsMet, Is.True);
        }
        [Test]
        public void ChoiceAndFinalResolutionAreRequiredBeforeAscensionAndSurviveIt()
        {
            var game = Checkpoint(5);
            Assert.That(game.CampaignNeedsChoice, Is.True);
            Assert.That(game.CanRebirth, Is.False);
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out _), Is.False);
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.False);
            Assert.That(game.TryChooseStory(2, _ => true), Is.True);
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.True);
            Assert.That(game.CanRebirth, Is.False);
            Assert.That(game.TryFinishNarrative(5, true, _ => true), Is.True);
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next), Is.True);
            Assert.That(next.CampaignActive && next.NarrativeIntroSeen, Is.True);
            Assert.That(next.StoryCompleted, Is.EqualTo(6));
            Assert.That(next.StoryChoice, Is.EqualTo(2));
            Assert.That(next.CampaignMissionReady, Is.False);
            Assert.That(next.Unlocked("auxiliary"), Is.True);
            Assert.That(next.Level("auxiliary"), Is.Zero);
            Assert.That(next.CanRebirth, Is.False);
            Assert.That(next.TryFinishNarrative(6, true, _ => true), Is.True);
        }
        [Test]
        public void FailedAndReentrantMissionWritesCannotUnlockAnything()
        {
            var game = Checkpoint(0); game.Buy("tap"); game.Buy("coil");
            double balance = game.Energy;
            Assert.That(game.TryBeginCampaignMission(candidate => {
                Assert.That(game.TryBeginCampaignMission(_ => true), Is.False);
                candidate.story.completed = 6;
                return false;
            }), Is.False);
            Assert.That(game.StoryCompleted, Is.Zero);
            Assert.That(game.Energy, Is.EqualTo(balance));
            Assert.That(game.TryBeginCampaignMission(_ => true), Is.True);
            Assert.That(game.TryFinishNarrative(1, true, _ => true), Is.False);
        }
        [Test]
        public void CampaignRejectsInconsistentLedgers()
        {
            var state = GameEngine.NewCampaignState(); state.story.completed = 1;
            Assert.That(state.IsValid(), Is.False);
            state = GameEngine.NewCampaignState(); state.story.missionReady = true;
            Assert.That(state.IsValid(), Is.False);
            state = GameEngine.NewCampaignState(); state.story.campaignRevision = 2;
            Assert.That(state.IsValid(), Is.False);
        }
        [Test]
        public void DedicatedSaveSlotStartsFreshAndRecoversPendingMissionFromBackup()
        {
            string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var old = new SaveStore(Path.Combine(folder, "reactor-save.json"), new CampaignCodec());
                Assert.That(old.Save(new GameState { energy = 1000000, lifetimeEnergy = 1000000 }), Is.True);
                var slot = new SaveStore(Path.Combine(folder, GameEngine.CampaignSaveFile), new CampaignCodec());
                slot.Load(); Assert.That(slot.LoadedExisting, Is.False);
                var game = Checkpoint(0, 200); game.Buy("tap"); game.Buy("coil");
                Assert.That(game.TryBeginCampaignMission(slot.Save), Is.True);
                Assert.That(slot.Save(game.Snapshot()), Is.True);
                File.WriteAllText(Path.Combine(folder, GameEngine.CampaignSaveFile), "interrupted write");
                var resumed = new GameEngine(slot.Load());
                Assert.That(slot.RecoveredBackup, Is.True);
                Assert.That(resumed.CampaignMissionReady, Is.True);
                Assert.That(resumed.StoryCompleted, Is.Zero);
                Assert.That(resumed.TryFinishNarrative(0, true, slot.Save), Is.True);
                Assert.That(new GameEngine(slot.Load()).StoryCompleted, Is.EqualTo(1));
            }
            finally { Directory.Delete(folder, true); }
        }
    }
}
