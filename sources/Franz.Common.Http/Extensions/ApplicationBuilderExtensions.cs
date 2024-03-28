using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace Microsoft.AspNetCore.Builder;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFrenchLocalization(this IApplicationBuilder applicationBuilder)
    {
        var defaultCulture = "fr";
        var cultureInfo = new CultureInfo(defaultCulture);

        applicationBuilder.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(cultureInfo),
            SupportedCultures = new List<CultureInfo> { cultureInfo },
            SupportedUICultures = new List<CultureInfo> { cultureInfo },
        });

        return applicationBuilder;
    }

    public static IApplicationBuilder UseDefaultCors(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseCors();

        return applicationBuilder;
    }

}