using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SellFlowApi.Exceptions;

namespace SellFlowApi.Middleware
{
   
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (NotFoundException ex) { await Write(ctx, 404, ex.Message); }
        catch (DomainException ex) { await Write(ctx, 400, ex.Message); }
        catch (UnauthorizedAccessException) { await Write(ctx, 401, "Unauthorized"); }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled exception");
            await Write(ctx, 500, "Something went wrong.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { error = message });
    }
}
}