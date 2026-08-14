using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schala
{
    public class EventDatabase : DbContext
    {
        public DbSet<SchalaEvent> Events { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(@"");
    }

    public class SchalaEvent
    {
        public int EventID { get; set; }
        public required string Owner { get; set; }
        public required string EventType { get; set; }
        public DateTime Time { get; set; }
        public required string Channel { get; set; }
        public required string RolePings { get; set; }
        public required string Keywords { get; set; }
        public required string Description { get; set; }
    }

    public static class SqliteHelper
    {
        public static void CreateDatabase(string file)
        {
            FileStream fs = File.Create(file);
            fs.Close();

            using SqliteConnection db = new SqliteConnection($"Data Source={file}");
            db.Open();

            SqliteCommand createTableCmd = db.CreateCommand();
            createTableCmd.CommandText = @"
                create table if not exists events (
                    EventID integer primary key autoincrement,
                    Owner text not null,
                    EventType text not null,
                    Time text not null,
                    Channel text not null,
                    RolePings text not null,
                    Keywords text not null,
                    Description text not null
                )";
            createTableCmd.ExecuteNonQuery();
        }
    }
}
