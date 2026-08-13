using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaskSearch.Logging;

namespace TaskSearch.Eft
{
    internal static class EftLocalization
    {
        private static Func<string, string, string> _localized;
        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            MethodInfo method = FindFromQuestNameIl();

            if (method == null)
            {
                method = FindByKnownTypeName();
            }

            if (method == null)
            {
                method = FindByAssemblyScan();
            }

            if (method == null)
            {
                TaskSearchLog.Warn(
                    "Could not locate EFT's localization method. Quest names and descriptions will still " +
                    "work, but item/location/trader/objective names will be indexed by raw id only.");
                return;
            }

            try
            {
                _localized = (Func<string, string, string>)Delegate.CreateDelegate(
                    typeof(Func<string, string, string>), method);
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Failed to bind EFT localization method", ex);
            }
        }

        private static MethodInfo FindFromQuestNameIl()
        {
            try
            {
                PropertyInfo nameProperty = AccessTools.Property(typeof(RawQuestClass), "Name");
                MethodInfo getter = nameProperty?.GetGetMethod(nonPublic: true);
                if (getter == null)
                {
                    return null;
                }

                foreach (KeyValuePair<OpCode, object> instruction in PatchProcessor.ReadMethodBody(getter))
                {
                    if (instruction.Key != OpCodes.Call && instruction.Key != OpCodes.Callvirt)
                    {
                        continue;
                    }

                    if (instruction.Value is MethodInfo candidate && IsLocalizeSignature(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static MethodInfo FindByKnownTypeName()
        {
            try
            {
                Assembly assembly = typeof(RawQuestClass).Assembly;

                foreach (string typeName in new[] { "EFT.LocalizationExtensions", "LocalizationExtensions", "GClass2348" })
                {
                    Type type = assembly.GetType(typeName, throwOnError: false);

                    if (type == null)
                    {
                        continue;
                    }

                    MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

                    for (int i = 0; i < methods.Length; i++)
                    {
                        if (IsLocalizeSignature(methods[i]))
                        {
                            return methods[i];
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static MethodInfo FindByAssemblyScan()
        {
            try
            {
                Type[] types = GetTypesSafely(typeof(RawQuestClass).Assembly);

                foreach (Type type in types)
                {
                    if (type == null || type.IsGenericTypeDefinition)
                    {
                        continue;
                    }

                    MethodInfo[] methods = type.GetMethods(
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

                    for (int i = 0; i < methods.Length; i++)
                    {
                        if (IsLocalizeSignature(methods[i]))
                        {
                            return methods[i];
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static Type[] GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                List<Type> loaded = new List<Type>();

                for (int i = 0; i < ex.Types.Length; i++)
                {
                    if (ex.Types[i] != null)
                    {
                        loaded.Add(ex.Types[i]);
                    }
                }

                return loaded.ToArray();
            }
        }

        private static bool IsLocalizeSignature(MethodInfo method)
        {
            if (method == null || !method.IsStatic || method.Name != "Localized")
            {
                return false;
            }

            if (method.ReturnType != typeof(string) || method.IsGenericMethodDefinition)
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 2
                   && parameters[0].ParameterType == typeof(string)
                   && parameters[1].ParameterType == typeof(string);
        }

        internal static string Localize(string key)
        {
            if (string.IsNullOrEmpty(key) || _localized == null)
            {
                return key;
            }

            try
            {
                return _localized(key, null) ?? key;
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("localize:" + key, $"Localizing '{key}' threw: {ex.Message}");
                return key;
            }
        }

        internal static bool TryLocalize(string key, out string value)
        {
            value = null;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string localized = Localize(key);

            if (string.IsNullOrEmpty(localized) || string.Equals(localized, key, StringComparison.Ordinal))
            {
                return false;
            }

            value = localized;
            return true;
        }

        internal static void CollectItemNames(string templateId, ICollection<string> into)
        {
            if (string.IsNullOrEmpty(templateId) || into == null)
            {
                return;
            }

            if (TryLocalize(templateId + " Name", out string name))
            {
                into.Add(name);
            }

            if (TryLocalize(templateId + " ShortName", out string shortName))
            {
                into.Add(shortName);
            }
        }

        internal static bool TryGetLocationName(string locationId, out string name)
        {
            name = null;

            if (string.IsNullOrEmpty(locationId))
            {
                return false;
            }

            return TryLocalize(locationId + " Name", out name);
        }
    }
}
