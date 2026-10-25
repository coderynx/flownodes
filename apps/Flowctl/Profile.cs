using System.Text.Json;

namespace Flowctl;

internal sealed class Profile
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string? Server { get; set; }
    public string? AccessToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public string? PersonalAccessToken { get; set; }

    public static Profile Load(string path) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<Profile>(File.ReadAllText(path), Json) ?? new Profile()
            : new Profile();

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
