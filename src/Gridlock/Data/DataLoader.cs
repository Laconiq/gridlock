using System.IO;
using System.Text.Json;

namespace Gridlock.Data
{
    public static class DataLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static T Load<T>(string path) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)!;

        public static T LoadFromDirectory<T>(string directory, string fileName) =>
            Load<T>(Path.Combine(directory, fileName));
    }
}
