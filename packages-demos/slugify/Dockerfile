# ----------------------------
# Base runtime image
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# ----------------------------
# Build stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Slugify.csproj", "./"]
RUN dotnet restore "Slugify.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Slugify.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ----------------------------
# Publish stage
# ----------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Slugify.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

# ----------------------------
# Final runtime image
# ----------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables
ENV PORT=80
ENV ASPNETCORE_URLS="http://+:80"

# Run the application
ENTRYPOINT ["dotnet", "Slugify.dll"]