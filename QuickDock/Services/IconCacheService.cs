using System.IO;
using System.Net.Http;

namespace QuickDock.Services;

public class IconCacheService
{
    private static IconCacheService? _instance;
    private static readonly object _lock = new();
    
    private readonly string _cacheDir;
    private readonly HttpClient _httpClient;
    
    public static IconCacheService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new IconCacheService();
                }
            }
            return _instance;
        }
    }
    
    private IconCacheService()
    {
        _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconCache");
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }
    
    public string? GetCachedIconPath(string url)
    {
        var cacheKey = GetCacheKey(url);
        var cachedPath = Path.Combine(_cacheDir, cacheKey);
        
        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }
        
        return null;
    }
    
    public async Task<string?> DownloadIconAsync(string targetUrl)
    {
        try
        {
            var cacheKey = GetCacheKey(targetUrl);
            var cachedPath = Path.Combine(_cacheDir, cacheKey);
            
            if (File.Exists(cachedPath))
            {
                return cachedPath;
            }
            
            var apiUrl = $"https://icon.bqb.cool?url={Uri.EscapeDataString(targetUrl)}";
            
            var imageData = await _httpClient.GetByteArrayAsync(apiUrl);
            
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }
            
            await File.WriteAllBytesAsync(cachedPath, imageData);
            
            return cachedPath;
        }
        catch
        {
            return null;
        }
    }
    
    public string? CacheLocalIcon(string localPath)
    {
        try
        {
            if (!File.Exists(localPath))
            {
                return null;
            }
            
            var extension = Path.GetExtension(localPath);
            var cacheKey = $"local_{Guid.NewGuid():N}{extension}";
            var cachedPath = Path.Combine(_cacheDir, cacheKey);
            
            File.Copy(localPath, cachedPath, true);
            
            return cachedPath;
        }
        catch
        {
            return null;
        }
    }
    
    private string GetCacheKey(string url)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url));
        return $"{Convert.ToHexString(hash)[..16]}.png";
    }
    
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                foreach (var file in Directory.GetFiles(_cacheDir))
                {
                    File.Delete(file);
                }
            }
        }
        catch { }
    }
}
