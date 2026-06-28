using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace SqlBoTx.Net.Share.Helpers
{
    public static class EnumExtensions
    {
        extension(Enum enumName) {

            public string GetDescription()
            {
                var description = string.Empty;
                var fieldInfo = enumName.GetType().GetField(enumName.ToString());
                var attributes = GetDescriptAttr(enumName, fieldInfo);
                if (attributes != null && attributes.Length > 0)
                {
                    description = attributes[0].Description;
                }
                else
                {
                    description = enumName.ToString();
                }
                return description;


            }

            private DescriptionAttribute[] GetDescriptAttr(FieldInfo fieldInfo)
            {
                if (fieldInfo != null)
                {
                    return (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                }
                return null;
            }
        }
    }
}
