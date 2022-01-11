using System;
using System.Reflection;
using System.Text.Json;
using CheckThat;
using Xunit;

namespace ProcessBoss.JsonRpc.Tests
{
	public class JsonRpcMethodBindingTests : JsonRpcFixture<JsonRpcRequest>
	{
		MethodInfo ArrayFromItemsMethod = typeof(JsonRpcMethodBindingTests).GetMethod(nameof(ArrayFromItems), BindingFlags.Static | BindingFlags.NonPublic);

		[Fact]
		public void positional_parameter_matching() {
			var r = RoundtripAs<JsonRpcRequest>(new { @params = ArrayFromItems(7, "Hello World.") });

			object[] bound = null;
			Check.That(() => TryBindParameters(ArrayFromItemsMethod, r, out bound));
			Check.That(
				() => bound[0] == (object)7,
				() => (string)bound[1] == "Hello World.");
		}

		[Fact]
		public void named_arguments() {
			var r = RoundtripAs<JsonRpcRequest>(new { @params = new
			{
				second = "The Answer Is",
				first = 42,
			} });

			object[] bound = null;
			Check.That(() => TryBindParameters(ArrayFromItemsMethod, r, out bound));
			Check.That(
				() => bound[0] == (object)42,
				() => (string)bound[1] == "The Answer Is");
		}

		static bool TryBindParameters(MethodInfo method, JsonRpcRequest request, out object[] bound) {
			var ps = method?.GetParameters() ?? throw new ArgumentNullException(nameof(method));
			if(request.HasPositionalParameters) {
				var xs = (JsonElement)request.Parameters;
				bound = new object[xs.GetArrayLength()];
				var n = 0;
				foreach(var item in xs.EnumerateArray()) {
					bound[n] = item.GetValue(ps[n].ParameterType);
					++n;
				}
				return true;
			} else {
				bound = new object[ps.Length];
				var xs = (JsonElement)request.Parameters;
				foreach (var item in xs.EnumerateObject()) {
					var key = item.Name;
					var n = Array.FindIndex(ps, x => x.Name == key);
					bound[n] = item.Value.GetValue(ps[n].ParameterType);
				}
				return true;
			}
		}

		static object[] ArrayFromItems(int first, string second) => new object[]{ first, second };
	}
}
