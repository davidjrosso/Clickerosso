using System;
using System.IO;
using NUnit.Framework;

namespace ReactorDeCaos.Tests
{
    public sealed class NarrativeTests
    {
        private static GameEngine FreshAdvanced(double energy)
        {
            var state = new GameEngine().Snapshot();
            state.energy = state.lifetimeEnergy = energy;
            return new GameEngine(state);
        }
        [Test]
        public void NewGameGetsIntroButExistingProgressGetsCatchupWithoutABacklog()
        {
            Assert.That(new GameEngine().NarrativeIntroSeen, Is.False);
            var legacy = new GameEngine(new GameState { energy = 1000000, lifetimeEnergy = 1000000 });
            Assert.That(legacy.NarrativeIntroSeen, Is.True);
            Assert.That(legacy.NarrativeScenesSeen, Is.EqualTo(6));
            Assert.That(legacy.StoryRead, Is.Zero, "Migration does not pretend the player read the archive.");
            Assert.That(legacy.Energy, Is.EqualTo(1000000));
        }
        [Test]
        public void LegacyArrivalBriefingIsSavedOnceWithoutResettingProgress()
        {
            var game = new GameEngine(new GameState { energy = 1000, lifetimeEnergy = 1000 });
            Assert.That(game.NarrativeBriefingSeen, Is.False);
            Assert.That(game.TryFinishNarrative(-2, false, _ => false), Is.False);
            Assert.That(game.TryFinishNarrative(-2, false, _ => true), Is.True);
            Assert.That(game.TryFinishNarrative(-2, true, _ => true), Is.False);
            Assert.That(new GameEngine(game.Snapshot()).NarrativeBriefingSeen, Is.True);
            Assert.That(game.Energy, Is.EqualTo(1000));
        }
        [Test]
        public void InterruptedNewIntroRemainsPendingAfterSavingAndLoading()
        {
            var game = new GameEngine(); game.Tap();
            var loaded = new GameEngine(game.Snapshot());
            Assert.That(loaded.NarrativeIntroSeen, Is.False);
            Assert.That(loaded.Energy, Is.EqualTo(1));
            Assert.That(loaded.TryFinishNarrative(-1, false, _ => true), Is.True);
            Assert.That(new GameEngine(loaded.Snapshot()).NarrativeIntroSeen, Is.True);
        }
        [Test]
        public void FailedPersistenceDoesNotConsumeAnIntroOrScene()
        {
            var game = FreshAdvanced(200);
            Assert.That(game.TryFinishNarrative(-1, true, _ => false), Is.False);
            Assert.That(game.NarrativeIntroSeen, Is.False);
            Assert.That(game.TryFinishNarrative(-1, true, _ => true), Is.True);
            Assert.That(game.TryFinishNarrative(0, true, _ => false), Is.False);
            Assert.That(game.NarrativeScenesSeen, Is.Zero);
            Assert.That(game.StoryRead, Is.Zero);
        }
        [Test]
        public void SceneCompletionIsSequentialAndIdempotentAndSkippingDoesNotMarkRead()
        {
            var game = FreshAdvanced(1000000); int writes = 0;
            Func<GameState, bool> save = _ => { writes++; return true; };
            Assert.That(game.TryFinishNarrative(0, true, save), Is.False);
            Assert.That(game.TryFinishNarrative(-1, false, save), Is.True);
            Assert.That(game.TryFinishNarrative(1, true, save), Is.False);
            Assert.That(game.TryFinishNarrative(0, false, save), Is.True);
            Assert.That(game.TryFinishNarrative(0, true, save), Is.False);
            Assert.That(game.NarrativeScenesSeen, Is.EqualTo(1));
            Assert.That(game.StoryRead, Is.Zero);
            Assert.That(writes, Is.EqualTo(2));
        }
        [Test]
        public void CompletingScenesDoesNotAwardCurrencyOrResetActiveGameplay()
        {
            var game = FreshAdvanced(1000000);
            for (int i = 0; i < 25; i++) game.Tap();
            game.ActivateOverload(); game.Advance(.5);
            var before = game.Snapshot(); int combo = game.Combo.Count; double remaining = game.Overload.Remaining;
            game.TryFinishNarrative(-1, true, _ => true);
            for (int i = 0; i < 6; i++) Assert.That(game.TryFinishNarrative(i, true, candidate => candidate.IsValid()), Is.True);
            Assert.That(game.StoryRead, Is.EqualTo(6));
            Assert.That(game.Energy, Is.EqualTo(before.energy));
            Assert.That(game.Lifetime, Is.EqualTo(before.lifetimeEnergy));
            Assert.That(game.Combo.Count, Is.EqualTo(combo));
            Assert.That(game.Overload.Remaining, Is.EqualTo(remaining));
        }
        [Test]
        public void CinematicLedgerIsDetachedAndCannotReenterACommit()
        {
            var game = new GameEngine();
            Assert.That(game.TryFinishNarrative(-1, true, candidate =>
            {
                Assert.That(game.NarrativeIntroSeen, Is.False);
                Assert.That(candidate.story.narrative.introSeen, Is.True);
                Assert.That(game.TryFinishNarrative(-1, true, _ => true), Is.False);
                candidate.story.narrative.introSeen = false;
                return true;
            }), Is.True);
            var snapshot = game.Snapshot(); snapshot.story.narrative.introSeen = false;
            Assert.That(game.NarrativeIntroSeen, Is.True);
        }
        [Test]
        public void RebirthPreservesSeenScenesAndUnlocksPresentationWithoutAnotherReset()
        {
            var game = FreshAdvanced(1000000);
            game.TryFinishNarrative(-1, true, _ => true);
            game.TryFinishNarrative(0, true, _ => true);
            Assert.That(game.TryFinishNarrative(6, true, _ => true), Is.False);
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next), Is.True);
            Assert.That(next.NarrativeScenesSeen, Is.EqualTo(1));
            int essence = next.Essence;
            Assert.That(next.TryFinishNarrative(6, true, _ => true), Is.True);
            Assert.That(next.TryFinishNarrative(6, true, _ => true), Is.False);
            Assert.That(next.Essence, Is.EqualTo(essence));
            Assert.That(next.RebirthCount, Is.EqualTo(1));
            Assert.That(new GameEngine(next.Snapshot()).NarrativeAscentSeen, Is.True);
        }
        [Test]
        public void CommunicationFlagsAreBoundedAndPersistBeforeBeingConsumed()
        {
            var game = new GameEngine();
            Assert.That(game.TryMarkCommunication(0, _ => false), Is.False);
            Assert.That(game.NarrativeCommunicationSeen(0), Is.False);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(game.TryMarkCommunication(i, _ => true), Is.True);
                Assert.That(game.TryMarkCommunication(i, _ => true), Is.False);
            }
            Assert.That(game.TryMarkCommunication(3, _ => true), Is.False);
            Assert.That(game.TryMarkCommunication(-1, _ => true), Is.False);
            Assert.That(new GameEngine(game.Snapshot()).Snapshot().story.narrative.communications, Is.EqualTo(7));
        }
        [Test]
        public void InvalidNarrativeProgressCannotReplaceASave()
        {
            foreach (var invalid in new[] { new NarrativeProgress { scenesSeen = -1 },
                new NarrativeProgress { introSeen = true, scenesSeen = 7 },
                new NarrativeProgress { scenesSeen = 1 }, new NarrativeProgress { communications = 8 },
                new NarrativeProgress { introSeen = true, ascentSeen = true } })
            {
                var state = FreshAdvanced(1000000).Snapshot(); state.story.narrative = invalid;
                Assert.That(state.IsValid(), Is.False);
                Assert.Throws<ArgumentException>(() => new GameEngine(state));
            }
        }
        [Test]
        public void ReplayEligibilityHidesFutureSpoilersAndNeverChangesState()
        {
            var game = FreshAdvanced(200);
            Assert.That(game.CanReplayNarrative(-1), Is.True);
            Assert.That(game.CanReplayNarrative(0), Is.True);
            Assert.That(game.CanReplayNarrative(1), Is.False);
            Assert.That(game.CanReplayNarrative(6), Is.False);
            Assert.That(game.CanReplayNarrative(99), Is.False);
            Assert.That(game.Energy, Is.EqualTo(200));
        }
        [Test]
        public void ShippedCatalogHasAllScenesUniqueVoiceIdsAndOneInteractiveFirstTap()
        {
#if UNITY_5_3_OR_NEWER
            var catalog = new UnityJsonCodec().Deserialize<NarrativeCatalog>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Narrative").text);
#else
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Assets/ReactorDeCaos/Resources/Narrative.json"))) directory = directory.Parent;
            Assert.That(directory, Is.Not.Null);
            var catalog = new NetJsonCodec().Deserialize<NarrativeCatalog>(File.ReadAllText(Path.Combine(directory.FullName, "Assets/ReactorDeCaos/Resources/Narrative.json")));
#endif
            Assert.DoesNotThrow(catalog.Validate);
            int interactive = 0, lines = 0;
            foreach (var scene in catalog.scenes)
                foreach (var beat in scene.beats) { lines++; if (beat.interactive) interactive++; }
            Assert.That(interactive, Is.EqualTo(1));
            Assert.That(lines, Is.EqualTo(46));
            catalog.scenes[0].beats[0].voice = "../bad";
            Assert.Throws<ArgumentException>(catalog.Validate);
        }
    }
}
