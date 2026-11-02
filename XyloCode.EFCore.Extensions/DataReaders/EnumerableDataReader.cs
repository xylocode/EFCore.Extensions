using System.Data;

namespace XyloCode.EFCore.Extensions.DataReaders
{
    internal class EnumerableDataReader<TEntity> : SimpleObjectDataReader, IDataReader
        where TEntity : class, new()
    {
        private readonly IEnumerator<TEntity> _enumerator;

        public EnumerableDataReader(IEnumerable<TEntity> entities)
            : base(typeof(TEntity))
        {
            _enumerator = entities.GetEnumerator();
        }

        public override bool Read()
        {
            if (_enumerator.MoveNext())
            {
                _value = _enumerator.Current;
                return true;
            }
            return false;
        }
    }
}
