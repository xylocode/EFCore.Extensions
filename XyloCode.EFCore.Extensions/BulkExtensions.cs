using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace XyloCode.EFCore.Extensions
{
    public static class BulkExtensions
    {

        public async static Task<int> MergeAsync<TDbContext, TEntity>(
            this TDbContext dbContext
            , IEnumerable<TEntity> entities
            , EFCoreCopyConfig copyConfig = null!
            , CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            where TEntity : class, new()
        {
            switch (dbContext.Database.ProviderName)
            {
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    break;
                default:
                    throw new NotSupportedException();
            }

            var master = new EFCoreCopyMaster<TDbContext, TEntity>(dbContext, copyConfig);
            await master.CreateTempTableAsync(cancellationToken);
            await master.WriteToServerAsync(entities, cancellationToken);
            var count = await master.MergeAsync(cancellationToken);
            await master.DropTempTableAsync(cancellationToken);
            return count;
        }

        public async static Task<int> MergeAsync<TDbContext, TEntity>(
            this TDbContext dbContext
            , IAsyncEnumerable<TEntity> entities
            , EFCoreCopyConfig copyConfig = null!
            , CancellationToken cancellationToken = default)
            where TDbContext: DbContext
            where TEntity: class, new()
        {
            switch (dbContext.Database.ProviderName)
            {
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    break;
                default:
                    throw new NotSupportedException();
            }

             var master = new EFCoreCopyMaster<TDbContext, TEntity>(dbContext, copyConfig);
            await master.CreateTempTableAsync(cancellationToken);
            await master.WriteToServerAsync(entities, cancellationToken);
            var count = await master.MergeAsync(cancellationToken);
            await master.DropTempTableAsync(cancellationToken);
            return count;
        }


        public static Task<int> NoCheckConstraintAllAsync<TDbContext, TEntity>(
            this TDbContext dbContext
            , CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            where TEntity : class, new()
        {
            return NoCheckConstraintAllAsync(dbContext, typeof(TEntity), cancellationToken);
        }

        public static Task<int> NoCheckConstraintAllAsync<TDbContext>(
            this TDbContext dbContext
            , Type entityType
            , CancellationToken cancellationToken = default)
            where TDbContext : DbContext
        {
            IEntityType iet = dbContext.Model.FindEntityType(entityType)!;
            string targetSchemaName = iet.GetSchema()!;
            string targetTableName = iet.GetTableName()!;
            string sqlCmdText = $"ALTER TABLE [{targetSchemaName}].[{targetTableName}] NOCHECK CONSTRAINT ALL";
            return dbContext.Database.ExecuteSqlRawAsync(sqlCmdText, cancellationToken);
        }

        public static Task<int> CheckConstraintAllAsync<TDbContext, TEntity>(
            this TDbContext dbContext
            , CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            where TEntity : class, new()
        {
            return CheckConstraintAllAsync(dbContext, typeof(TEntity), cancellationToken);
        }

        public static Task<int> CheckConstraintAllAsync<TDbContext>(
            this TDbContext dbContext
            , Type entityType
            , CancellationToken cancellationToken = default)
            where TDbContext : DbContext
        {
            IEntityType iet = dbContext.Model.FindEntityType(entityType)!;
            string targetSchemaName = iet.GetSchema()!;
            string targetTableName = iet.GetTableName()!;
            string sqlCmdText = $"ALTER TABLE [{targetSchemaName}].[{targetTableName}] WITH CHECK CHECK CONSTRAINT ALL";
            return dbContext.Database.ExecuteSqlRawAsync(sqlCmdText, cancellationToken);
        }
    }
}
