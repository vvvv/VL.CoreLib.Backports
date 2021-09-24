using MBrace.FsPickler;
using MBrace.FsPickler.Json;
using Microsoft.FSharp.Core;
using System;
using System.Runtime.CompilerServices;
using VL.Core;

namespace VL.Lib.Backports
{
    /// <summary>
    /// Provides serializers which are compatible with 2021.4
    /// </summary>
    public static class Serialization
    {
        static readonly ConditionalWeakTable<IVLFactory, TypeNameConverter> typeNameConverters = new ConditionalWeakTable<IVLFactory, TypeNameConverter>();

        private static FSharpOption<ITypeNameConverter> GetTypeNameConverter(NodeContext nodeContext)
        {
            if (nodeContext is null)
                return FSharpOption<ITypeNameConverter>.None;

            return typeNameConverters.GetValue(nodeContext.Factory, f => new TypeNameConverter(f));
        }

        public static string SerializeXml<T>(T value, bool indent = false, NodeContext nodeContext = default)
        {
            var serializer = new XmlSerializer(indent, GetTypeNameConverter(nodeContext));
            return serializer.PickleToString(value);
        }

        public static T DeserializeXml<T>(string serializedValue, bool indent = false, NodeContext nodeContext = default)
        {
            var serializer = new XmlSerializer(indent, GetTypeNameConverter(nodeContext));
            return serializer.UnPickleOfString<T>(serializedValue);
        }

        public static string SerializeJson<T>(T value, bool indent = false, bool omitHeader = true, NodeContext nodeContext = default)
        {
            var serializer = new JsonSerializer(indent, omitHeader, GetTypeNameConverter(nodeContext));
            return serializer.PickleToString(value);
        }

        public static T DeserializeJson<T>(string serializedValue, bool indent = false, bool omitHeader = true, NodeContext nodeContext = default)
        {
            var serializer = new JsonSerializer(indent, omitHeader, GetTypeNameConverter(nodeContext));
            return serializer.UnPickleOfString<T>(serializedValue);
        }

        [Obsolete("BSON format has been deprecated by Newtonsoft")]
        public static byte[] SerializeBson<T>(T value, NodeContext nodeContext = default)
        {
            var serializer = new BsonSerializer(GetTypeNameConverter(nodeContext));
            return serializer.Pickle(value);
        }

        [Obsolete("BSON format has been deprecated by Newtonsoft")]
        public static T DeserializeBson<T>(byte[] serializedValue, NodeContext nodeContext = default)
        {
            var serializer = new BsonSerializer(GetTypeNameConverter(nodeContext));
            return serializer.UnPickle<T>(serializedValue);
        }

        public static byte[] SerializeBinary<T>(T value, bool forceLittleEndian = false, NodeContext nodeContext = default)
        {
            var serializer = new BinarySerializer(forceLittleEndian, GetTypeNameConverter(nodeContext));
            return serializer.Pickle(value);
        }

        public static T DeserializeBinary<T>(byte[] serializedValue, bool forceLittleEndian = false, NodeContext nodeContext = default)
        {
            var serializer = new BinarySerializer(forceLittleEndian, GetTypeNameConverter(nodeContext));
            return serializer.UnPickle<T>(serializedValue);
        }

        sealed class TypeNameConverter : ITypeNameConverter
        {
            private readonly IVLFactory factory;

            public TypeNameConverter(IVLFactory factory)
            {
                this.factory = factory;
            }

            public TypeInfo OfSerializedType(TypeInfo value)
            {
                var name = value.Name;
                foreach (var typeInfo in factory.RegisteredTypes)
                {
                    if (typeInfo.ClrType.FullName == name)
                        return new TypeInfo(ToFullName(typeInfo), assemblyInfo: value.AssemblyInfo);
                }
                return value;
            }

            public TypeInfo ToDeserializedType(TypeInfo value)
            {
                foreach (var typeInfo in factory.RegisteredTypes)
                {
                    if (ToFullName(typeInfo) == value.Name)
                        return new TypeInfo(typeInfo.ClrType.FullName, AssemblyInfo.OfAssembly(typeInfo.ClrType.Assembly));
                }
                return value;
            }

            static string ToFullName(IVLTypeInfo typeInfo)
            {
                if (string.IsNullOrWhiteSpace(typeInfo.Category))
                    return typeInfo.Name;
                return $"{typeInfo.Category}.{typeInfo.Name}";
            }
        }
    }
}
