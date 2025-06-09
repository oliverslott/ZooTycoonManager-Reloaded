﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager.Interfaces
{
    public interface ISaveable
    {
        void Save(SqliteTransaction transaction);
    }
}
