using System;
using System.Collections.Generic;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public static class LightTypeCatalog
    {
        private static readonly Dictionary<Type, Type[]> s_StateModulesCache = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<Type, List<Type>> s_EffectsByModuleCache = new Dictionary<Type, List<Type>>();

        public static IReadOnlyList<Type> GetModuleTypesForState(Type stateType)
        {
            if (stateType == null || !typeof(ILightState).IsAssignableFrom(stateType))
            {
                return Array.Empty<Type>();
            }

            if (s_StateModulesCache.TryGetValue(stateType, out Type[] cached))
            {
                return cached;
            }

            var modules = new List<Type>();
            Type[] interfaces = stateType.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type iface = interfaces[i];
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IHasLightModule<>))
                {
                    Type moduleType = iface.GetGenericArguments()[0];
                    if (!modules.Contains(moduleType))
                    {
                        modules.Add(moduleType);
                    }
                }
            }

            Type[] result = modules.ToArray();
            s_StateModulesCache[stateType] = result;
            return result;
        }

        public static IReadOnlyList<Type> GetEffectTypesForModule(Type moduleType)
        {
            if (moduleType == null)
            {
                return Array.Empty<Type>();
            }

            if (s_EffectsByModuleCache.TryGetValue(moduleType, out List<Type> cached))
            {
                return cached;
            }

            var matched = new List<Type>();
            IReadOnlyList<Type> allEffects = StageTypeRegistry.EffectTypes;
            for (int i = 0; i < allEffects.Count; i++)
            {
                Type effectType = allEffects[i];
                if (TryGetEffectModuleType(effectType, out Type effectModuleType) && effectModuleType == moduleType)
                {
                    matched.Add(effectType);
                }
            }

            s_EffectsByModuleCache[moduleType] = matched;
            return matched;
        }

        public static IReadOnlyList<Type> GetLayoutTypes()
            => StageTypeRegistry.GetConcreteTypesDerivedFrom(typeof(ILightLayout));

        public static bool TryGetEffectModuleType(Type effectType, out Type moduleType)
        {
            for (Type current = effectType; current != null && current != typeof(object); current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(LightEffectBase<>))
                {
                    moduleType = current.GetGenericArguments()[0];
                    return true;
                }
            }

            moduleType = null;
            return false;
        }

        public static void Invalidate()
        {
            s_StateModulesCache.Clear();
            s_EffectsByModuleCache.Clear();
        }
    }
}
