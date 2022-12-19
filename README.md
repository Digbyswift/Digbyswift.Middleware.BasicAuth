# Digbyswift.Middleware.BasicAuth

No thrills BasicAuth middleware.

## Config

```
"BasicAuth": {
    "Enabled": true/false,
    "Username": "",
    "Password": "",
    "Realm": ""
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