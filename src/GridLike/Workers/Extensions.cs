namespace GridLike.Workers
{
    public static class Extensions
    {
        public static bool IsSafe(this WorkerState state)
        {
            return state == WorkerState.Busy || state == WorkerState.Ready || state == WorkerState.Registered;
        }
        
    }
}