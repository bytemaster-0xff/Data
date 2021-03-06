﻿using System;
using System.Data;

namespace Sumo.Procedure.CodeGen
{
    internal static class TypeExtensions
    {
        public static SqlDbType ToSqlDbType(this string typeName)
        {
            if (!Enum.TryParse(typeName, true, out SqlDbType sqlDbType)) throw new ArgumentOutOfRangeException($"{nameof(typeName)}:{typeName}");
            return sqlDbType;
        }
    }
}
