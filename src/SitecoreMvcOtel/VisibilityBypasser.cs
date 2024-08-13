using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SitecoreMvcOtel;

internal class VisibilityBypasser
{
    public static readonly VisibilityBypasser Instance = new();

    private VisibilityBypasser() { }

    public Func<object, TResult> GenerateFieldReadAccessor<TResult>(Type ownerType, string fieldName)
    {
        if (ownerType == null)
        {
            throw new ArgumentNullException(nameof(ownerType));
        }

        if (fieldName == null)
        {
            throw new ArgumentNullException(nameof(fieldName));
        }

        var dynamicMethod = GenerateFieldReadAccessorInternal<TResult>(ownerType, fieldName);

        return (Func<object, TResult>)dynamicMethod.CreateDelegate(typeof(Func<object, TResult>));
    }

    private static DynamicMethod GenerateFieldReadAccessorInternal<TResult>(Type ownerType, string fieldName)
    {
        var fieldInfo = GetFieldInfo(ownerType, fieldName);

        return GenerateFieldReadAccessorInternal<TResult>(fieldInfo);
    }

    private static DynamicMethod GenerateFieldReadAccessorInternal<TResult>(FieldInfo fieldInfo)
    {
        var resultType = typeof(TResult);

        if (!resultType.IsAssignableFrom(fieldInfo.FieldType))
        {
            throw new Exception(string.Format("The return type for field {0} does not inherit or implement {1}", fieldInfo.Name, resultType.AssemblyQualifiedName));
        }

        var ownerType = fieldInfo.DeclaringType;

        if (ownerType == null)
        {
            throw new NullReferenceException(nameof(ownerType));
        }

        var dynamicMethod = CreateDynamicMethod(ownerType, resultType, new[] { typeof(object) });
        var ilGenerator = dynamicMethod.GetILGenerator();

        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Castclass, ownerType);
        ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
        ilGenerator.Emit(OpCodes.Ret);

        return dynamicMethod;
    }

    private static FieldInfo GetFieldInfo(Type type, string fieldName)
    {
        var fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        if (fieldInfo == null)
        {
            throw new KeyNotFoundException(string.Format("Unable to find field {0} in type {1}", fieldName, type.AssemblyQualifiedName));
        }

        return fieldInfo;
    }

    private static DynamicMethod CreateDynamicMethod(Type ownerType, Type resultType, params Type[] parameterTypes) => new(Guid.NewGuid().ToString(), resultType, parameterTypes, ownerType, skipVisibility: true);
}