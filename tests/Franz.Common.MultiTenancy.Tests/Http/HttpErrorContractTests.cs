using Franz.Common.Errors;
using Franz.Common.Http.Bootstrap.Exceptions;
using Franz.Common.Mediator.Validation;
using Franz.Common.Http.Errors;
using Franz.Common.Http.Errors.Strategy;
using Franz.Common.Http.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class HttpErrorContractTests
{
    [Fact]
    public void ErrorResponseProvider_UsesSanitizedProductionStrategyMessage()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var provider = new ErrorResponseProvider(environment);
        var exception = new InvalidOperationException("database password leaked");
        var context = CreateExceptionContext(exception);
        var strategy = new DefaultErrorResponseStrategy(environment);

        var response = provider.PrepareResponse(context, strategy);

        Assert.DoesNotContain("database password leaked", response.Message, StringComparison.Ordinal);
        Assert.Null(response.StackTrace);
        Assert.Equal(StatusCodes.Status500InternalServerError, ((ObjectResult)context.Result!).StatusCode);
    }

    [Fact]
    public void ErrorResponseProvider_UsesBusinessStrategyMessage()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var provider = new ErrorResponseProvider(environment);
        var context = CreateExceptionContext(new FunctionalException("The order cannot be completed."));

        var response = provider.PrepareResponse(context, new BadRequestErrorResponseStrategy());

        Assert.Equal("The order cannot be completed.", response.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, ((ObjectResult)context.Result!).StatusCode);
    }

    [Fact]
    public void ErrorResponseProvider_UsesValidationStrategyForBadRequest()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var provider = new ErrorResponseProvider(environment);
        var validationException = new ValidationException(
            [new ValidationError("Name", "Name is required.")]);
        var context = CreateExceptionContext(validationException);
        var strategy = new ValidationErrorResponseStrategy();

        var response = provider.PrepareResponse(context, strategy);

        Assert.Contains("Name is required.", response.Message, StringComparison.Ordinal);
        Assert.Equal(StatusCodes.Status400BadRequest, ((ObjectResult)context.Result!).StatusCode);
    }

    [Fact]
    public void AddErrors_RegistersDefaultStrategyAsFallback()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(Environments.Production));
        services.AddFranzHttp().AddErrors();

        using var provider = services.BuildServiceProvider();
        var strategies = provider.GetServices<IErrorResponseStrategy>();

        Assert.Contains(strategies, strategy => strategy is DefaultErrorResponseStrategy);
        Assert.NotNull(provider.GetRequiredService<ExceptionFilter>());
    }

    [Fact]
    public async Task GlobalExceptionHandler_MapsValidationExceptionTo400WithStructuredErrors()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var handler = new FranzGlobalExceptionHandler(
            NullLogger<FranzGlobalExceptionHandler>.Instance,
            environment);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new ValidationException(
            [new ValidationError("Name", "Name is required.")]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var response = await reader.ReadToEndAsync();

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("\"errors\"", response, StringComparison.Ordinal);
        Assert.Contains("Name is required.", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GlobalExceptionHandler_MapsBusinessExceptionTo422WithoutLeakingProductionDetails()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var handler = new FranzGlobalExceptionHandler(
            NullLogger<FranzGlobalExceptionHandler>.Instance,
            environment);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new BusinessException("order.conflict", "Internal database table customers failed."),
            CancellationToken.None);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var response = await reader.ReadToEndAsync();

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
        Assert.Contains("Business rule violated", response, StringComparison.Ordinal);
        Assert.DoesNotContain("Internal database table customers failed.", response, StringComparison.Ordinal);
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ExceptionContext(actionContext, []) { Exception = exception };
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Franz.Common.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
