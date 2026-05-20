namespace HoscyCore.Services.Media;

public class MediaUpdateInfo
{
    public bool? Playing { get; set; }
    public MediaUpdateInfoTrack? Track { get; set; }

    public override string ToString()
    {
        return $"Playing: {Playing?.ToString() ?? "NULL"}, Track: {Track?.ToString() ?? "NULL"}";
    }
}

public class MediaUpdateInfoTrack
{
    public string[] Artists { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"\"{Title}\" by {string.Join(", ", Artists.Select(x => $"\"{x}\""))} on \"{Album}\"";
    }
}