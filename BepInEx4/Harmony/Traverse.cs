using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Harmony
{
    public class Traverse
    {
        private static readonly AccessCache Cache;

        private readonly MemberInfo _info;

        private readonly MethodBase _method;

        private readonly object[] _params;

        private readonly object _root;

        private readonly Type _type;

        [MethodImpl(MethodImplOptions.Synchronized)]
        static Traverse()
        {
            if (Cache == null) Cache = new AccessCache();
        }

        private Traverse()
        {
        }

        public Traverse(Type type)
        {
            _type = type;
        }

        public Traverse(object root)
        {
            _root = root;
            _type = root != null ? root.GetType() : null;
        }

        private Traverse(object root, MemberInfo info, object[] index)
        {
            _root = root;
            _type = root != null ? root.GetType() : null;
            _info = info;
            _params = index;
        }

        private Traverse(object root, MethodInfo method, object[] parameter)
        {
            _root = root;
            _type = method.ReturnType;
            _method = method;
            _params = parameter;
        }

        public static Traverse Create(Type type)
        {
            return new Traverse(type);
        }

        public static Traverse Create<T>()
        {
            return Create(typeof(T));
        }

        public static Traverse Create(object root)
        {
            return new Traverse(root);
        }

        public static Traverse CreateWithType(string name)
        {
            return new Traverse(AccessTools.TypeByName(name));
        }

        public object GetValue()
        {
            if (_info is FieldInfo) return ((FieldInfo) _info).GetValue(_root);
            if (_info is PropertyInfo)
                return ((PropertyInfo) _info).GetValue(_root, AccessTools.all, null, _params,
                    CultureInfo.CurrentCulture);
            if (_method != null) return _method.Invoke(_root, _params);
            if (_root == null && _type != null) return _type;
            return _root;
        }

        public T GetValue<T>()
        {
            var value = GetValue();
            if (value == null) return default;
            return (T) value;
        }

        public object GetValue(params object[] arguments)
        {
            if (_method == null) throw new Exception("cannot get method value without method");
            return _method.Invoke(_root, arguments);
        }

        public T GetValue<T>(params object[] arguments)
        {
            if (_method == null) throw new Exception("cannot get method value without method");
            return (T) _method.Invoke(_root, arguments);
        }

        public Traverse SetValue(object value)
        {
            if (_info is FieldInfo)
                ((FieldInfo) _info).SetValue(_root, value, AccessTools.all, null, CultureInfo.CurrentCulture);
            if (_info is PropertyInfo)
                ((PropertyInfo) _info).SetValue(_root, value, AccessTools.all, null, _params,
                    CultureInfo.CurrentCulture);
            if (_method != null) throw new Exception("cannot set value of method " + _method.FullDescription());
            return this;
        }

        private Traverse Resolve()
        {
            if (_root == null && _type != null) return this;
            return new Traverse(GetValue());
        }

        public Traverse Type(string name)
        {
            if (name == null) throw new ArgumentNullException("name cannot be null");
            if (_type == null) return new Traverse();
            var type = AccessTools.Inner(_type, name);
            if (type == null) return new Traverse();
            return new Traverse(type);
        }

        public Traverse Field(string name)
        {
            if (name == null) throw new ArgumentNullException("name cannot be null");
            var traverse = Resolve();
            if (traverse._type == null) return new Traverse();
            var fieldInfo = Cache.GetFieldInfo(traverse._type, name);
            if (fieldInfo == null) return new Traverse();
            if (!fieldInfo.IsStatic && traverse._root == null) return new Traverse();
            return new Traverse(traverse._root, fieldInfo, null);
        }

        public List<string> Fields()
        {
            return AccessTools.GetFieldNames(Resolve()._type);
        }

        public Traverse Property(string name, object[] index = null)
        {
            if (name == null) throw new ArgumentNullException("name cannot be null");
            var traverse = Resolve();
            if (traverse._root == null || traverse._type == null) return new Traverse();
            var propertyInfo = Cache.GetPropertyInfo(traverse._type, name);
            if (propertyInfo == null) return new Traverse();
            return new Traverse(traverse._root, propertyInfo, index);
        }

        public List<string> Properties()
        {
            return AccessTools.GetPropertyNames(Resolve()._type);
        }

        public Traverse Method(string name, params object[] arguments)
        {
            if (name == null) throw new ArgumentNullException("name cannot be null");
            var traverse = Resolve();
            if (traverse._type == null) return new Traverse();
            var types = AccessTools.GetTypes(arguments);
            var methodInfo = Cache.GetMethodInfo(traverse._type, name, types);
            if (methodInfo == null) return new Traverse();
            return new Traverse(traverse._root, (MethodInfo) methodInfo, arguments);
        }

        public Traverse Method(string name, Type[] paramTypes, object[] arguments = null)
        {
            if (name == null) throw new ArgumentNullException("name cannot be null");
            var traverse = Resolve();
            if (traverse._type == null) return new Traverse();
            var methodInfo = Cache.GetMethodInfo(traverse._type, name, paramTypes);
            if (methodInfo == null) return new Traverse();
            return new Traverse(traverse._root, (MethodInfo) methodInfo, arguments);
        }

        public List<string> Methods()
        {
            return AccessTools.GetMethodNames(Resolve()._type);
        }

        public bool FieldExists()
        {
            return _info != null;
        }

        public bool MethodExists()
        {
            return _method != null;
        }

        public bool TypeExists()
        {
            return _type != null;
        }

        public static void IterateFields(object source, Action<Traverse> action)
        {
            var sourceTrv = Create(source);
            AccessTools.GetFieldNames(source).ForEach(delegate(string f) { action(sourceTrv.Field(f)); });
        }

        public static void IterateFields(object source, object target, Action<Traverse, Traverse> action)
        {
            var sourceTrv = Create(source);
            var targetTrv = Create(target);
            AccessTools.GetFieldNames(source).ForEach(delegate(string f)
            {
                action(sourceTrv.Field(f), targetTrv.Field(f));
            });
        }

        public static void IterateFields(object source, object target, Action<string, Traverse, Traverse> action)
        {
            var sourceTrv = Create(source);
            var targetTrv = Create(target);
            AccessTools.GetFieldNames(source).ForEach(delegate(string f)
            {
                action(f, sourceTrv.Field(f), targetTrv.Field(f));
            });
        }

        public static void IterateProperties(object source, Action<Traverse> action)
        {
            var sourceTrv = Create(source);
            AccessTools.GetPropertyNames(source).ForEach(delegate(string f) { action(sourceTrv.Property(f)); });
        }

        public static void IterateProperties(object source, object target, Action<Traverse, Traverse> action)
        {
            var sourceTrv = Create(source);
            var targetTrv = Create(target);
            AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
            {
                action(sourceTrv.Property(f), targetTrv.Property(f));
            });
        }

        public static void IterateProperties(object source, object target, Action<string, Traverse, Traverse> action)
        {
            var sourceTrv = Create(source);
            var targetTrv = Create(target);
            AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
            {
                action(f, sourceTrv.Property(f), targetTrv.Property(f));
            });
        }

        public override string ToString()
        {
            var methodBase = _method ?? GetValue();
            return methodBase?.ToString();
        }
    }
}