using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Slaggy.Reflection
{
    public static class ReflectionUtilities
    {
        // https://stackoverflow.com/questions/9201859/why-doesnt-type-getfields-return-backing-fields-in-a-base-class
        public static FieldInfo[] GetFieldInfosIncludingBaseClasses(Type type, BindingFlags bindingFlags)
        {
            FieldInfo[] fieldInfos = type.GetFields(bindingFlags);

            // If this class doesn't have a base, don't waste any time
            if (type.BaseType == typeof(object)) return fieldInfos;

            // Otherwise, collect all types up to the furthest base class
            var currentType = type;
            var fieldComparer = new FieldInfoComparer();
            var fieldInfoList = new HashSet<FieldInfo>(fieldInfos, fieldComparer);
            while (currentType != typeof(object))
            {
                fieldInfos = currentType.GetFields(bindingFlags);
                fieldInfoList.UnionWith(fieldInfos);
                currentType = currentType.BaseType;
            }

            return fieldInfoList.ToArray();
        }

        private class FieldInfoComparer : IEqualityComparer<FieldInfo>
        {
            public bool Equals(FieldInfo x, FieldInfo y) =>
                x != null && y != null && x.DeclaringType == y.DeclaringType && x.Name == y.Name;

            public int GetHashCode(FieldInfo obj) =>
                obj.Name.GetHashCode() ^ obj.DeclaringType.GetHashCode();
        }
    }

    public static class TypeExtensionsFieldInfo
    {
        public static FieldInfo[] GetFieldsIncludingBase(this Type type, BindingFlags bindingFlags)
            => ReflectionUtilities.GetFieldInfosIncludingBaseClasses(type, bindingFlags);
    }
}
