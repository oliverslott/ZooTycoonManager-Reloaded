using Microsoft.Data.Sqlite;

namespace ZooTycoonManager.Interfaces
{
    public interface ISaveable
    {
        void Save(SqliteTransaction transaction);
    }
}
