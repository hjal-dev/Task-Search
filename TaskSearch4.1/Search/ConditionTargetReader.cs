using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.Quests;
using Newtonsoft.Json;

namespace TaskSearch.Search
{
    internal static class ConditionTargetReader
    {
        private static readonly Dictionary<Type, Func<object, object>[]> Cache =
            new Dictionary<Type, Func<object, object>[]>();

        private static readonly HashSet<string> TargetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "target",
            "targets",
            "zoneId",
            "zoneIds",
            "zoneID",
            "exitName",
            "mapNames",
            "containsItems"
        };

        internal static void Collect(Condition condition, ICollection<string> into)
        {
            if (condition == null || into == null)
            {
                return;
            }

            foreach (Func<object, object> read in GetAccessors(condition.GetType()))
            {
                object value;

                try
                {
                    value = read(condition);
                }
                catch (Exception)
                {
                    continue;
                }

                if (value is string single)
                {
                    if (!string.IsNullOrEmpty(single))
                    {
                        into.Add(single);
                    }

                    continue;
                }

                if (value is string[] many)
                {
                    foreach (string item in many)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            into.Add(item);
                        }
                    }
                }
            }
        }

        private static Func<object, object>[] GetAccessors(Type type)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(type, out Func<object, object>[] cached))
                {
                    return cached;
                }
            }

            List<Func<object, object>> accessors = new List<Func<object, object>>();

            try
            {
                const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic
                                           | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
                {
                    foreach (FieldInfo field in current.GetFields(Flags))
                    {
                        if (!IsStringLike(field.FieldType) || !IsTargetMember(field, field.Name))
                        {
                            continue;
                        }

                        FieldInfo captured = field;
                        accessors.Add(instance => captured.GetValue(instance));
                    }

                    foreach (PropertyInfo property in current.GetProperties(Flags))
                    {
                        if (!IsStringLike(property.PropertyType)
                            || !property.CanRead
                            || property.GetIndexParameters().Length > 0
                            || !IsTargetMember(property, property.Name))
                        {
                            continue;
                        }

                        MethodInfo getter = property.GetGetMethod(nonPublic: true);

                        if (getter == null)
                        {
                            continue;
                        }

                        PropertyInfo capturedProperty = property;
                        accessors.Add(instance => capturedProperty.GetValue(instance, null));
                    }
                }
            }
            catch (Exception)
            {
            }

            Func<object, object>[] result = accessors.ToArray();

            lock (Cache)
            {
                Cache[type] = result;
            }

            return result;
        }

        private static bool IsStringLike(Type type)
        {
            return type == typeof(string) || type == typeof(string[]);
        }

        private static bool IsTargetMember(MemberInfo member, string name)
        {
            if (name.IndexOf("k__BackingField", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            if (TargetNames.Contains(name))
            {
                return true;
            }

            try
            {
                JsonPropertyAttribute json = member.GetCustomAttribute<JsonPropertyAttribute>();
                return json?.PropertyName != null && TargetNames.Contains(json.PropertyName);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
