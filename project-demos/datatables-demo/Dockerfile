FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DataTablesDemo.csproj", "."]
RUN dotnet restore "DataTablesDemo.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "DataTablesDemo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataTablesDemo.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataTablesDemo.dll"]
