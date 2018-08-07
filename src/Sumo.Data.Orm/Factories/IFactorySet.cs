﻿using Sumo.Data.Factories;
using Sumo.Data.Schema;
using Sumo.Data.Schema.Factories;

namespace Sumo.Data.Orm.Factories
{
    public interface IFactorySet
    {
        IConnectionFactory ConnectionFactory { get; }
        IDataAdapterFactory DataAdapterFactory { get; }
        ISchemaParameterFactory ParameterFactory { get; }
        ITransactionFactory TransactionFactory { get; }
        IScriptBuilder ScriptBuilder { get; }
        ISqlStatementBuilder SqlStatementBuilder { get; }
    }
}