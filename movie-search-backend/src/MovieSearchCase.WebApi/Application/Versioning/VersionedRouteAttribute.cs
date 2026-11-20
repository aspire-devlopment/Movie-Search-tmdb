using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.WebApi.Application;

namespace MovieSearchCase.WebApi.Application.Versioning;

[AttributeUsage(AttributeTargets.Class)]
public class VersionedRouteAttribute : RouteAttribute
{
    public VersionedRouteAttribute()
        : base(ApiDefaults.VersionedControllerRoute)
    {
    }
}
