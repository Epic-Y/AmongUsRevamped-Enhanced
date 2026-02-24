using System;
using System.Linq.Expressions;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace AmongUsRevamped;


// Credit: TommyXL
public static class Il2CppCastHelper
{
    public static T CastFast<T>(this Il2CppObjectBase obj) where T : Il2CppObjectBase
    {
        if (obj is T casted) return casted;
#if ANDROID
        return obj.Cast<T>();
#else
        return obj.Pointer.CastFast<T>();
#endif
    }

    private static T CastFast<T>(this IntPtr ptr) where T : Il2CppObjectBase
    {
        return CastHelper<T>.Cast(ptr);
    }

    private static class CastHelper<T> where T : Il2CppObjectBase
    {
        public static readonly Func<IntPtr, T> Cast;

        static CastHelper()
        {
            ConstructorInfo constructor = typeof(T).GetConstructor([typeof(IntPtr)]);
            ParameterExpression ptr = Expression.Parameter(typeof(IntPtr));
            NewExpression create = Expression.New(constructor!, ptr);
            Expression<Func<IntPtr, T>> lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            Cast = lambda.Compile();
        }
    }
}