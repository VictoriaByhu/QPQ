# Збірка з контекстом Services/Catalog (там лежать усі п'ять проєктів сервісу):
#   docker build -f Catalog.Api/Dockerfile -t qpq-catalog-api .
#   docker run -p 8080:8080 -e ConnectionStrings__CatalogDb="Server=...;Database=QPQ_CatalogEf;..." qpq-catalog-api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спершу лише csproj: шар із restore кешується, поки не змінилися залежності.
COPY Catalog.Domain/Catalog.Domain.csproj Catalog.Domain/
COPY Catalog.Dal/Catalog.Dal.csproj Catalog.Dal/
COPY Catalog.Bll/Catalog.Bll.csproj Catalog.Bll/
COPY Catalog.Api/Catalog.Api.csproj Catalog.Api/
RUN dotnet restore Catalog.Api/Catalog.Api.csproj

COPY Catalog.Domain/ Catalog.Domain/
COPY Catalog.Dal/ Catalog.Dal/
COPY Catalog.Bll/ Catalog.Bll/
COPY Catalog.Api/ Catalog.Api/
RUN dotnet publish Catalog.Api/Catalog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Образ aspnet запускається не від root; порт 8080 — стандарт .NET 8+.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
