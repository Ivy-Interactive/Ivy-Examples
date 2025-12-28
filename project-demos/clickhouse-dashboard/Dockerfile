# Simple Dockerfile for Ivy App only
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app
COPY ClickHouseDashboard.cs ./

ENV ASPNETCORE_URLS=http://+:5010
ENV CLICKHOUSE_HOST=clickhouse
EXPOSE 5010

CMD ["dotnet", "run", "ClickHouseDashboard.cs"]
