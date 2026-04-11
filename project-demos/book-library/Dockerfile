FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["BookLibrary.csproj", "./"]
RUN dotnet restore "BookLibrary.csproj"

COPY . .
RUN dotnet build "BookLibrary.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookLibrary.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV PORT=80
ENV ASPNETCORE_URLS="http://+:80"

ENTRYPOINT ["dotnet","./BookLibrary.dll"]
