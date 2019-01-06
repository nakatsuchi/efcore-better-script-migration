using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCoreBetterScriptMigration
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new BloggingContext();
            var assembly = context.Database.GetService<IMigrationsAssembly>();
            var history = context.Database.GetService<IHistoryRepository>();
            var generator = context.Database.GetService<IMigrationsSqlGenerator>();
            var migrator = context.Database.GetService<IMigrator>();

            Console.WriteLine(history.GetCreateIfNotExistsScript());
            Console.WriteLine("GO");

            var migrationIds = context.Database.GetMigrations()
                .OrderBy(id => id);
            foreach (var migrationId in migrationIds)
            {
                var migrationType = assembly.Migrations[migrationId];
                var migration = assembly.CreateMigration(migrationType, context.Database.ProviderName);
                var commands = generator.Generate(migration.UpOperations, migration.TargetModel);

                foreach (var command in commands)
                {
                    Console.WriteLine(history.GetBeginIfNotExistsScript(migrationId));
                    Console.WriteLine($"EXEC ('{command.CommandText.Replace("'", "''")}');");
                    Console.WriteLine(history.GetEndIfScript());
                    Console.WriteLine("GO");
                }

                Console.WriteLine(history.GetBeginIfNotExistsScript(migrationId));
                Console.WriteLine(history.GetInsertScript(
                    new HistoryRow(migrationId, Microsoft.EntityFrameworkCore.Internal.ProductInfo.GetVersion())));
                Console.WriteLine(history.GetEndIfScript());
                Console.WriteLine("GO");
            }
        }
    }
}
