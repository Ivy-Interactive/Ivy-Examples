# Dockerfile for Ivy File-Based App
# Using SDK image since file-based apps require dotnet run

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

# Copy the application file
COPY HelloApp.cs .

# Set environment variables for Ivy
ENV ASPNETCORE_URLS=http://+:5010
ENV DOTNET_EnablePackageSdkResolver=true

# Expose Ivy port (default is 5010)
EXPOSE 5010

# Run file-based app
# For file-based apps we use dotnet run with the file directly
CMD ["dotnet", "run", "HelloApp.cs"]
