using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Numeira
{
    internal static class Ext
    {
        public static Span<T> AsSpan<T>(this List<T> list)
        {
            var tuple = Unsafe.As<Tuple<T[], int>>(list);
            return tuple.Item1.AsSpan(0, tuple.Item2);
        }

        public static Span<T> Skip<T>(this Span<T> span, int count) => span[Math.Min(span.Length - 1, count)..];

        public static ReadOnlySpan<T> Skip<T>(this ReadOnlySpan<T> span, int count) => span[Math.Min(span.Length - 1, count)..];

        public static bool Find(this Span<ShaderFallbackSettings> span, InheritMode mode)
        {
            if (span.IsEmpty)
                return false;

            foreach (var x in span)
            {
                if (x.Inherit == mode)
                    return true;
            }

            return false;
        }

        public static bool Find<T>(this Span<T> span, T material) where T : UnityEngine.Object
        {
            foreach(var x in span)
            {
                if (material == x)
                    return true;
            }
            return false;
        }
    }

    internal static class EnumExt<T>
    {
        public static readonly string[] Names = Enum.GetNames(typeof(T));
    }

    internal static class ListExt<T>
    {
        public static readonly List<T> Shared = new();
    }
}