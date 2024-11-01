using System.Reflection;

namespace Graphs.Extensions;

public static class TypeExtensions
{
    /// <summary>
    /// TODO: Target certain constructors instead of the first constructor
    /// </summary>
    public static (ConstructorInfo?, IEnumerable<Type>) GetTypesFromFirstConstructor(this Type type)
    {
        var types = new List<Type>();
        var ctorInfos = type.GetConstructors();
        if (ctorInfos is null || ctorInfos.Length == 0)
        {
            return (null, types);
        }
        var ctorInfo = ctorInfos[0];
        if (ctorInfo is null)
        {
            return (ctorInfo, types);
        }
        foreach (var parameterInfo in ctorInfo.GetParameters())
        {
            types.Add(parameterInfo.ParameterType);
        }
        return (ctorInfo, types);
    }

    public static bool IsTypeOrSubclassOf(this Type type1, Type type2)
    {
        return type1 == type2 || type2.IsSubclassOf(type2);
    }

    /// <summary>
    /// https://stackoverflow.com/questions/374651/how-to-check-if-an-object-is-nullable
    /// </summary>
    public static bool IsNullable(this Type type)
    {
        if (!type.IsValueType) return true; // ref-type
        if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
        return false; // value-type
    }

    public static (ConstructorInfo?, IEnumerable<Type>?, IEnumerable<object?>) GetConstructorArgInstances(this Type type, IEnumerable<object> possibleCtorParameters)
    {
        var ctorParameterInstances = new List<object?>();
        var (ctorInfo, constructorArgTypes) = type.GetTypesFromFirstConstructor();
        if (constructorArgTypes is null || ctorInfo is null)
        {
            return (null, null, ctorParameterInstances);
        }
        foreach (var parameterType in constructorArgTypes)
        {
            var ctorParameter = possibleCtorParameters.FirstOrDefault(node => node.GetType().IsAssignableFrom(parameterType));
            if (ctorParameter is not null)
            {
                ctorParameterInstances.Add(ctorParameter);
                continue;
            }
            if (parameterType.IsNullable())
            {
                ctorParameterInstances.Add(null);
                continue;
            }
            throw new ArgumentOutOfRangeException($"Failed to find parameter instance of {parameterType.Name} that is necessary to construct {type.Name}");
        }
        return (ctorInfo, constructorArgTypes, ctorParameterInstances);
    }
}
