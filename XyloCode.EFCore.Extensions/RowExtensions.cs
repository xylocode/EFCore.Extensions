namespace XyloCode.EFCore.Extensions
{
    public static class RowExtensions
    {
        private static readonly DateTime _zero = new(2022, 2, 24);
        public static int GetRowStamp()
        {
            return ((int)((DateTime.UtcNow - _zero).TotalSeconds));
        }
    }
}
