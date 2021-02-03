using System;
using System.Buffers;
using System.Text.Json;

namespace ProcessBoss.JsonRpc
{
	public static class JsonElementExtensions
	{
		class ArrayBufferWriter<T> : IBufferWriter<T>
		{
			const int MinBuffer = 256;
			T[] buffer = new T[MinBuffer];
			int pos;

			public void Advance(int count) {
				pos += count;
			}

			public Memory<T> GetMemory(int sizeHint = 0) {
				EnsureSpace(sizeHint);
				return new Memory<T>(buffer, pos, sizeHint);
			}

			public Span<T> GetSpan(int sizeHint = 0) {
				EnsureSpace(sizeHint);
				return new Span<T>(buffer, pos, sizeHint);
			}

			void EnsureSpace(int sizeHint) {
				if(buffer.Length - pos < sizeHint)
					Array.Resize(ref buffer, buffer.Length + sizeHint);
			}

			public ReadOnlySpan<T> AsReadonlySpan() => new ReadOnlySpan<T>(buffer, 0, pos);
		}

		public static object GetValue(this JsonElement json, Type type) {
			switch(Type.GetTypeCode(type)) {
				case TypeCode.Boolean: return json.GetBoolean();
				case TypeCode.SByte: return json.GetSByte();
				case TypeCode.Int16: return json.GetInt16();
				case TypeCode.Int32: return json.GetInt32();
				case TypeCode.Int64: return json.GetInt64();
				case TypeCode.Byte: return json.GetByte();
				case TypeCode.UInt16: return json.GetUInt16();
				case TypeCode.UInt32: return json.GetUInt32();
				case TypeCode.UInt64: return json.GetUInt64();
				case TypeCode.Single: return json.GetSingle();
				case TypeCode.Double: return json.GetDouble();
				case TypeCode.Decimal: return json.GetDecimal();
				case TypeCode.String: return json.GetString();

				case TypeCode.DateTime: return json.GetDateTime();
			}
			return Convert.ChangeType(json.GetRawText(), type);
		}

		public static T ToObject<T>(in this JsonElement json, JsonSerializerOptions options = null) {
			var bytes = new ArrayBufferWriter<byte>();
			using(var writer = new Utf8JsonWriter(bytes))
				json.WriteTo(writer);

			return JsonSerializer.Deserialize<T>(bytes.AsReadonlySpan(), options);
		}
	}
}
