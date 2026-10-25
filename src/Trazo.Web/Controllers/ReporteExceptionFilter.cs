using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

public class ReporteExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not PublicacionException error) return;
        var path = context.HttpContext.Request.Path.Value!;
        if (path.EndsWith("/resolver", StringComparison.Ordinal)) path = path[..^9];
        context.Result = new ViewResult {
            ViewName = "~/Views/Reportes/Error.cshtml", StatusCode = error.Status,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState) {
                ["Mensaje"] = error.Message, ["Revisar"] = path, ["Title"] = "Revisar solicitud"
            }
        };
        context.ExceptionHandled = true;
    }
}
