# EF Core (Bulk) Extensions

An EF Core extension library for high-performance bulk data synchronization using SQL MERGE, supporting both IEnumerable&lt;T&gt; and IAsyncEnumerable&lt;T&gt; data sources.

The library is optimized for working with DevExpress XAF system fields (GCRecord, OptimisticLockField), as well as custom versioning and change-tracking fields (RowState, RowStamp). It supports state-aware record processing, including handling of deleted objects, and provides utilities for temporarily disabling foreign key constraint checks during synchronization operations.

## Supported Databases

- Microsoft SQL Server
- Microsoft Azure SQL Database

## How to use

```cs
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using XyloCode.EFCore.Extensions;

namespace XyloCode.EFCore.Examples
{
    internal class SimpleFiller
    {
        private readonly SimpleEFCoreDbContext _dbContext;
        private readonly Channel<Country> _countryChannel = Channel.CreateUnbounded<Country>();

        public SimpleFiller(SimpleEFCoreDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public Task CountryMergeAsync(IEnumerable<Country> countries, CancellationToken cancellationToken = default)
        {
            var copyConfig = new EFCoreCopyConfig
            {
                BulkCopyTimeout = 300,
            };
            return _dbContext.MergeAsync(countries, copyConfig, cancellationToken);
        }


        private Task<int> CountryWorkerAsync(CancellationToken cancellationToken = default)
        {
            var entities = _countryChannel.Reader.ReadAllAsync(cancellationToken);
            var copyConfig = new EFCoreCopyConfig
            {
                BulkCopyTimeout = 0,
            };
            return _dbContext.MergeAsync(entities, copyConfig, cancellationToken);
        }


        public async Task AddCountryAsync(Country country, CancellationToken cancellationToken = default)
        {
            await _countryChannel.Writer.WriteAsync(country, cancellationToken);
        }
        
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.NoCheckConstraintAllAsync(typeof(Country), cancellationToken);

            var t = new Task[1];
            int i = 0;
            t[i++] = CountryWorkerAsync(cancellationToken);
            await Task.WhenAll(t);

            await _dbContext.CheckConstraintAllAsync(typeof(Country), cancellationToken);
        }
    }
}
```

## License

MIT License