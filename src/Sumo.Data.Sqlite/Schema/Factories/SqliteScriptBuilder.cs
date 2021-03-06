﻿using System.Reflection;

namespace Sumo.Data.Schema.Sqlite
{
    public class SqliteScriptBuilder : IScriptBuilder
    {
        public string BuildAlterTableScript(Table table, Column[] columnsToAdd, Column[] columnsToRemove)
        {
            throw new System.NotImplementedException();
        }

        public string BuildCreateScript(Catalog catalog)
        {
            return catalog.ToCreateScript(true);
        }

        public string BuildCreateScript(Schema schema)
        {
            return schema.ToCreateScript(true);
        }

        public string BuildCreateScript(Table table)
        {
            return table.ToCreateScript(true);
        }

        public Table BuildTable<T>() where T : class
        {
            var table = new Table(TypeInfoCache<T>.Name);
            for (var i = 0; i < TypeInfoCache<T>.ReadWriteProperties.Length; ++i)
            {
                var property = TypeInfoCache<T>.ReadWriteProperties[i];
                var dbType = property.PropertyType.ToDbType();
                var column = table.AddColumn(property.Name, dbType);
                var pkAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                column.IsPrimaryKey = pkAttribute != null;
                if (column.IsPrimaryKey)
                {
                    column.PrimaryKey.Direction = pkAttribute.Direction;
                    column.PrimaryKey.IsAutoIncrement = pkAttribute.AutoIncrement;
                }
                else
                {
                    column.IsNullable = property.GetCustomAttribute<RequiredAttribute>() == null;
                    column.IsUnique = property.GetCustomAttribute<UniqueAttribute>() != null;
                    var defaultAttribute = property.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultAttribute != null) column.Default = defaultAttribute.Value;
                    var commentAttribute = property.GetCustomAttribute<CommentAttribute>();
                    if (commentAttribute != null) column.AddComment(commentAttribute.Comment);
                    if (dbType.IsSizableType())
                    {
                        var maxSizeAttribute = property.GetCustomAttribute<MaxSizeAttribute>();
                        if (maxSizeAttribute != null) column.MaxLength = maxSizeAttribute.Size;
                    }
                }
                var fkAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
                column.HasForeignKey = fkAttribute != null;
                if (column.HasForeignKey)
                {
                    column.ForeignKey.Schema = fkAttribute.Schema;
                    column.ForeignKey.Table = fkAttribute.ReferenceTable;
                    column.ForeignKey.Column = fkAttribute.ReferenceColumn;
                }
            }
            return table;
        }
    }
}
