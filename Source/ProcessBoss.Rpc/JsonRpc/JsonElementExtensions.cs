using System;
using System.Buffers;
using System.Text.Json;

namespace ProcessBoss.JsonRpc
{
	static class JsonElementExtensions
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

		public static T ToObject<T>(in this JsonElement json, JsonSerializerOptions options = null) {
			var bytes = new ArrayBufferWriter<byte>();
			using(var writer = new Utf8JsonWriter(bytes))
				json.WriteTo(writer);

			return JsonSerializer.Deserialize<T>(bytes.AsReadonlySpan(), options);
		}
	}
}
