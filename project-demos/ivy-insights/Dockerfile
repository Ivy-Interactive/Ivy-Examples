# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy and restore
COPY ["IvyInsights.csproj", "./"]
RUN dotnet restore "IvyInsights.csproj"

# Copy everything and build
COPY . .
RUN dotnet build "IvyInsights.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IvyInsights.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV PORT=80
ENV ASPNETCORE_URLS="http://+:80"

# Run the executable
ENTRYPOINT ["dotnet","./IvyInsights.dll"]