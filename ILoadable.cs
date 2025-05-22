using Microsoft.Data.Sqlite;

namespace ZooTycoonManager
{
    public interface ILoadable
    {
        void Load(SqliteDataReader reader);
    }
} 