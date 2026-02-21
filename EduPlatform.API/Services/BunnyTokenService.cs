using Newtonsoft.Json;
//using System.Security.Cryptographics;
using System.Security.Cryptography;
using System.Text;

public class BunnyTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<BunnyTokenService> _logger;

    public BunnyTokenService(HttpClient httpClient, IConfiguration config, ILogger<BunnyTokenService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    // ==============================
    // رفع الفيديو إلى Bunny Stream
    // ==============================
    public async Task<string> UploadVideoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("الملف غير صالح");

        var libraryId = _config["Bunny:LibraryId"];
        var apiKey = _config["Bunny:ApiKey"];

        if (string.IsNullOrEmpty(libraryId) || string.IsNullOrEmpty(apiKey))
            throw new Exception("Bunny CDN الإعدادات غير مكتملة");

        try
        {
            // 1️⃣ إنشاء فيديو جديد
            var createRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://video.bunnycdn.com/library/600042/videos"
            );

            createRequest.Headers.Add("AccessKey", apiKey);

            var jsonContent = JsonConvert.SerializeObject(new
            {
                title = file.FileName
            });

            createRequest.Content = new StringContent(
                jsonContent,
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation($"Creating video in Bunny: {file.FileName}");
            var createResponse = await _httpClient.SendAsync(createRequest);

            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Create Video Failed: {error}");
                throw new Exception($"فشل إنشاء الفيديو: {error}");
            }

            var videoData = await createResponse.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(videoData);
            string videoId = result.guid;

            // 2️⃣ رفع ملف الفيديو
            using var stream = file.OpenReadStream();

            var uploadRequest = new HttpRequestMessage(
                HttpMethod.Put,
                $"https://video.bunnycdn.com/library/600042/videos/{videoId}"
            );

            uploadRequest.Headers.Add("AccessKey", apiKey);
            uploadRequest.Content = new StreamContent(stream);

            _logger.LogInformation($"Uploading video to Bunny: {file.FileName}, Size: {file.Length} bytes");
            var uploadResponse = await _httpClient.SendAsync(uploadRequest);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var error = await uploadResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Upload Failed: {error}");
                throw new Exception($"فشل رفع الفيديو: {error}");
            }

            _logger.LogInformation($"Video uploaded successfully: {videoId}");
            return videoId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UploadVideoAsync");
            throw;
        }
    }

    // ==============================
    // حذف فيديو من Bunny Stream
    // ==============================
    public async Task<bool> DeleteVideoAsync(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return false;

        var libraryId = _config["Bunny:LibraryId"];
        var apiKey = _config["Bunny:ApiKey"];

        if (string.IsNullOrEmpty(libraryId) || string.IsNullOrEmpty(apiKey))
            return false;

        try
        {
            var deleteRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                $"https://video.bunnycdn.com/library/600042/videos/{videoId}"
            );

            deleteRequest.Headers.Add("AccessKey", apiKey);

            _logger.LogInformation($"Deleting video from Bunny: {videoId}");
            var deleteResponse = await _httpClient.SendAsync(deleteRequest);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                var error = await deleteResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Delete Failed: {error}");
                return false;
            }

            _logger.LogInformation($"Video deleted successfully: {videoId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting video {videoId}");
            return false;
        }
    }

    // ==============================
    // تحديث عنوان الفيديو
    // ==============================
    public async Task<bool> UpdateVideoTitleAsync(string videoId, string newTitle)
    {
        if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(newTitle))
            return false;

        var libraryId = _config["Bunny:LibraryId"];
        var apiKey = _config["Bunny:ApiKey"];

        if (string.IsNullOrEmpty(libraryId) || string.IsNullOrEmpty(apiKey))
            return false;

        try
        {
            var updateRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://video.bunnycdn.com/library/600042/videos/{videoId}"
            );

            updateRequest.Headers.Add("AccessKey", apiKey);

            var jsonContent = JsonConvert.SerializeObject(new
            {
                title = newTitle
            });

            updateRequest.Content = new StringContent(
                jsonContent,
                Encoding.UTF8,
                "application/json"
            );

            var updateResponse = await _httpClient.SendAsync(updateRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var error = await updateResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Update Title Failed: {error}");
                return false;
            }

            _logger.LogInformation($"Video title updated: {videoId} -> {newTitle}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating video title {videoId}");
            return false;
        }
    }

    // ===================================
    // الحصول على معلومات الفيديو
    // ===================================
    public async Task<object> GetVideoInfoAsync(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return null;

        var libraryId = _config["Bunny:LibraryId"];
        var apiKey = _config["Bunny:ApiKey"];

        if (string.IsNullOrEmpty(libraryId) || string.IsNullOrEmpty(apiKey))
            return null;

        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://video.bunnycdn.com/library/600042/videos/{videoId}"
            );

            request.Headers.Add("AccessKey", apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Get Video Info Failed: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting video info {videoId}");
            return null;
        }
    }

    // ===================================
    // توليد Secure Token URL للمشاهدة
    // ===================================
    public string GenerateVideoUrl(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return null;

        var libraryId = _config["Bunny:LibraryId"];
        var secret = _config["Bunny:StreamSecret"];

        if (string.IsNullOrEmpty(libraryId) || string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Bunny CDN settings incomplete for GenerateVideoUrl");
            return $"https://iframe.mediadelivery.net/embed/600042/{videoId}";
        }

        try
        {
            var expiry = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
            var path = $"/600042/{videoId}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(secret + path + expiry)
            );

            var token = BitConverter.ToString(hashBytes)
                .Replace("-", "")
                .ToLower();

            return $"https://iframe.mediadelivery.net/embed/600042/{videoId}?token={token}&expires={expiry}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating video URL for {videoId}");
            return $"https://iframe.mediadelivery.net/embed/600042/{videoId}";
        }
    }

    // ===================================
    // الحصول على قائمة الفيديوهات
    // ===================================
    public async Task<List<object>> GetVideosListAsync(int page = 1, int itemsPerPage = 100)
    {
        var libraryId = _config["Bunny:LibraryId"];
        var apiKey = _config["Bunny:ApiKey"];

        if (string.IsNullOrEmpty(libraryId) || string.IsNullOrEmpty(apiKey))
            return new List<object>();

        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://video.bunnycdn.com/library/600042/videos?page={page}&itemsPerPage={itemsPerPage}"
            );

            request.Headers.Add("AccessKey", apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Get Videos List Failed: {response.StatusCode}");
                return new List<object>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);

            if (result?.items != null)
            {
                return JsonConvert.DeserializeObject<List<object>>(result.items.ToString());
            }

            return new List<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos list");
            return new List<object>();
        }
    }
}