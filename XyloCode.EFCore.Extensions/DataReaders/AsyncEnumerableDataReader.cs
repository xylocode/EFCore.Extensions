using System.Data;

namespace XyloCode.EFCore.Extensions.DataReaders
{
    internal class AsyncEnumerableDataReader<TEntity> : SimpleObjectDataReader, IDataReader
        where TEntity : class, new()
    {
        private readonly IAsyncEnumerator<TEntity> _enumerator;
        public AsyncEnumerableDataReader(IAsyncEnumerable<TEntity> entities)
            : base(typeof(TEntity))
        {
            _enumerator = entities.GetAsyncEnumerator();
        }

        public override bool Read()
        {
            ValueTask<bool> vt = _enumerator.MoveNextAsync();
            Task<bool> t = vt.AsTask();
            t.Wait();
            if (t.Result)
            {
                _value = _enumerator.Current;
                return true;
            }
            return false;
        }
    }
}
