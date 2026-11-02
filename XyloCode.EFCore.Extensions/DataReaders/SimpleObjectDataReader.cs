using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace XyloCode.EFCore.Extensions.DataReaders
{
    internal abstract class SimpleObjectDataReader : IDataReader
    {
        protected readonly PropertyInfo[] _properties;
        protected readonly Dictionary<string, int> _map = [];
        protected object _value = null!;

        protected SimpleObjectDataReader(Type entityType)
        {
            _properties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < _properties.Length; i++)
                _map.Add(_properties[i].Name, i);
        }

        public object this[int i] => GetValue(i);
        
        public object this[string name]
        {
            get
            {
                var index = GetOrdinal(name);
                if (index >= 0)
                    return GetValue(index);
                return null!;
            }
        }

        public int Depth => 0;

        public bool IsClosed => throw new NotImplementedException();

        public int RecordsAffected => 0;

        public int FieldCount => _properties.Length;

        public virtual void Close() { }

        public virtual void Dispose() { }

        public bool GetBoolean(int i) => throw new NotImplementedException();

        public byte GetByte(int i)=>throw new NotImplementedException();

        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)=>throw new NotImplementedException();

        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i) => throw new NotImplementedException();

        public string GetDataTypeName(int i) => _properties[i].PropertyType.Name;
        
        public DateTime GetDateTime(int i) => throw new NotImplementedException();

        public decimal GetDecimal(int i) =>throw new NotImplementedException();

        public double GetDouble(int i) => throw new NotImplementedException();


        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int i) => _properties[i].PropertyType;


        public float GetFloat(int i)=>throw new NotImplementedException();

        public Guid GetGuid(int i)=>throw new NotImplementedException();

        public short GetInt16(int i) => throw new NotImplementedException();

        public int GetInt32(int i) =>throw new NotImplementedException();

        public long GetInt64(int i) =>throw new NotImplementedException();

        public string GetName(int i) => _properties[i].Name;

        public int GetOrdinal(string name)
        {
            if (_map.TryGetValue(name, out int index))
                return index;
            return -1;
        }

        public DataTable? GetSchemaTable() => throw new NotSupportedException();

        public string GetString(int i) => throw new NotImplementedException();

        public object GetValue(int i) => _properties[i].GetValue(_value)!;

        public int GetValues(object[] values) => throw new NotImplementedException();

        public bool IsDBNull(int i) => _properties[i].GetValue(_value) == null;
        
        public bool NextResult() => false;

        public abstract bool Read();
    }
}
