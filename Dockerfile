FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN mkdir /repo && chown $APP_UID /repo
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ARG NODE_VERSION=18.16.0
ARG ALPINE_VERSION=3.17.2

FROM node:18.16.0-alpine AS node

FROM alpine:3.17.2

COPY --from=node /usr/lib /usr/lib
COPY --from=node /usr/local/lib /usr/local/lib
COPY --from=node /usr/local/include /usr/local/include
COPY --from=node /usr/local/bin /usr/local/bin

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SS14.Admin/SS14.Admin.csproj", "SS14.Admin/"]
RUN apt update && apt install npm -y
RUN npm i --save tailwindcss
RUN dotnet restore "SS14.Admin/SS14.Admin.csproj"
COPY . .
WORKDIR "/src/SS14.Admin"
RUN dotnet build "SS14.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SS14.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SS14.Admin.dll"]
