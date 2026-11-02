namespace XyloCode.EFCore.Extensions
{
    internal class SimpleObjectProperty
    {
        // Имя свойства в C#
        public string Name { get; set; } = null!;
        public string? SqlName { get; set; }
        public string? SqlType { get; set; }
        public bool IsKey { get; set; } = false;
        public bool IsRowVersion { get; set; } = false;
        public bool IsIgnored { get; set; } = false;

        public bool IsRowStamp { get; set; } = false;
        public bool IsRowState { get; set; } = false;


        public bool UseForMergeSearchCondition { get; set; }
        public bool UseForClauseSearchCondition { get; set; }
        public bool UseWhenMatched { get; set; }
        public bool UseWhenNotMatchedByTarget { get; set; }
        public bool UseWhenNotMatchedBySource { get; set; }

    }
}
