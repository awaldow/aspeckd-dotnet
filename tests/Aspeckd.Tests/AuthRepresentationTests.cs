using System.Net.Http.Json;
using Aspeckd.Attributes;
using Aspeckd.Configuration;
using Aspeckd.Extensions;
using Aspeckd.Models;
using Aspeckd.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspeckd.Tests;

/// <summary>
/// Tests that cover the root-level and group-level auth representation in the agent spec.
/// </summary>
public class AuthRepresentationTests
{
    // -----------------------------------------------------------------------
    // AgentAuthInfo model
    // -----------------------------------------------------------------------

    [Fact]
    public void AgentAuthInfo_AllPropertiesDefaultToNull()
    {
        var auth = new AgentAuthInfo();

        Assert.Null(auth.Scheme);
        Assert.Null(auth.HeaderName);
        Assert.Null(auth.TokenEndpoint);
        Assert.Null(auth.GrantType);
        Assert.Null(auth.Instructions);
    }

    [Fact]
    public void AgentAuthInfo_StoresAllProperties()
    {
        var auth = new AgentAuthInfo
        {
            Scheme = "bearer",
            HeaderName = "Authorization",
            TokenEndpoint = "https://example.com/token",
            GrantType = "client_credentials",
            Instructions = "Request access via the portal."
        };

        Assert.Equal("bearer", auth.Scheme);
        Assert.Equal("Authorization", auth.HeaderName);
        Assert.Equal("https://example.com/token", auth.TokenEndpoint);
        Assert.Equal("client_credentials", auth.GrantType);
        Assert.Equal("Request access via the portal.", auth.Instructions);
    }

    [Fact]
    public void AgentAuthInfo_TokenEndpointNullIsValidValue()
    {
        // Null TokenEndpoint is a valid explicit signal that there's no programmatic path.
        var auth = new AgentAuthInfo
        {
            Scheme = "bearer",
            HeaderName = "Authorization",
            TokenEndpoint = null,
            Instructions = "Visit the portal to obtain a token."
        };

        Assert.Null(auth.TokenEndpoint);
        Assert.Equal("bearer", auth.Scheme);
    }

    [Fact]
    public void AgentAuthInfo_InstructionsSupportsFreeText()
    {
        var instructions = "## Steps\n\n1. Navigate to https://portal.example.com\n2. Request access\n3. Use the token.";
        var auth = new AgentAuthInfo { Instructions = instructions };
        Assert.Equal(instructions, auth.Instructions);
    }

    // -----------------------------------------------------------------------
    // AspeckdOptions.Auth
    // -----------------------------------------------------------------------

    [Fact]
    public void AspeckdOptions_AuthDefaultsToNull()
    {
        var options = new AspeckdOptions();
        Assert.Null(options.Auth);
    }

    [Fact]
    public void AspeckdOptions_AuthCanBeSet()
    {
        var options = new AspeckdOptions
        {
            Auth = new AgentAuthInfo
            {
                Scheme = "bearer",
                HeaderName = "Authorization"
            }
        };

        Assert.NotNull(options.Auth);
        Assert.Equal("bearer", options.Auth.Scheme);
    }

    // -----------------------------------------------------------------------
    // AgentToolGroupAttribute.AuthInstructions
    // -----------------------------------------------------------------------

    [Fact]
    public void AgentToolGroupAttribute_AuthInstructionsDefaultsToNull()
    {
        var attr = new AgentToolGroupAttribute("MyGroup");
        Assert.Null(attr.AuthInstructions);
    }

    [Fact]
    public void AgentToolGroupAttribute_AuthInstructionsCanBeSet()
    {
        var attr = new AgentToolGroupAttribute("MyGroup")
        {
            AuthInstructions = "Request PIM activation through the Azure portal."
        };

        Assert.Equal("Request PIM activation through the Azure portal.", attr.AuthInstructions);
    }

    // -----------------------------------------------------------------------
    // AgentSpecIndex.Auth — root-level auth block in index response
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_ContainsRootAuthBlock_WhenConfigured()
    {
        await using var factory = new AuthTestWebAppFactory(
            auth: new AgentAuthInfo
            {
                Scheme = "bearer",
                HeaderName = "Authorization",
                Instructions = "Use your Azure AD token."
            });
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.NotNull(index.Auth);
        Assert.Equal("bearer", index.Auth.Scheme);
        Assert.Equal("Authorization", index.Auth.HeaderName);
        Assert.Equal("Use your Azure AD token.", index.Auth.Instructions);
    }

    [Fact]
    public async Task GetIndex_AuthIsNull_WhenNotConfigured()
    {
        // TestWebAppFactory does not set Auth in options.
        await using var factory = new Helpers.TestWebAppFactory();
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        Assert.Null(index.Auth);
    }

    // -----------------------------------------------------------------------
    // AgentToolGroup.Auth — group-level auth override
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetIndex_GroupHasAuthBlock_WhenAuthInstructionsSet()
    {
        await using var factory = new AuthTestWebAppFactory(
            auth: new AgentAuthInfo { Scheme = "bearer", HeaderName = "Authorization" },
            useGroupAuthInstructions: true);
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var group = index.Groups.First(g => g.Name == "SecureGroup");
        Assert.NotNull(group.Auth);
        Assert.Equal(AuthTestWebAppFactory.GroupAuthInstructions, group.Auth.Instructions);
    }

    [Fact]
    public async Task GetIndex_GroupAuthIsNull_WhenNoAuthInstructionsSet()
    {
        await using var factory = new AuthTestWebAppFactory(
            auth: new AgentAuthInfo { Scheme = "bearer" },
            useGroupAuthInstructions: false);
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var group = index.Groups.First(g => g.Name == "SecureGroup");
        Assert.Null(group.Auth);
    }

    [Fact]
    public async Task GetIndex_GroupAuthOnlyContainsInstructions_WhenSetViaAttribute()
    {
        await using var factory = new AuthTestWebAppFactory(
            auth: new AgentAuthInfo { Scheme = "bearer", HeaderName = "Authorization" },
            useGroupAuthInstructions: true);
        var client = factory.CreateClient();

        var index = await client.GetFromJsonAsync<AgentSpecIndex>("/.well-known/agents");

        Assert.NotNull(index);
        var group = index.Groups.First(g => g.Name == "SecureGroup");
        Assert.NotNull(group.Auth);
        // Only instructions is set via attribute; scheme and headerName inherit from root.
        Assert.Equal(AuthTestWebAppFactory.GroupAuthInstructions, group.Auth.Instructions);
        Assert.Null(group.Auth.Scheme);
        Assert.Null(group.Auth.HeaderName);
        Assert.Null(group.Auth.TokenEndpoint);
        Assert.Null(group.Auth.GrantType);
    }
}

/// <summary>
/// A minimal web application factory used by <see cref="AuthRepresentationTests"/>
/// to exercise root and group-level auth configuration.
/// </summary>
internal sealed class AuthTestWebAppFactory : WebApplicationFactory<AuthTestWebAppFactory>
{
    private readonly AgentAuthInfo? _auth;
    private readonly bool _useGroupAuthInstructions;

    internal const string GroupAuthInstructions = "Need elevated access for this group.";
    internal const string GroupPimInstructions = "Request PIM activation.";

    public AuthTestWebAppFactory(AgentAuthInfo? auth, bool useGroupAuthInstructions = false)
    {
        _auth = auth;
        _useGroupAuthInstructions = useGroupAuthInstructions;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        var auth = _auth;
        var useGroupAuth = _useGroupAuthInstructions;

        return Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAgentSpec(opt =>
                    {
                        opt.Title = "Auth Test API";
                        opt.Auth = auth;
                    });
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        if (useGroupAuth)
                        {
                            endpoints.MapGet(
                                "/api/secure",
                                [AgentToolGroup("SecureGroup",
                                    Description = "Secure operations",
                                    AuthInstructions = AuthTestWebAppFactory.GroupAuthInstructions)]
                                [AgentDescription("Secure endpoint")]
                                () => Results.Ok("secure"));
                        }
                        else
                        {
                            endpoints.MapGet(
                                "/api/secure",
                                [AgentToolGroup("SecureGroup",
                                    Description = "Secure operations")]
                                [AgentDescription("Secure endpoint")]
                                () => Results.Ok("secure"));
                        }

                        endpoints.MapGet(
                            "/api/open",
                            [AgentDescription("Open endpoint")]
                            () => Results.Ok("open"));

                        endpoints.MapAgentSpec();
                        endpoints.MapControllers();
                    });
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var contentRoot = Path.GetDirectoryName(typeof(AuthTestWebAppFactory).Assembly.Location)!;
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebHostDefaults.ContentRootKey] = contentRoot
            }));

        return base.CreateHost(builder);
    }
}
