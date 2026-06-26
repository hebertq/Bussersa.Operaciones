using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Utilidades.ClasesGenericas
{
    public static class MapperLF
    {
        public static TDestination ConvertToDBEntity<TDestination, TSource>(TSource model) where TDestination : new()
        {
            var mappers     = new ObjectCopyBase[]{new MapperUnoptimized() };
            var sourceType  = model.GetType();
            var targetType  = new TDestination().GetType();
            var destination = new TDestination();

            foreach (var mapper in mappers)
            {
                mapper.MapTypes(sourceType, targetType);
                mapper.Copy(model, destination);
            }

            return destination;
        }
    }

    public class PropertyMap
    {
        public PropertyInfo SourceProperty { get; set; }
        public PropertyInfo TargetProperty { get; set; }
    }

    public static class ObjectExtensions
    {
        public static IList<PropertyMap> GetMatchingProps(this object source, object target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var sourceProperties = source.GetType().GetProperties();
            var targetProperties = target.GetType().GetProperties();

            var properties = (from s in sourceProperties
                              from t in targetProperties
                              where s.Name == t.Name &&
                                  s.CanRead &&
                                  t.CanWrite &&
                                  s.PropertyType.IsPublic &&
                                  t.PropertyType.IsPublic &&
                                  s.PropertyType == t.PropertyType &&
                                  (
                                      (s.PropertyType.IsValueType &&
                                      t.PropertyType.IsValueType
                                      ) ||
                                      (s.PropertyType == typeof(string) &&
                                      t.PropertyType == typeof(string)
                                      )
                                  )
                              select new PropertyMap
                              {
                                  SourceProperty = s,
                                  TargetProperty = t
                              }).ToList();

            return properties;
        }
    }

    public abstract class ObjectCopyBase
    {
        public abstract void MapTypes(Type source, Type target);
        public abstract void Copy(object source, object target);

        protected virtual IList<PropertyMap> GetMatchingProperties
            (object sourceType, object targetType)
        {
            var sourceProperties1 = sourceType.GetType().BaseType.GetProperties();
            var targetProperties1 = targetType.GetType().BaseType.GetProperties();

            var lista = ObjectExtensions.GetMatchingProps(sourceType, targetType);

            var properties = new List<PropertyMap>();
            foreach (PropertyInfo sourceProperties in sourceType.GetType().BaseType.GetProperties())
            {
                string nameIn = sourceProperties.Name;               
                foreach (PropertyInfo targetProperties in targetType.GetType().BaseType.GetProperties())
                {
                    string nameInO = targetProperties.Name;
                    //object valueInO = propertyInfoO.GetValue(Out, null);
                    if (nameIn == nameInO)
                        properties.Add(new PropertyMap
                        {
                            SourceProperty = sourceProperties,
                            TargetProperty = targetProperties
                        });
                }
            }


            return properties;
        }

        protected virtual string GetMapKey(Type sourceType, Type targetType)
        {
            var keyName = "Copy_";
            keyName += sourceType.FullName.Replace(".", "_").Replace("+", "_");
            keyName += "_";
            keyName += targetType.FullName.Replace(".", "_").Replace("+", "_");

            return keyName;
        }
    }

    public class MapperUnoptimized : ObjectCopyBase
    {
        public override void MapTypes(Type source, Type target)
        {
        }

        public override void Copy(object source, object target)
        {
            var propMap = GetMatchingProperties(source, target);

            for (var i = 0; i < propMap.Count; i++)
            {
                var prop = propMap[i];
                var sourceValue = prop.SourceProperty.GetValue(source, null);
                prop.TargetProperty.SetValue(target, sourceValue, null);
            }
        }
    }

    public class MapperOptimized : ObjectCopyBase
    {
        private readonly Dictionary<string, PropertyMap[]> _maps = new Dictionary<string, PropertyMap[]>();

        public override void MapTypes(Type source, Type target)
        {
            var key = GetMapKey(source, target);
            if (_maps.ContainsKey(key))
            {
                return;
            }

            var props = GetMatchingProperties(source, target);
            _maps.Add(key, props.ToArray());
        }

        public override void Copy(object source, object target)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();

            var key = GetMapKey(sourceType, targetType);
            if (!_maps.ContainsKey(key))
            {
                MapTypes(sourceType, targetType);
            }

            var propMap = _maps[key];

            for (var i = 0; i < propMap.Length; i++)
            {
                var prop = propMap[i];
                var sourceValue = prop.SourceProperty.GetValue(source, null);
                prop.TargetProperty.SetValue(target, sourceValue, null);
            }
        }
    }

    public class MapperDynamicCode : ObjectCopyBase
    {
        private readonly Dictionary<string, Type> _comp = new Dictionary<string, Type>();

        public override void MapTypes(Type source, Type target)
        {
            var key = GetMapKey(source, target);
            if (_comp.ContainsKey(key))
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("using ObjectToObjectMapper;\r\n");
            builder.Append("namespace Copy {\r\n");
            builder.Append("    public class ");
            builder.Append(key);
            builder.Append(" {\r\n");
            builder.Append("        public static void CopyProps(");
            builder.Append(target.FullName.Replace("+", "."));
            builder.Append(" source, ");
            builder.Append(target.FullName.Replace("+", "."));
            builder.Append(" target) {\r\n");

            var map = GetMatchingProperties(source, target);
            foreach (var item in map)
            {
                builder.Append("            target.");
                builder.Append(item.TargetProperty.Name);
                builder.Append(" = ");
                builder.Append("source.");
                builder.Append(item.SourceProperty.Name);
                builder.Append(";\r\n");
            }

            builder.Append("        }\r\n   }\r\n}");

            var syntaxTree = CSharpSyntaxTree.ParseText(builder.ToString());

            string assemblyName = Path.GetRandomFileName();
            var refPaths = new[] {
            typeof(System.Object).GetTypeInfo().Assembly.Location,
            typeof(Console).GetTypeInfo().Assembly.Location,
            Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
            this.GetType().GetTypeInfo().Assembly.Location
        };

            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                ms.Seek(0, SeekOrigin.Begin);

                Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                var type = assembly.GetType("Copy." + key);
                _comp.Add(key, type);
            }
        }

        public override void Copy(object source, object target)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();

            var key = GetMapKey(sourceType, targetType);
            if (!_comp.ContainsKey(key))
            {
                MapTypes(sourceType, targetType);
            }

            var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod;
            var args = new[] { source, target };
            _comp[key].InvokeMember("CopyProps", flags, null, null, args);
        }
    }

}
