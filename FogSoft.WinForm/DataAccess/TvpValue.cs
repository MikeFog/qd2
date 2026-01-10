using System.Data;

namespace FogSoft.WinForm.DataAccess
{
    public sealed class TvpValue
    {
        public DataTable Table { get; }
        public string TypeName { get; } 
        public TvpValue(DataTable table, string typeName)
        {
            Table = table;
            TypeName = typeName;
        }
    }
}