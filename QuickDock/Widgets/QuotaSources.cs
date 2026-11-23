using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace QuickDock.Widgets;

internal enum QuotaPeriod
{
    Short,
    Weekly
}

internal sealed record QuotaWindow(QuotaPeriod Period, string Label, double UsedPercent, DateTimeOffset? ResetsAt)
{
    public double Remaining => Math.Clamp(100 - UsedPercent, 0, 100);
}

internal static class QuotaSources
{
    public static async Task<IReadOnlyList<QuotaWindow>> ReadCodexAsync(CancellationToken cancellationToken)
    {
        var start = FindCodex();
        if (start == null) return [];

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        var token = timeout.Token;
        start.ArgumentList.Add("app-server");
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        using var process = Process.Start(start);
        if (process == null) return [];
        process.ErrorDataReceived += (_, _) => { };
        process.BeginErrorReadLine();
        try
        {
            var version = typeof(QuotaSources).Assembly.GetName().Version!.ToString(3);
            await SendAsync(process, new { id = 1, method = "initialize", @params = new { clientInfo = new { name = "quickdock", title = "QuickDock", version } } }, token);
            if (await ReadResultAsync(process, 1, token) == null) return [];
            await SendAsync(process, new { method = "initialized" }, token);
            await SendAsync(process, new { id = 2, method = "account/rateLimits/read" }, token);
            if (await ReadResultAsync(process, 2, token) is not { } result) return [];

            var limits = Property(result, "rateLimits");
            var windows = ReadCodexWindows(limits);
            return windows.Count != 0 ? windows :
                ReadCodexWindows(Property(Property(result, "rateLimitsByLimitId"), "codex"));
        }
        finally
        {
            process.StandardInput.Close();
            try
            {
                using var exitTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                await process.WaitForExitAsync(exitTimeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
            }
        }
    }

    private static async Task SendAsync(Process process, object request, CancellationToken token)
    {
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), token);
        await process.StandardInput.FlushAsync(token);
    }

    private static async Task<JsonElement?> ReadResultAsync(Process process, int id, CancellationToken token)
    {
        while (await process.StandardOutput.ReadLineAsync(token) is { } line)
        {
            if (!line.StartsWith('{')) continue;
            using var message = JsonDocument.Parse(line);
            var root = message.RootElement;
            var messageId = Property(root, "id");
            if (messageId.ValueKind != JsonValueKind.Number || !messageId.TryGetInt32(out var received) || received != id)
                continue;

            var result = Property(root, "result");
            return result.ValueKind == JsonValueKind.Object ? result.Clone() : null;
        }
        return null;
    }

    private static List<QuotaWindow> ReadCodexWindows(JsonElement limits)
    {
        var windows = new List<QuotaWindow>();
        foreach (var (key, fallback, period) in new[]
        {
            ("primary", "5h", QuotaPeriod.Short),
            ("secondary", "7d", QuotaPeriod.Weekly)
        })
        {
            var window = Property(limits, key);
            var duration = Property(window, "windowDurationMins");
            var label = duration.ValueKind == JsonValueKind.Number && duration.TryGetInt32(out var minutes)
                ? FormatDuration(minutes) : fallback;
            if (ReadWindow(window, period, label, "usedPercent", "resetsAt") is { } parsed)
                windows.Add(parsed);
        }
        return windows;
    }

    public static async Task<IReadOnlyList<QuotaWindow>> ReadClaudeAsync(CancellationToken token)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuickDock", "claude-quota.json");
        if (!File.Exists(path)) return [];

        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var document = await JsonDocument.ParseAsync(file, cancellationToken: token);
        var limits = document.RootElement;
        var windows = new List<QuotaWindow>();
        foreach (var (key, label, period) in new[]
        {
            ("five_hour", "5h", QuotaPeriod.Short),
            ("seven_day", "7d", QuotaPeriod.Weekly)
        })
        {
            if (ReadWindow(Property(limits, key), period, label, "used_percentage", "resets_at") is { } window &&
                window.ResetsAt > DateTimeOffset.UtcNow)
                windows.Add(window);
        }
        return windows;
    }

    private static QuotaWindow? ReadWindow(JsonElement window, QuotaPeriod period, string label, string usedKey, string resetKey)
    {
        var usage = Property(window, usedKey);
        if (usage.ValueKind != JsonValueKind.Number || !usage.TryGetDouble(out var used) || !double.IsFinite(used))
            return null;

        DateTimeOffset? reset = null;
        var resetsAt = Property(window, resetKey);
        if (resetsAt.ValueKind == JsonValueKind.Number && resetsAt.TryGetInt64(out var timestamp) &&
            timestamp is > 0 and <= 253402300799)
            reset = DateTimeOffset.FromUnixTimeSeconds(timestamp);

        return new QuotaWindow(period, label, used, reset);
    }

    private static JsonElement Property(JsonElement source, string name) =>
        source.ValueKind == JsonValueKind.Object && source.TryGetProperty(name, out var value) ? value : default;

    private static string FormatDuration(int minutes)
    {
        if (minutes > 0 && minutes % 1440 == 0) return $"{minutes / 1440}d";
        if (minutes > 0 && minutes % 60 == 0) return $"{minutes / 60}h";
        return $"{minutes}m";
    }

    private static ProcessStartInfo? FindCodex()
    {
        var configured = Environment.GetEnvironmentVariable("QUICKDOCK_CODEX_PATH");
        if (IsExecutablePath(configured)) return new ProcessStartInfo(configured!);

        var folders = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Select(folder => folder.Trim().Trim('"')).Where(Path.IsPathFullyQualified).ToArray();
        var executable = folders.Select(folder => Path.Combine(folder, "codex.exe")).FirstOrDefault(IsExecutablePath);
        if (executable != null) return new ProcessStartInfo(executable);

        var node = folders.Select(folder => Path.Combine(folder, "node.exe")).FirstOrDefault(IsExecutablePath);
        var npmEntry = folders.Select(folder => Path.Combine(folder, "node_modules", "@openai", "codex", "bin", "codex.js"))
            .FirstOrDefault(File.Exists);
        if (node != null && npmEntry != null)
        {
            var start = new ProcessStartInfo(node);
            start.ArgumentList.Add(npmEntry);
            return start;
        }

        var desktopBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
        if (!Directory.Exists(desktopBin)) return null;
        executable = new DirectoryInfo(desktopBin).EnumerateDirectories().OrderByDescending(folder => folder.LastWriteTimeUtc)
            .Select(folder => Path.Combine(folder.FullName, "codex.exe")).FirstOrDefault(IsExecutablePath);
        return executable == null ? null : new ProcessStartInfo(executable);
    }

    private static bool IsExecutablePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) &&
        Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(path);
}
