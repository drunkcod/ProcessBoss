using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessBoss.JsonRpc;

namespace ProcessBoss.Rpc
{
	[JsonConverter(typeof(RequestIdJsonConverter))]
	public struct RequestId : IEquatable<RequestId>
	{
		static readonly string NumberMarker = new string(new[]{ '\0' });

		public static RequestId Null => new RequestId(null);

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
			other.number == this.number
			&& (other.IsNumber && this.IsNumber || other.str == this.str);

		public static bool operator==(RequestId x, RequestId y) => x.Equals(y);
		public static bool operator!=(RequestId x, RequestId y) => !x.Equals(y);

		public static implicit operator RequestId(long value) => new RequestId(value);
		public static implicit operator RequestId(string value) => new RequestId(value);
	}

	public class RequestIdJsonConverter : JsonRpcConverter<RequestId>
	{
		public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			ReadRequestId(ref reader);

		public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options) => 
			WriteRequestId(writer, value);
	}
}
