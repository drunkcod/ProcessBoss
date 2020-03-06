using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessBoss.JsonRpc
{
	[JsonConverter(typeof(RequestIdJsonConverter))]
	public struct RequestId : IEquatable<RequestId>
	{
		static readonly string NumberMarker = new string(new[]{ '\0' });

		readonly long number;
		readonly string str;

		public bool IsMissing => number == 0 && ReferenceEquals(null, str);

		public bool IsNumber => ReferenceEquals(NumberMarker, str);

		public long Number => IsNumber ? number : 0;

		public string String => IsNumber ? null : str; 

		public RequestId(long id) 
		{
			this.number = id;
			this.str = NumberMarker;
		}

		public RequestId(string value) {
			this.str = value;
			this.number = value?.GetHashCode() ?? -1;
		}

		public override int GetHashCode() => Number.GetHashCode();

		public override bool Equals(object obj) => obj is RequestId other && Equals(other);

		public override string ToString() => JsonSerializer.Serialize(this);

		public bool Equals(RequestId other) =>
			(other.IsNumber && this.IsNumber && other.number == this.number)
			|| other.str == this.str;

		public static bool operator==(RequestId x, RequestId y) => x.Equals(y);
		public static bool operator!=(RequestId x, RequestId y) => !x.Equals(y);
	}

	class RequestIdJsonConverter : JsonConverter<RequestId>
	{
		public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			reader.TokenType switch
			{
				JsonTokenType.Number => new RequestId(reader.GetInt64()),
				JsonTokenType.String => new RequestId(reader.GetString()),
				JsonTokenType.Null => default,
				_ => throw new InvalidOperationException(),
			};

		public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options) {
			if (value.IsNumber)
				writer.WriteNumberValue(value.Number);
			else writer.WriteStringValue(value.String);
		}
	}
}
