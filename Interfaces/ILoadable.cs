using Microsoft.Data.Sqlite;

namespace ZooTycoonManager.Interfaces
{
    public interface ILoadable
    {
        void Load(SqliteDataReader reader);
    }
}