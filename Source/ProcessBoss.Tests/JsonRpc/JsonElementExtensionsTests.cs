using System;
using System.Text.Json;
using CheckThat;
using Xunit;

namespace ProcessBoss.JsonRpc.Tests
{
	public class JsonElementExtensionsTests
	{
		[Theory]
		[InlineData(16, typeof(short))]
		[InlineData(32, typeof(int))]
		[InlineData(64, typeof(long))]
		[InlineData(1.0, typeof(float))]
		[InlineData(2.0, typeof(double))]
		[InlineData(3.0, typeof(decimal))]
		[InlineData(true, typeof(bool))]
		[InlineData(255, typeof(byte))]
		[InlineData("2020-03-13 01:02:03", typeof(DateTime))]
		[InlineData("Hello World!", typeof(string))]
		public void GetValue(object value, Type type) {
			var json = JsonDocument.Parse(JsonSerializer.Serialize(new { value = Convert.ChangeType(value, type) } ));
			Check.That(() => json.RootElement.GetProperty("value").GetValue(type) == Convert.ChangeType(value, type));

		}
	}
}
