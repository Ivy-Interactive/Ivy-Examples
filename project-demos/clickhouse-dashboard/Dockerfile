# Simple Dockerfile for Ivy App only
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app
COPY ClickHouseDashboard.cs ./
COPY init.sql ./

ENV PORT=5000
ENV ASPNETCORE_URLS=http://+:5000
ENV CLICKHOUSE_HOST=clickhouse
EXPOSE 5000

CMD ["dotnet", "run", "ClickHouseDashboard.cs"]
