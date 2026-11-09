using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;
using System.Text;
using XyloCode.EFCore.Extensions.DataReaders;

namespace XyloCode.EFCore.Extensions
{
    internal class EFCoreCopyMaster<TDbContext, TEntity>:
        EFCoreCopyMaster<TDbContext>
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        public EFCoreCopyMaster(TDbContext dbContext, EFCoreCopyConfig copyConfig):
            base(dbContext, typeof(TEntity), copyConfig)
        { }

        public Task WriteToServerAsync(
            IEnumerable<TEntity> entities
            , CancellationToken cancellationToken = default)
        {
            var reader = new EnumerableDataReader<TEntity>(entities);
            return WriteToServerAsync(reader, cancellationToken);
        }

        public Task WriteToServerAsync(
            IAsyncEnumerable<TEntity> entities
            , CancellationToken cancellationToken = default)
        {
            var reader = new AsyncEnumerableDataReader<TEntity>(entities);
            return WriteToServerAsync(reader, cancellationToken);
        }
    }


    internal class EFCoreCopyMaster<TDbContext>
        where TDbContext : DbContext
    {
        private readonly EFCoreCopyConfig _config;
        private readonly SqlConnection _sqlConnection;

        private readonly IEntityType _entityType;
        protected readonly SimpleObjectProperty[] _columns;
        private readonly string _targetSchemaName;
        private readonly string _targetTableName;
        private readonly string _tempTableName;

        private readonly bool _useRowStamp = false;
        private readonly string _rowStampColumnName = null!;
        private readonly bool _useRowState = false;
        private readonly string _rowStateColumnName = null!;
        private readonly bool _useDeferredDeletion = false;
        private readonly string _deferredDeletionColumnName = null!;




        public EFCoreCopyMaster(TDbContext dbContext, Type entityType, EFCoreCopyConfig copyConfig)
        {
            _config = copyConfig ?? new();
            
            string? cs = dbContext.Database.GetConnectionString();
            _sqlConnection = new SqlConnection(cs);
            _sqlConnection.Open();

            _entityType = dbContext.Model.FindEntityType(entityType) ??
                throw new Exception("FindEntityType() return null");

            _targetSchemaName = _entityType.GetSchema()!;
            _targetTableName = _entityType.GetTableName()!;

            string sysName = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            _tempTableName =
                _config.UseTempTableEngine ?
                $"#{_targetTableName}_TEMP{sysName}" :
                $"[{_targetSchemaName}].[{_targetTableName}_TEMP{sysName}]";

            #region Set Columns
            var properties = _entityType.GetProperties();
            var columnList = new List<SimpleObjectProperty>();
            var ids = StoreObjectIdentifier.Table(_targetTableName, _targetSchemaName);

            foreach (var p in properties)
            {
                if (p.IsCollection)
                    continue;

                var col = new SimpleObjectProperty
                {
                    Name = p.Name,
                    SqlName = p.GetColumnName(ids),
                    SqlType = p.GetColumnType(),
                    IsKey = p.IsKey(),
                };


                if(string.IsNullOrEmpty(col.SqlName))
                {
                    col.IsIgnored = true;
                    continue;
                }


                if (_config.IgnoredPropertyNames.Contains(col.Name))
                {
                    col.IsIgnored = true;
                }

                if(col.Name == _config.RowStampPropertyName && col.SqlType == "int")
                {
                    col.IsIgnored = true;
                    col.IsRowStamp = true;
                    _useRowStamp = _config.UseRowStamp;
                    _rowStampColumnName = col.SqlName;
                }

                if (col.Name == _config.RowStatePropertyName && col.SqlType == "int")
                {
                    col.IsIgnored = true;
                    col.IsRowState = true;
                    _useRowState = _config.UseRowState;
                    _rowStateColumnName = col.SqlName;
                }

                if (col.Name == _config.DeferredDeletionPropertyName && col.SqlType == "int")
                {
                    col.IsIgnored = true;
                    _useDeferredDeletion = true;
                    _deferredDeletionColumnName = col.SqlName;
                }

                if (col.SqlType == "rowversion")
                {
                    col.IsRowVersion = true;
                    col.IsIgnored = true;
                }

                columnList.Add(col);
            }
            _columns = [.. columnList];
            #endregion
        }

        public async Task WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken = default)
        {
            var sqlTransaction = _sqlConnection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            var sqlBulkCopy = new SqlBulkCopy(_sqlConnection, _config.SqlBulkCopyOptions, sqlTransaction)
            {
                BatchSize = _config.BatchSize,
                BulkCopyTimeout = _config.BulkCopyTimeout,
                EnableStreaming = _config.EnableStreaming,
                NotifyAfter = _config.NotifyAfter,
                DestinationTableName = _tempTableName,
            };

            if (_config.SqlRowsCopied is not null)
                sqlBulkCopy.SqlRowsCopied += _config.SqlRowsCopied;

            foreach (var col in _columns)
            {
                if (col.IsIgnored) continue;
                sqlBulkCopy.ColumnMappings.Add(col.SqlName, col.SqlName);
            }

            await sqlBulkCopy.WriteToServerAsync(dataReader, cancellationToken);
            await sqlTransaction.CommitAsync(cancellationToken);
        }

        public Task<int> CreateTempTableAsync(CancellationToken cancellationToken = default)
        {
            var q = new StringBuilder();
            q.AppendLine($"CREATE TABLE {_tempTableName} (");

            int i = 0;
            foreach (var p in _columns)
            {
                if (p.IsIgnored && !p.IsRowStamp) continue;
                if (i++ > 0) q.Append(", ");
                q.AppendLine($"[{p.SqlName}] {p.SqlType} NULL");
            }


            q.AppendLine(");");

            var sqlCmd = _sqlConnection.CreateCommand();
            sqlCmd.CommandText = q.ToString();
            return sqlCmd.ExecuteNonQueryAsync(cancellationToken);
        }


        public Task<int> DropTempTableAsync(CancellationToken cancellationToken = default)
        {
            var sqlCmd = _sqlConnection.CreateCommand();
            sqlCmd.CommandText = $"DROP TABLE {_tempTableName}";
            return sqlCmd.ExecuteNonQueryAsync(cancellationToken);
        }

  

        private Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken = default)
        {
            var sqlCmd = _sqlConnection.CreateCommand();
            sqlCmd.CommandText = sql;
            return sqlCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> MergeAsync(CancellationToken cancellationToken = default)
        {
            var q = new StringBuilder();
            q.AppendLine($"MERGE INTO [{_targetSchemaName}].[{_targetTableName}]");
            q.AppendLine("WITH (HOLDLOCK)");
            q.AppendLine("AS T");
            q.AppendLine("USING (");
            q.AppendLine($"SELECT * FROM {_tempTableName}");
            q.AppendLine(") S ON ");
            q.AppendLine(GetSearchCondition());
            q.AppendLine(GetMainWhenMatchedUpdateClause());
            q.AppendLine(GetWhenNotMatchedByTargetClause());
            q.AppendLine(GetWhenNotMatchedBySourceClause());
            q.AppendLine(GetMergeOptions());
            q.Append(';');


            var dbTransaction = await _sqlConnection.BeginTransactionAsync(cancellationToken);
            var sqlCmd = _sqlConnection.CreateCommand();
            sqlCmd.Transaction = (SqlTransaction)dbTransaction;
            sqlCmd.CommandText = q.ToString();
            sqlCmd.CommandTimeout = _config.CommandTimeout;
            int cnt = await sqlCmd.ExecuteNonQueryAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return cnt;

            string GetSearchCondition()
            {
                var q = new StringBuilder();
                if (_config.UpdateByProperties.Count == 0)
                {
                    int i = 0;
                    foreach (var item in _columns)
                    {
                        if (item.IsKey)
                        {
                            if (i++ > 0) q.Append(" AND ");
                            q.AppendLine($"T.[{item.SqlName}] = S.[{item.SqlName}]");
                        }
                    }
                }
                else
                {
                    int i = 0;
                    foreach (var item in _columns)
                    {
                        if (_config.UpdateByProperties.Contains(item.Name))
                        {
                            if (i++ > 0) q.Append(" AND ");
                            q.AppendLine($"T.[{item.SqlName}] = S.[{item.SqlName}]");
                        }
                    }
                }
                return q.ToString();
            }
        
            string GetWhenNotMatchedBySourceClause()
            {
                var q = new StringBuilder();
                switch (_config.DeletionMode)
                {
                    case DeletionMode.None:
                        break;

                    case DeletionMode.FullDeletion:
                        q.AppendLine("WHEN NOT MATCHED BY SOURCE");
                        q.AppendLine("THEN DELETE");
                        break;

                    case DeletionMode.DeferredDeletion:

                        if (!(_useRowState || _useDeferredDeletion))
                            throw new NotSupportedException("Deferred deletion is not supported when RowState or DeferredDeletion columns are used.");

                        q.AppendLine("WHEN NOT MATCHED BY SOURCE");
                        q.AppendLine("THEN UPDATE SET");

                        int i = 0;
                        if (_useRowStamp)
                        {
                            if (i++ > 0) q.Append(", ");
                            q.AppendLine($"[{_rowStampColumnName}] = {_config.ZeroRowStampSql}");
                        }

                        if (_useRowState)
                        {
                            if (i++ > 0) q.Append(", ");
                            q.AppendLine($"[{_rowStateColumnName}] = {(int)RowState.DELETED}");
                        }

                        if (_useDeferredDeletion)
                        {
                            if (i++ > 0) q.Append(", ");
                            q.AppendLine($"[{_deferredDeletionColumnName}] = DATEDIFF(second, '1970-01-01', GETUTCDATE())");
                        }
                        break;

                    case DeletionMode.DeferredDeletionIfPossibleElseNone:
                        if (_useRowState || _useDeferredDeletion)
                            goto case DeletionMode.DeferredDeletion;
                        break;

                    case DeletionMode.DeferredDeletionIfPossibleElseFullDeletion:
                        if (_useRowState || _useDeferredDeletion)
                            goto case DeletionMode.DeferredDeletion;
                        else 
                            goto case DeletionMode.FullDeletion;

                    default:
                        throw new NotSupportedException();
                }
                return q.ToString();
            }


            string GetWhenNotMatchedByTargetClause()
            {
                var q = new StringBuilder();
                q.AppendLine("WHEN NOT MATCHED BY TARGET");
                q.AppendLine("THEN INSERT (");
                int i = 0;
                foreach (var item in _columns)
                {
                    if (item.IsIgnored) continue;
                    if (i++ > 0) q.Append(", ");
                    q.AppendLine($"[{item.SqlName}]");
                }

                if(_useRowStamp)
                    q.AppendLine($", [{_rowStampColumnName}]");

                if (_useRowState)
                    q.AppendLine($", [{_rowStateColumnName}]");

                q.AppendLine(") VALUES (");
                i = 0;
                foreach (var item in _columns)
                {
                    if (item.IsIgnored) continue;
                    if (i++ > 0) q.Append(", ");
                    q.AppendLine($"S.[{item.SqlName}]");
                }

                if (_useRowStamp)
                    q.AppendLine($", COALESCE(S.[{_rowStampColumnName}], {_config.ZeroRowStampSql})");

                if(_useRowState)
                    q.AppendLine($", {(int)RowState.INSERTED}");

                q.AppendLine(")");
                return q.ToString();
            }

            string GetMainWhenMatchedUpdateClause()
            {
                var columnAssignments = GetColumnAssignmentForMainWhenMatchedUpdateClause();
                if (string.IsNullOrWhiteSpace(columnAssignments))
                    return string.Empty;

                var q = new StringBuilder();
                q.AppendLine("WHEN MATCHED");
                if (_config.AdditionalWhenMatchedUpdateConditions is not null)
                {
                    foreach (var item in _config.AdditionalWhenMatchedUpdateConditions)
                        q.AppendLine("AND " + item);
                }

                if (_useRowState)
                    q.AppendLine($"AND T.[{_rowStateColumnName}] = {(int)RowState.UPDATABLE}");

                if(_useRowStamp)
                    q.AppendLine($"AND T.[{_rowStampColumnName}] < S.[{_rowStampColumnName}]");

                q.AppendLine("THEN UPDATE SET");
                q.AppendLine(columnAssignments);
                return q.ToString();
            }

            string GetColumnAssignmentForMainWhenMatchedUpdateClause()
            {
                var q = new StringBuilder();
                int i = 0;
                if (_config.UpdateByProperties.Count == 0)
                {
                    foreach (var item in _columns)
                    {
                        if (item.IsIgnored || item.IsKey) continue;
                        if (i++ > 0) q.Append(", ");
                        q.AppendLine($"T.[{item.SqlName}] = S.[{item.SqlName}]");
                    }
                }
                else
                {
                    foreach (var item in _columns)
                    {
                        if (item.IsIgnored || item.IsKey || _config.UpdateByProperties.Contains(item.Name)) continue;
                        if (i++ > 0) q.Append(',');
                        q.AppendLine($"T.[{item.SqlName}] = S.[{item.SqlName}]");
                    }
                }

                if (i == 0) return string.Empty;

                if (_useRowStamp)
                    q.AppendLine($", T.[{_rowStampColumnName}] = COALESCE(S.[{_rowStampColumnName}], {_config.ZeroRowStampSql})");

                if (_useRowState)
                    q.AppendLine($", T.[{_rowStateColumnName}] = {(int)RowState.UPDATED}");

                return q.ToString();
            }

            string GetMergeOptions()
            {
                if(_config.MergeOptions is null || _config.MergeOptions.Count == 0)
                    return string.Empty;

                return "OPTION (" + string.Join(',', _config.MergeOptions) + ')';
            }
        }
    }
}
