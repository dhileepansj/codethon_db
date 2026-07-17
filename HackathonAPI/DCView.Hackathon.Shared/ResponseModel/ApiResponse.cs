namespace DCView.Hackathon.Shared.ResponseModel;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public ApiResponse(T? data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public ApiResponse(IEnumerable<string> errors, string? message = null)
    {
        Success = false;
        Errors = errors;
        Message = message;
    }
}
