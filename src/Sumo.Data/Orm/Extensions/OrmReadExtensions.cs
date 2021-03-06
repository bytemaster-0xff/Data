﻿using Sumo.Data.Schema;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// this namespace is correct. don't add .Orm to it.
namespace Sumo.Data
{
    public static class OrmReadExtensions
    {
        public static T ToObject<T>(this DataRow row) where T : class
        {
            // allows activator to use non-public constructors
            var result = (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, null, null);
            //var result = Activator.CreateInstance<T>();
            for (var i = 0; i < TypeInfoCache<T>.Properties.Length; ++i)
            {
                var property = TypeInfoCache<T>.Properties[i];
                if (!row.IsNull(property.Name))
                {
                    //todo: this can be optimized by passing in the table definition or a cache of row types from the ToArray methods
                    //todo: this can be optimized by getting the underlying nullable types in TypeInfoCache 
                    var columnValue = row[property.Name];
                    var isNullable = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var propertyType = isNullable ? Nullable.GetUnderlyingType(property.PropertyType) : property.PropertyType;
                    var value = propertyType == columnValue.GetType() ?
                        columnValue :
                        Convert.ChangeType(columnValue, property.PropertyType);
                    property.SetValue(result, value);
                }
            }
            return result;
        }

        //public static T ToObject<T>(this DataRow row, types) where T : class
        //{
        //    // allows activator to use non-public constructors
        //    var result = (T)Activator.CreateInstance(
        //        typeof(T),
        //        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
        //        null, null, null);
        //    //var result = Activator.CreateInstance<T>();
        //    for (var i = 0; i < TypeInfoCache<T>.Properties.Length; ++i)
        //    {
        //        var property = TypeInfoCache<T>.Properties[i];
        //        if (!row.IsNull(property.Name))
        //        {
        //            var columnValue = row[property.Name];
        //            columnValue.
        //            var value = Convert.ChangeType(row[property.Name], property.PropertyType);
        //            property.SetValue(result, value);
        //        }
        //    }
        //    return result;
        //}

        /// <summary>
        /// for binary types
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static object[] ToObjectArray(this DataRowCollection rows)
        {
            var result = new object[rows.Count];
            for (var i = 0; i < rows.Count; ++i)
            {
                result[i] = rows[i][0];
            }
            return result;
        }

        public static object[] ReadRowFromStream(this BinaryReader reader, Table table)
        {
            var row = new object[table.Columns.Count];
            foreach (var col in table.Columns.OrderBy(cols => cols.OrdinalPosition))
            {
                if (col.IsNullable)
                {
                    row[col.OrdinalPosition - 1] = (reader.ReadByte() == 1) ? reader.Read(col.DataType) : null;
                }
                else
                {
                    row[col.OrdinalPosition - 1] = reader.Read(col.DataType);
                }
            }

            return row;
        }

        public static void WriteToStream(this BinaryWriter writer, Table table, DataRow row)
        {
            //TODO: do an assertion on all ordinals not being zero
            foreach (var col in table.Columns.OrderBy(cols => cols.OrdinalPosition))
            {
                if (col.IsNullable)
                {
                    if (row.IsNull(col.Name))
                    {
                        writer.Write((byte)0);
                    }
                    else
                    {
                        writer.Write((byte)1);
                        var value = row[col.Name];
                        value.Write(writer, col.DataType);
                    }
                }
                else
                {
                    var value = row[col.Name];
                    value.Write(writer, col.DataType);
                }
            }
        }

        public static void WriteToStream(this BinaryWriter writer, Table table, DataRowCollection rows)
        {
            for (var idx = 0; idx < rows.Count; ++idx)
            {
                writer.WriteToStream(table, rows[idx]);
            }
        }

        /// <summary>
        /// for value types like string, datetime, and numeric types 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static T[] ToValueArray<T>(this DataRowCollection rows) where T : struct
        {
            var result = new T[rows.Count];
            if (rows.Count > 0)
            {
                var type = typeof(T);
                var isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
                type = isNullable ? Nullable.GetUnderlyingType(type) : type;
                var doConvert = type != rows[0][0].GetType();
                for (var i = 0; i < rows.Count; ++i)
                {
                    result[i] = (T)(doConvert ? rows[i][0] : Convert.ChangeType(rows[i][0], type));
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplentation">The type to use to for the interface implementation. Must have public or protected constructor.</typeparam>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static TInterface[] ToArray<TInterface, TImplentation>(this DataRowCollection rows) where TImplentation : class, TInterface
        {
            var result = new TInterface[rows.Count];
            for (var i = 0; i < rows.Count; ++i)
            {
                result[i] = rows[i].ToObject<TImplentation>();
            }
            return result;
        }

        public static T[] ToArray<T>(this DataRowCollection rows) where T : class
        {
            var result = new T[rows.Count];
            for (var i = 0; i < rows.Count; ++i)
            {
                result[i] = rows[i].ToObject<T>();
            }
            return result;
        }

        public static async Task<T[]> ToArrayAsync<T>(this DataRowCollection rows) where T : class
        {
            return await Task.Run(() =>
            {
                var result = new T[rows.Count];
                for (var i = 0; i < rows.Count; ++i)
                {
                    result[i] = rows[i].ToObject<T>();
                }
                return result;
            });
        }

        public static T[] ToArrayParallel<T>(this DataRowCollection rows) where T : class
        {
            var result = new T[rows.Count];
            Parallel.For(0, rows.Count, i =>
            {
                result[i] = rows[i].ToObject<T>();
            });
            return result;
        }
    }
}
