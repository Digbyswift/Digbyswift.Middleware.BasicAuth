# Digbyswift.Middleware.BasicAuth

[![NuGet version (Digbyswift.Middleware.BasicAuth)](https://img.shields.io/nuget/v/Digbyswift.Middleware.BasicAuth.svg)](https://www.nuget.org/packages/Digbyswift.Middleware.BasicAuth/)
[![Build and publish package](https://github.com/Digbyswift/Digbyswift.Middleware.BasicAuth/actions/workflows/dotnet-build-publish.yml/badge.svg)](https://github.com/Digbyswift/Digbyswift.Middleware.BasicAuth/actions/workflows/dotnet-build-publish.yml)

No thrills BasicAuth middleware.

## Config

```
"BasicAuth": {
    "Enabled": true/false,
    "Username": "",
    "Password": "",
    "Realm": "",
    "ByPassKey": "",
    "ExcludedPaths": ["/robots.txt", "/media"],
    "WhitelistedReferrers":  ["https://example.net", "https://www.example.com"],
    "WhitelistedIPs":  ["127.0.0.1", "127.0.0.2"]
}
```

If needed, this can be accessed using:

```
public YourClass(IOptions<BasicAuthSettings> authSettings)
{
    ...
}
```

## Startup

```
services.AddBasicAuth()
```
and

```
appBuilder.UseBasicHttpAuthentication()
```

There is no need to use conditional checks for registering this middleware since
the config setting `Enabled` will prevent the middleware from being registered.