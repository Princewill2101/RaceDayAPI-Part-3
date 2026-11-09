using System.Net;

namespace RaceDayWeb.Services;

// What every API call returns: either data, or a friendly error message.
public class ApiResult<T>
{
    public bool Success { get; init; }
    public HttpStatusCode Status { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = string.Empty;

    // 401 from the API means our session there has ended
    public bool SessionExpired => Status == HttpStatusCode.Unauthorized;

    public static ApiResult<T> Ok(T? data, HttpStatusCode status) =>
        new() { Success = true, Data = data, Status = status };

    public static ApiResult<T> Fail(HttpStatusCode status, string error) =>
        new() { Success = false, Status = status, Error = error };
}

// For calls where we only care whether they worked
public class Empty { }
