﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sumo.Data.Commands;
using Sumo.Data.Factories;
using Sumo.Data.Factories.Sqlite;
using Sumo.Data.Orm.Factories;
using Sumo.Data.Schema.Factories.Sqlite;

namespace Test.Sumo.Data.Sqlite.Commands
{
    public class Test
    {
        public int TestId { get; set; }
        public string Name { get; set; }
    }

    public class DoesNotExist
    {

    }

    [TestClass]
    public class Command_Tests
    {
        private readonly string _connectionString = "Filename=./sqlite.db; Mode=ReadWriteCreate";

        [TestMethod]
        public void Constructor()
        {
            IParameterFactory parameterFactory = new SqliteParameterFactory();
            IConnectionFactory connectionFactory = new SqliteConnectionFactory();

            using (var connection = connectionFactory.Open(_connectionString))
            using (var command = new Command(connection, parameterFactory))
            {
                Assert.IsNotNull(command);
            }
        }

        [TestMethod]
        public void ExecuteScalor()
        {
            IParameterFactory parameterFactory = new SqliteParameterFactory();
            IConnectionFactory connectionFactory = new SqliteConnectionFactory();
            ISqlStatementBuilder sqlBuilder = new SqliteStatementBuilder();

            using (var connection = connectionFactory.Open(_connectionString))
            using (var command = new Command(connection, parameterFactory))
            {
                var sql = sqlBuilder.GetExistsStatement<Test>();
                var exists = command.ExecuteScalar<bool>(sql);
                Assert.IsTrue(exists);

                sql = sqlBuilder.GetExistsStatement<DoesNotExist>();
                exists = command.ExecuteScalar<bool>(sql);
                Assert.IsFalse(exists);
            }
        }
    }
}
