﻿using Sumo.Data.Commands;
using Sumo.Data.Orm.Factories;
using Sumo.Data.Orm.Types;
using Sumo.Data.Queries;
using Sumo.Data.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sumo.Data.Orm.Repositories
{
    public class Repository : IRepository
    {
        private readonly IFactorySet _factorySet;
        private readonly string _connectionString;

        public Repository(IFactorySet factorySet, string connectionString)
        {
            _factorySet = factorySet ?? throw new ArgumentNullException(nameof(factorySet));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            _connectionString = connectionString;
        }

        public T Read<T>(object searchKey) where T : class
        {
            if (searchKey == null) throw new ArgumentNullException(nameof(searchKey));
            if (EntityInfoCache<T>.PrimaryKeyProperties.Length > 1) throw new NotSupportedException($"{TypeInfoCache<T>.FullName} has a multi-part primary key. Use method 'T[] Read<T>(Dictionary<string, object> parameters) where T : class' instead.");
            var parameters = new Dictionary<string, object>();
            var primaryKey = EntityInfoCache<T>.PrimaryKeyProperties[0];
            parameters.Add(primaryKey.Name, searchKey);
            var results = Read<T>(parameters);
            return results != null && results.Length > 0 ? results[0] : null;
        }

        public async Task<T> ReadAsync<T>(object searchKey) where T : class
        {
            if (searchKey == null) throw new ArgumentNullException(nameof(searchKey));
            if (EntityInfoCache<T>.PrimaryKeyProperties.Length > 1) throw new NotSupportedException($"{TypeInfoCache<T>.FullName} has a multi-part primary key. Use method 'T[] Read<T>(Dictionary<string, object> parameters) where T : class' instead.");
            var parameters = new Dictionary<string, object>();
            var primaryKey = EntityInfoCache<T>.PrimaryKeyProperties[0];
            parameters.Add(primaryKey.Name, searchKey);
            var results = await ReadAsync<T>(parameters);
            return results != null && results.Length > 0 ? results[0] : null;
        }

        public T[] Read<T>(Dictionary<string, object> parameters) where T : class
        {
            T[] result = null;
            using (var connection = _factorySet.ConnectionFactory.Open(_connectionString))
            using (var reader = new SqlReader(connection, _factorySet.ParameterFactory, _factorySet.DataAdapterFactory))
            {
                var sql = _factorySet.SqlStatementBuilder.GetSelectStatement<T>(parameters);
                var dataSet = reader.Read(sql, parameters);
                if (dataSet.Tables.Count > 0) result = dataSet.Tables[0].Rows.ToArray<T>();
            }
            return result;
        }

        public async Task<T[]> ReadAsync<T>(Dictionary<string, object> parameters) where T : class
        {
            T[] result = null;
            using (var connection = await _factorySet.ConnectionFactory.OpenAsync(_connectionString))
            using (var reader = new SqlReader(connection, _factorySet.ParameterFactory, _factorySet.DataAdapterFactory))
            {
                var sql = _factorySet.SqlStatementBuilder.GetSelectStatement<T>(parameters);
                var dataSet = await reader.ReadAsync(sql, parameters);
                if (dataSet.Tables.Count > 0) result = await dataSet.Tables[0].Rows.ToArrayAsync<T>();
            }
            return result;
        }

        private Dictionary<string, object> GetWriteParameters<T>() where T : class
        {
            var result = new Dictionary<string, object>(EntityInfoCache<T>.EntityWriteParameterNames.Length);
            for (var i = 0; i < EntityInfoCache<T>.EntityWriteParameterNames.Length; ++i)
            {
                result[EntityInfoCache<T>.EntityWriteParameterNames[i]] = null;
            }
            return result;
        }

        private void SetWriteParameters<T>(T entity, Dictionary<string, object> parameters) where T : class
        {
            for (var i = 0; i < EntityInfoCache<T>.NonAutoIncrementProperties.Length; ++i)
            {
                parameters[EntityInfoCache<T>.EntityWriteParameterNames[i]] =
                    EntityInfoCache<T>.NonAutoIncrementProperties[i].GetValue(entity) ?? DBNull.Value;
            }
        }

        public void Write<T>(T entity) where T : class
        {
            var tableExistsSql = _factorySet.SqlStatementBuilder.GetExistsStatement<T>();
            using (var connection = _factorySet.ConnectionFactory.Open(_connectionString))
            using (var command = new Command(connection, _factorySet.ParameterFactory))
            using (var transaction = _factorySet.TransactionFactory.BeginTransaction(connection))
            {
                try
                {
                    var tableExists = command.ExecuteScalar<bool>(tableExistsSql, transaction);
                    if (!tableExists)
                    {
                        var tableDefinition = _factorySet.ScriptBuilder.BuildTable<T>();
                        var createTableSql = _factorySet.ScriptBuilder.BuildDbCreateScript(tableDefinition);
                        command.Execute(createTableSql, transaction);
                    }
                    var insertSql = _factorySet.SqlStatementBuilder.GetInsertStatement<T>();
                    var parameters = GetWriteParameters<T>();
                    SetWriteParameters(entity, parameters);
                    var rowsInserted = command.Execute(insertSql, parameters, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public async Task WriteAsync<T>(T entity) where T : class
        {
            await Task.Run(async () =>
            {
                var tableExistsSql = _factorySet.SqlStatementBuilder.GetExistsStatement<T>();
                using (var connection = _factorySet.ConnectionFactory.Open(_connectionString))
                using (var command = new Command(connection, _factorySet.ParameterFactory))
                using (var transaction = _factorySet.TransactionFactory.BeginTransaction(connection))
                {
                    try
                    {
                        var tableExists = await command.ExecuteScalarAsync<bool>(tableExistsSql, transaction);
                        if (!tableExists)
                        {
                            var tableDefinition = _factorySet.ScriptBuilder.BuildTable<T>();
                            var createTableSql = _factorySet.ScriptBuilder.BuildDbCreateScript(tableDefinition);
                            await command.ExecuteAsync(createTableSql, transaction);
                        }
                        var insertSql = _factorySet.SqlStatementBuilder.GetInsertStatement<T>();
                        var parameters = GetWriteParameters<T>();
                        SetWriteParameters(entity, parameters);
                        var rowsInserted = await command.ExecuteAsync(insertSql, parameters, transaction);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            });
        }

        public void Write<T>(T[] entities) where T : class
        {
            var tableExistsSql = _factorySet.SqlStatementBuilder.GetExistsStatement<T>();
            using (var connection = _factorySet.ConnectionFactory.Open(_connectionString))
            using (var command = new Command(connection, _factorySet.ParameterFactory))
            using (var transaction = _factorySet.TransactionFactory.BeginTransaction(connection))
            {
                try
                {
                    var tableExists = command.ExecuteScalar<bool>(tableExistsSql, transaction);
                    if (!tableExists)
                    {
                        var tableDefinition = _factorySet.ScriptBuilder.BuildTable<T>();
                        var createTableSql = _factorySet.ScriptBuilder.BuildDbCreateScript(tableDefinition);
                        command.Execute(createTableSql, transaction);
                    }
                    var insertSql = _factorySet.SqlStatementBuilder.GetInsertStatement<T>();
                    var parameters = GetWriteParameters<T>();
                    for (var i = 0; i < entities.Length; ++i)
                    {
                        var entity = entities[i];
                        SetWriteParameters(entity, parameters);
                        var rowsInserted = command.Execute(insertSql, parameters, transaction);
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public async Task WriteAsync<T>(T[] entities) where T : class
        {
            await Task.Run(async () =>
            {
                var tableExistsSql = _factorySet.SqlStatementBuilder.GetExistsStatement<T>();
                using (var connection = _factorySet.ConnectionFactory.Open(_connectionString))
                using (var command = new Command(connection, _factorySet.ParameterFactory))
                using (var transaction = _factorySet.TransactionFactory.BeginTransaction(connection))
                {
                    try
                    {
                        var tableExists = await command.ExecuteScalarAsync<bool>(tableExistsSql, transaction);
                        if (!tableExists)
                        {
                            var tableDefinition = _factorySet.ScriptBuilder.BuildTable<T>();
                            var createTableSql = _factorySet.ScriptBuilder.BuildDbCreateScript(tableDefinition);
                            await command.ExecuteAsync(createTableSql, transaction);
                        }
                        var insertSql = _factorySet.SqlStatementBuilder.GetInsertStatement<T>();
                        var parameters = GetWriteParameters<T>();
                        for (var i = 0; i < entities.Length; ++i)
                        {
                            var entity = entities[i];
                            SetWriteParameters(entity, parameters);
                            var rowsInserted = await command.ExecuteAsync(insertSql, parameters, transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            });
        }
    }
}