using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DCView.Hackathon.Shared.ResponseModel;

namespace DCView.Hackathon.Shared.Filters;

public class ResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            var valueType = objectResult.Value.GetType();
            var isEnvelope = valueType.IsGenericType
                             && valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>);

            if (!isEnvelope)
            {
                var apiResponseType = typeof(ApiResponse<>).MakeGenericType(valueType);
                var apiResponse = Activator.CreateInstance(apiResponseType, objectResult.Value, null);
                context.Result = new ObjectResult(apiResponse)
                {
                    StatusCode = objectResult.StatusCode
                };
            }
        }
        else if (context.Result is EmptyResult)
        {
            context.Result = new ObjectResult(new ApiResponse<object>(null))
            {
                StatusCode = StatusCodes.Status204NoContent
            };
        }

        await next();
    }
}
