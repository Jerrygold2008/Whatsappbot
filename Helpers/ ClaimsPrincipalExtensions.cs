using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using SellFlowApi.Exceptions;

namespace SellFlowApi.Helpers
{
    public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var v = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(v, out var id) ? id : throw new UnauthorizedAccessException();
    }

    public static Guid GetBusinessId(this ClaimsPrincipal user)
    {
        var v = user.FindFirst("businessId")?.Value;
        return Guid.TryParse(v, out var id) ? id : throw new DomainException("Create your business first.");
    }

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? "";
}

}