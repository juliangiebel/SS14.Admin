# SS14.Admin

SS14.Admin is the web-based admin panel intended to be used with Space Station 14.

## Configuration

Example config file:

```yml
Serilog:
    Using: [ "Serilog.Sinks.Console" ]
    MinimumLevel:
        Default: Information
        Override:
            SS14: Debug
            Microsoft: "Warning"
            Microsoft.Hosting.Lifetime: "Information"
            Microsoft.AspNetCore: Warning
            IdentityServer4: Warning
    WriteTo:
        - Name: Console
          Args:
              OutputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}"

    Enrich: [ "FromLogContext" ]

    #Loki:
    #    Address: "http://localhost:3102"
    #    Name: "centcomm"

ConnectionStrings:
    # Connects to the same postgres database as the game server
    DefaultConnection: "Server=127.0.0.1;Port=5432;Database=ss14;User Id=ss14-admin;Password=foobar"

#set to your domain ex: ss14server.com
AllowedHosts: "central.spacestation14.io"

urls: "http://localhost:27689/"

PathBase: "/admin"

WebRootPath: "/opt/ss14_admin/bin/wwwroot"

ForwardProxies:
    - 127.0.0.1

Auth:
    Authority: "https://central.spacestation14.io/web/"
    ClientId: "YOUR-CLIENT-ID"
    ClientSecret: "foobar"

authServer: "https://central.spacestation14.io/auth"
```
## Build

To build for a linux based system run the command
```
dotnet publish -c Release -r linux-x64 --no-self-contained
```
The files will be dropped in the Publish folder as seen in the structure below
```
SS14.Admin
└─bin
   └─Release
        └─net9.0
            └─linux-x64
                └─publish
                    │   wwwroot
                    │   ...Other files
```
After adding a properly [configured]() ``` appsettings.yml```, to the publish folder just run ```./SS14.Admin```
### Extras

When registering an OAuth app against our auth server, use `/signin-oidc` as redirect URI (relative to whatever path your SS14.Admin thing is at, so for us it's `https://central.spacestation14.io/admin/signin-oidc`).

It is recommended to run the get commands below, to untrack the development appsettings.
```
git update-index --assume-unchanged appsettings.Development.yml
```
or this command to re track it.
```
git update-index --no-assume-unchanged appsettings.Development.yml
```
