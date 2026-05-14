# Dockerfile for the Ivy deploy UI (the web wizard).
# The Ubuntu Desktop image that gets deployed to Sliplane
# lives at docker/Dockerfile.ubuntu-desktop.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["TendrilUbuntuDeploy.csproj", "./"]
RUN dotnet restore "TendrilUbuntuDeploy.csproj"

COPY . .
RUN dotnet build "TendrilUbuntuDeploy.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TendrilUbuntuDeploy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV PORT=80
ENV ASPNETCORE_URLS="http://+:80"

ENTRYPOINT ["dotnet", "./TendrilUbuntuDeploy.dll"]
