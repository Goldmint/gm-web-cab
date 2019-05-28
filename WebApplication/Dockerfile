FROM microsoft/dotnet:2.1-aspnetcore-runtime

WORKDIR /app
COPY ./dist/ ./

ENTRYPOINT ["dotnet", "Goldmint.WebApplication.dll"]
