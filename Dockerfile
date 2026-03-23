FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 80
ENV PORT=80
ENV ASPNETCORE_URLS="http://+:80"
CMD ["echo", "placeholder"]
