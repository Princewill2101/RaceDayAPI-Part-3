using RaceDayWeb.Models;

namespace RaceDayWeb.Services;

// Every data operation in the MVC app goes through this interface -
// the MVC project has no database access of its own.
public interface IRaceDayApiClient
{
    // Auth
    Task<ApiResult<UserDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResult<Empty>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResult<Empty>> LogoutAsync();

    // Profile
    Task<ApiResult<UserDto>> GetProfileAsync();
    Task<ApiResult<UserDto>> UpdateProfileAsync(UpdateProfileRequestDto request);
    Task<ApiResult<UserDto>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResult<List<EventDto>>> GetEventsAsync();
    Task<ApiResult<EventDto>> GetEventAsync(int id);
    Task<ApiResult<List<EventTypeDto>>> GetEventTypesAsync();
    Task<ApiResult<List<OrganiserEventDto>>> GetMyOrganiserEventsAsync();
    Task<ApiResult<EventDto>> CreateEventAsync(EventRequestDto request);
    Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequestDto request);
    Task<ApiResult<Empty>> DeleteEventAsync(int id);
    Task<ApiResult<Empty>> UploadBannerAsync(int eventId, IFormFile file);

    // Categories
    Task<ApiResult<CategoryDto>> AddCategoryAsync(int eventId, CategoryRequestDto request);
    Task<ApiResult<Empty>> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult<EnrolmentDto>> EnrolAsync(int eventId, int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();

    // Results
    Task<ApiResult<ResultDto>> RecordResultAsync(int enrolmentId, ResultRequestDto request);
    Task<ApiResult<List<ResultDto>>> GetMyResultsAsync();
}
