namespace ReactorDeCaos
{
    public static class RebirthTransaction
    {
        // A failed write leaves the original engine, currency, modules and timers untouched.
        public static bool TryCommit(GameEngine current, SaveStore store, Specialization branch,
            int quotedReward, out GameEngine committed)
        {
            committed = current;
            if (current == null || store == null || !current.TryPrepareRebirth(branch, quotedReward, out var next)) return false;
            if (!store.Save(next.Snapshot())) return false;
            committed = next;
            return true;
        }
    }
}
