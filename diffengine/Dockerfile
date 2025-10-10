# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project and restore
COPY ["DiffengineExample.csproj", "./"]
RUN dotnet restore "DiffengineExample.csproj"

# Copy everything and build
COPY . .
RUN dotnet build "DiffengineExample.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiffengineExample.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Listen on port 80
ENV ASPNETCORE_URLS=http://+:80

# Run the app
ENTRYPOINT ["dotnet","DiffengineExample.dll"]
