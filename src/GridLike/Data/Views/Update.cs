namespace GridLike.Data.Views
{
    public enum UpdateType
    {
        Add,
        Update,
        Delete
    }
    
    public record ViewUpdate<T> where T : class 
    {
        public UpdateType Type { get; init; }
        public T View { get; init; } = null!;
    }
    
    
}