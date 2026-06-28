using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using SqlBoTx.Net.Share.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SqlBoTx.Net.EFCore
{
    public class ApplicationDbContextBase : DbContext
    {
        protected readonly IConfiguration _configuration;
        protected ApplicationDbContextBase(DbContextOptions contextOptions, IConfiguration configuration)
         : base(contextOptions)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 构建表模型映射
        /// </summary>
        protected void BuilderTable(EntityTypeBuilder typeBuilder, Type entityType)
        {
            // 表名
            var tableDes = entityType.GetCustomAttribute<DescriptionAttribute>();
            var tableDesName = tableDes != null && !string.IsNullOrWhiteSpace(tableDes.Description) ? tableDes.Description : entityType.Name;
            typeBuilder.ToTable(StringHelper.ConvertToSnakeCase(entityType.Name), (p) => p.HasComment(tableDesName));
        }

        /// <summary>
        /// 构建表属性字段映射
        /// </summary>
        protected void BuilderProperty(EntityTypeBuilder typeBuilder, Type entityType)
        {
            // 遍历将被添加到表关系的属性
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => !p.GetMethod.IsVirtual || p.GetMethod.IsFinal);
            foreach (var property in properties)
            {
                // 表字段列名
                var propertybuilder = typeBuilder.Property(property.Name);
                propertybuilder.HasColumnName(StringHelper.ConvertToSnakeCase(property.Name));

                // 表字段默认类型
                if (property.PropertyType == typeof(string))
                {
                    var column = property.GetCustomAttribute<ColumnAttribute>();
                    if (column == null || string.IsNullOrEmpty(column.TypeName))
                    {
                        propertybuilder.HasColumnType("NVARCHAR(255)");
                    }
                }

                // 字段描述
                var description = property.GetCustomAttribute<DescriptionAttribute>();
                if (description != null && !string.IsNullOrWhiteSpace(description.Description))
                {
                    propertybuilder.HasComment(description.Description);
                }
            }

        }
    }
}
