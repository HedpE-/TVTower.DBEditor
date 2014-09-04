﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeKnight.Core;
using System.Collections;
using TVTower.Entities;

namespace TVTower.Converter
{
    public static class DBValueConverter
    {
        public static object ConvertDBValueToType(Type type, object value)
        {
            if (value is DBNull)
                value = null;

            Type nullableType = Nullable.GetUnderlyingType(type);

            if (nullableType != null) // It's nullable
                type = nullableType;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CodeKnight.Core.WeakReference<>))
            {
                var refType = type.GetGenericArguments()[0];
                var weakRefValue = ConvertDBValueToType(refType, value);

                var constructor = type.GetConstructor(new Type[] { refType });
                return constructor.Invoke(new object[] {weakRefValue });
            }

            if (type == typeof(string))
            {
                if (value == null)
                    return null;
                else
                    return value.ToString();
            }
            else if (type == typeof(int))
            {
                if (value != null)
                    return int.Parse(value.ToString());
                else
                    return 0;
            }
            else if (type == typeof(bool))
            {
                bool valueBool = false;
                int intValue = 0;

                if (int.TryParse(value.ToString(), out intValue))
                {
                    if (intValue == 0)
                        return false;
                    else if (intValue == 1)
                        return true;
                }
                else
                {
                    return bool.Parse(value.ToString());
                }
            }
            else if (type == typeof(Guid))
            {
                Guid guid;
                if (Guid.TryParse(value.ToString(), out guid))
                    return guid;
                else
                    return Guid.Empty;
            }
            else if (type.IsEnum)
            {
                return Enum.Parse(type, value.ToString());
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var listType = type.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));

                if (value != null)
                {
                    foreach (var currValue in value.ToString().ToStringList())
                    {
                        var convertedType = ConvertDBValueToType(listType, currValue);
                        list.Add(convertedType);
                    }
                    return list;
                }
                else
                    return null;
            }
            else if (type == typeof(TVTProgramme))
            {
                if (value != null)
                {
                    var result = (TVTEntity)Activator.CreateInstance(type);
                    result.OnlyReference = true;
                    
                    Guid guid;
                    if (Guid.TryParse(value.ToString(), out guid))
                    {
                        result.Id = guid;
                        return result;
                    }
                    else
                        return null;
                }
                else
                    return null;
            }
            else if (type == typeof(TVTPerson))
            {
                if (value != null)
                {
                    var result = (TVTEntity)Activator.CreateInstance(type);
                    result.OnlyReference = true;
                    result.Id = Guid.Parse(value.ToString());
                    return result;
                }
                else
                    return null;
            }
            else if (type == typeof(TVTNews))
            {
                if (value != null)
                {
                    var result = (TVTEntity)Activator.CreateInstance(type);
                    result.OnlyReference = true;
                    result.Id = Guid.Parse(value.ToString());
                    return result;
                }
                else
                    return null;
            }
            else
            {
                throw new NotSupportedException();
            }

            return null;
        }
    }
    
}
