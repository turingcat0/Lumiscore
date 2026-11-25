using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lumiscore.Core
{
    public static class StageTypeRegistry
    {
        private static bool s_Initialized;
        private static readonly List<Type> s_ModuleTypes = new List<Type>();
        private static readonly List<Type> s_EffectTypes = new List<Type>();
        private static readonly Dictionary<Type, object> s_MixersByModule = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, List<Type>> s_DerivedTypeCache = new Dictionary<Type, List<Type>>();

        public static IReadOnlyList<Type> ModuleTypes
        {
            get
            {
                EnsureInitialized();
                return s_ModuleTypes;
            }
        }

        public static IReadOnlyList<Type> EffectTypes
        {
            get
            {
                EnsureInitialized();
                return s_EffectTypes;
            }
        }

        public static void Invalidate()
        {
            s_Initialized = false;
            s_ModuleTypes.Clear();
            s_EffectTypes.Clear();
            s_MixersByModule.Clear();
            s_DerivedTypeCache.Clear();
        }

        public static void RegisterMixer<TModule>(IStageModuleMixer<TModule> mixer)
            where TModule : class, IStageStateModule
        {
            EnsureInitialized();
            if (mixer == null)
            {
                s_MixersByModule.Remove(typeof(TModule));
            }
            else
            {
                s_MixersByModule[typeof(TModule)] = mixer;
            }
        }

        public static IStageModuleMixer<TModule> GetMixer<TModule>()
            where TModule : class, IStageStateModule
        {
            return GetMixerForModule(typeof(TModule)) as IStageModuleMixer<TModule>;
        }

        public static object GetMixerForModule(Type moduleType)
        {
            if (moduleType == null)
            {
                return null;
            }

            EnsureInitialized();
            s_MixersByModule.TryGetValue(moduleType, out object mixer);
            return mixer;
        }

        public static IReadOnlyList<Type> GetConcreteTypesDerivedFrom(Type baseOrInterfaceType)
        {
            if (baseOrInterfaceType == null)
            {
                return Array.Empty<Type>();
            }

            if (s_DerivedTypeCache.TryGetValue(baseOrInterfaceType, out List<Type> cached))
            {
                return cached;
            }

            var result = new List<Type>();
            CollectConcreteTypes(baseOrInterfaceType, result);
            s_DerivedTypeCache[baseOrInterfaceType] = result;
            return result;
        }

        private static void EnsureInitialized()
        {
            if (s_Initialized)
            {
                return;
            }

            s_Initialized = true;
            s_ModuleTypes.Clear();
            s_EffectTypes.Clear();
            s_MixersByModule.Clear();

            CollectConcreteTypes(typeof(IStageStateModule), s_ModuleTypes);
            CollectConcreteTypes(typeof(StageEffectBase), s_EffectTypes);

            var mixerTypes = new List<Type>();
            CollectConcreteTypes(typeof(IStageModuleMixer<>), mixerTypes);

            for (int i = 0; i < mixerTypes.Count; i++)
            {
                Type mixerType = mixerTypes[i];
                if (mixerType.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                Type[] interfaces = mixerType.GetInterfaces();
                for (int j = 0; j < interfaces.Length; j++)
                {
                    Type iface = interfaces[j];
                    if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IStageModuleMixer<>))
                    {
                        continue;
                    }

                    Type moduleType = iface.GetGenericArguments()[0];
                    if (s_MixersByModule.TryGetValue(moduleType, out object existing))
                    {
                        Debug.LogError(
                            $"[Lumiscore] Duplicate IStageModuleMixer<{moduleType.Name}> found: " +
                            $"{existing.GetType().FullName} and {mixerType.FullName}. Keeping {existing.GetType().FullName}.");
                        continue;
                    }

                    try
                    {
                        s_MixersByModule[moduleType] = Activator.CreateInstance(mixerType);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Lumiscore] Failed to instantiate mixer {mixerType.FullName}: {ex.Message}");
                    }
                }
            }
        }

        private static void CollectConcreteTypes(Type baseOrInterfaceType, List<Type> results)
        {
#if UNITY_EDITOR
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(baseOrInterfaceType);
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                if (IsConcreteType(type))
                {
                    results.Add(type);
                }
            }
#else
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Assembly assembly = assemblies[a];
                if (IsIgnoredSystemAssembly(assembly.GetName().Name))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type != null && IsConcreteType(type) && MatchesBaseOrInterface(type, baseOrInterfaceType))
                    {
                        results.Add(type);
                    }
                }
            }
#endif
        }

        private static bool IsConcreteType(Type type)
            => type != null && !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition;

#if !UNITY_EDITOR
        private static bool IsIgnoredSystemAssembly(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            return name.StartsWith("System", StringComparison.Ordinal) ||
                   name.StartsWith("Unity", StringComparison.Ordinal) ||
                   name.StartsWith("mscorlib", StringComparison.Ordinal) ||
                   name.StartsWith("Mono.", StringComparison.Ordinal) ||
                   name.StartsWith("netstandard", StringComparison.Ordinal);
        }

        private static bool MatchesBaseOrInterface(Type candidate, Type baseOrInterfaceType)
        {
            if (!baseOrInterfaceType.IsGenericTypeDefinition)
            {
                return baseOrInterfaceType.IsAssignableFrom(candidate);
            }

            if (baseOrInterfaceType.IsInterface)
            {
                Type[] interfaces = candidate.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (interfaces[i].IsGenericType &&
                        interfaces[i].GetGenericTypeDefinition() == baseOrInterfaceType)
                    {
                        return true;
                    }
                }

                return false;
            }

            for (Type current = candidate.BaseType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == baseOrInterfaceType)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
