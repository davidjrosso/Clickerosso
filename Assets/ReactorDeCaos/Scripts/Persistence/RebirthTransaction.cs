using System;

namespace ReactorDeCaos
{
    public static class RebirthTransaction
    {
        // A failed write leaves the original engine, currency, modules and timers untouched.
        public static bool TryCommit(GameEngine current, SaveStore store, Specialization branch,
            int quotedReward, out GameEngine committed)
        {
            return TryCommit(current, store == null ? (Func<GameState, bool>)null : store.Save, branch, quotedReward, out committed);
        }
        public static bool TryCommit(GameEngine current, Func<GameState, bool> persist, Specialization branch,
            int quotedReward, out GameEngine committed)
        {
            committed = current;
            if (current == null || persist == null || !current.TryPrepareRebirth(branch, quotedReward, out var next)) return false;
            if (!persist(next.Snapshot())) return false;
            committed = next;
            return true;
        }
    }
}
