# Збірка з контекстом у корені репозиторію (Gateway залежить від Platform.ServiceDefaults):
#   docker build -f Platform.ApiGateway/Dockerfile -t qpq-gateway .
# Адреси сервісів у кластерах YARP (http://catalog-api тощо) поза Aspire задайте через
# services__<ім'я>__http__0 або перевизначте ReverseProxy__Clusters__* змінними оточення.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спершу лише csproj: шар із restore кешується, поки не змінилися залежності.
COPY Platform.ServiceDefaults/Platform.ServiceDefaults.csproj Platform.ServiceDefaults/
COPY Platform.ApiGateway/Platform.ApiGateway.csproj Platform.ApiGateway/
RUN dotnet restore Platform.ApiGateway/Platform.ApiGateway.csproj

COPY Platform.ServiceDefaults/ Platform.ServiceDefaults/
COPY Platform.ApiGateway/ Platform.ApiGateway/
RUN dotnet publish Platform.ApiGateway/Platform.ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Platform.ApiGateway.dll"]
