using System.Text.Json;

namespace BlueprintAPI.Policies {
    public class JsonLowercaseNamingPolicy : JsonNamingPolicy {
        public override string ConvertName(string name) {
            return CamelCase.ConvertName(name).ToLowerInvariant();
        }
    }
}
