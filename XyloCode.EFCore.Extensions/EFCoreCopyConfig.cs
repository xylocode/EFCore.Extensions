using Microsoft.Data.SqlClient;

namespace XyloCode.EFCore.Extensions
{
    public class EFCoreCopyConfig
    {
        public SqlBulkCopyOptions SqlBulkCopyOptions { get; set; } = SqlBulkCopyOptions.Default;
        public int CommandTimeout { get; set; } = 600;
        public int BatchSize { get; set; } = 2_000;
        public int BulkCopyTimeout { get; set; } = 600;
        public bool EnableStreaming { get; set; } = true;
        public int NotifyAfter { get; set; } = 0;
        public SqlRowsCopiedEventHandler? SqlRowsCopied { get; set; }
        public bool UseTempTableEngine { get; set; } = true;
        public HashSet<string> UpdateByProperties { get; set; } = [];
        public HashSet<string> IgnoredPropertyNames { get; set; } = ["RowSeqNo", "OptimisticLockField"];
        public HashSet<string> AdditionalWhenMatchedUpdateConditions { get; set; } = [];

        public DeletionMode DeletionMode { get; set; } = DeletionMode.DeferredDeletionIfPossibleElseNone;
        public string RowStampPropertyName { get; set; } = "RowStamp";
        public string RowStatePropertyName { get; set; } = "RowState";
        public string DeferredDeletionPropertyName { get; set; } = "GCRecord";

        public bool UseRowStamp { get; set; } = true;
        public bool UseRowState { get; set; } = true;
        public HashSet<string> MergeOptions { get; set; } = ["MAXDOP 4"];
        public string ZeroRowStampSql { get; set; } = "DATEDIFF(second, '2022-02-24', GETUTCDATE())";
    }
}