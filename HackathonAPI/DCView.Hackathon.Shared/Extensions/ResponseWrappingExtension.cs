using Microsoft.Extensions.DependencyInjection;
using DCView.Hackathon.Shared.Filters;

namespace DCView.Hackathon.Shared.Extensions;

public static class ResponseWrappingExtension
{
    public static IMvcBuilder AddResponseWrapper(this IMvcBuilder builder)
    {
        builder.AddMvcOptions(options =>
        {
            options.Filters.Add<ResponseWrapperFilter>();
        });
        return builder;
    }
}
