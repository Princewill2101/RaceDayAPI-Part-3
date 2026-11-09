using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RaceDayWeb.Models;

namespace RaceDayWeb.Services;

// Talks to the Part 2 API. The API keeps its own session (cookie "RaceDay.Session"),
// so on login we capture that cookie, keep it in THIS app's session, and send it
// back with every later request made for that user.
public class RaceDayApiClient : IRaceDayApiClient
{
    private const string ApiCookieName = "RaceDay.Session";

    // The API uses PascalCase property names (see its Program.cs)
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy        = null,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApiClient> _log;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor,
        ILogger<RaceDayApiClient> log)
    {
        _http     = http;
        _accessor = accessor;
        _log      = log;
    }

    private ISession? Session => _accessor.HttpContext?.Session;

    // ── Auth ─────────────────────────────────────────────────
    public async Task<ApiResult<UserDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = JsonContent.Create(request, options: Json)
            };
            using var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
                return ApiResult<UserDto>.Fail(res.StatusCode, await ReadErrorAsync(res));

            // Grab the API's session cookie from the Set-Cookie header
            if (res.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var c in cookies)
                {
                    if (c.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
                    {
                        string value = c.Substring(ApiCookieName.Length + 1).Split(';')[0];
                        Session?.SetString(SessionKeys.ApiCookie, value);
                    }
                }
            }

            var user = JsonSerializer.Deserialize<UserDto>(
                await res.Content.ReadAsStringAsync(), Json);
            return ApiResult<UserDto>.Ok(user, res.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Unreachable<UserDto>(ex);
        }
    }

    public Task<ApiResult<Empty>> RegisterAsync(RegisterRequestDto request) =>
        SendAsync<Empty>(HttpMethod.Post, "api/auth/register", request);

    public async Task<ApiResult<Empty>> LogoutAsync()
    {
        var result = await SendAsync<Empty>(HttpMethod.Post, "api/auth/logout");
        Session?.Remove(SessionKeys.ApiCookie);
        return result;
    }

    // ── Profile ──────────────────────────────────────────────
    public Task<ApiResult<UserDto>> GetProfileAsync() =>
        SendAsync<UserDto>(HttpMethod.Get, "api/users/me");

    public Task<ApiResult<UserDto>> UpdateProfileAsync(UpdateProfileRequestDto request) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/users/me", request);

    public Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/users/me/picture", content: BuildFileContent(file));

    // ── Events ───────────────────────────────────────────────
    public Task<ApiResult<List<EventDto>>> GetEventsAsync() =>
        SendAsync<List<EventDto>>(HttpMethod.Get, "api/events");

    public Task<ApiResult<EventDto>> GetEventAsync(int id) =>
        SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<List<EventTypeDto>>> GetEventTypesAsync() =>
        SendAsync<List<EventTypeDto>>(HttpMethod.Get, "api/eventtypes");

    public Task<ApiResult<List<OrganiserEventDto>>> GetMyOrganiserEventsAsync() =>
        SendAsync<List<OrganiserEventDto>>(HttpMethod.Get, "api/organiser/events");

    public Task<ApiResult<EventDto>> CreateEventAsync(EventRequestDto request) =>
        SendAsync<EventDto>(HttpMethod.Post, "api/events", request);

    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequestDto request) =>
        SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", request);

    public Task<ApiResult<Empty>> DeleteEventAsync(int id) =>
        SendAsync<Empty>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResult<Empty>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<Empty>(HttpMethod.Post, $"api/events/{eventId}/banner", content: BuildFileContent(file));

    // ── Categories ───────────────────────────────────────────
    public Task<ApiResult<CategoryDto>> AddCategoryAsync(int eventId, CategoryRequestDto request) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<ApiResult<Empty>> DeleteCategoryAsync(int categoryId) =>
        SendAsync<Empty>(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ── Enrolments ───────────────────────────────────────────
    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/events/{eventId}/enrolments",
            new EnrolRequestDto { CategoryId = categoryId });

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments/me");

    // ── Results ──────────────────────────────────────────────
    public Task<ApiResult<ResultDto>> RecordResultAsync(int enrolmentId, ResultRequestDto request) =>
        SendAsync<ResultDto>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request);

    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, "api/results/me");

    // ── Plumbing ─────────────────────────────────────────────

    // Sends a request with this user's API session cookie attached.
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url,
        object? body = null, HttpContent? content = null)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);

            string? cookie = Session?.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
                req.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={cookie}");

            if (content != null)
                req.Content = content;
            else if (body != null)
                req.Content = JsonContent.Create(body, body.GetType(), options: Json);

            using var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
                return ApiResult<T>.Fail(res.StatusCode, await ReadErrorAsync(res));

            T? data = default;
            if (res.StatusCode != HttpStatusCode.NoContent)
            {
                string text = await res.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(text))
                    data = JsonSerializer.Deserialize<T>(text, Json);
            }
            return ApiResult<T>.Ok(data, res.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Unreachable<T>(ex);
        }
    }

    private static MultipartFormDataContent BuildFileContent(IFormFile file)
    {
        var form   = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(stream, "file", file.FileName);   // field name must match the API parameter
        return form;
    }

    private ApiResult<T> Unreachable<T>(Exception ex)
    {
        _log.LogWarning(ex, "Could not reach the RaceDay API.");
        return ApiResult<T>.Fail(HttpStatusCode.ServiceUnavailable,
            "We couldn't reach the RaceDay service. Please try again in a moment.");
    }

    // Turns an API error response into one short message that is safe to show to a user.
    private static async Task<string> ReadErrorAsync(HttpResponseMessage res)
    {
        string? apiMessage = null;
        try
        {
            string text = await res.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var m) &&
                        m.ValueKind == JsonValueKind.String)
                        apiMessage = m.GetString();
                    else if (doc.RootElement.TryGetProperty("errors", out var errs) &&
                             errs.ValueKind == JsonValueKind.Object)
                    {
                        // ASP.NET validation problem details: take the first message
                        foreach (var field in errs.EnumerateObject())
                            foreach (var msg in field.Value.EnumerateArray())
                            {
                                apiMessage = msg.GetString();
                                goto done;
                            }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            /* not the JSON shape we expected - fall through to the generic message */
        }
        done:

        return res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => apiMessage ?? "Your session has ended. Please log in again.",
            HttpStatusCode.Forbidden    => "You don't have permission to do that.",
            HttpStatusCode.NotFound     => apiMessage ?? "We couldn't find what you were looking for.",
            HttpStatusCode.BadRequest   => apiMessage ?? "Some of the details you entered aren't valid.",
            HttpStatusCode.Conflict     => apiMessage ?? "That has already been done.",
            HttpStatusCode.ServiceUnavailable => apiMessage ?? "That service is unavailable right now.",
            _ => "Something went wrong on our side. Please try again."
        };
    }
}
