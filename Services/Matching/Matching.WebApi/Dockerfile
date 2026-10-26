# Збірка з контекстом Services/Matching (там лежать усі проєкти сервісу):
#   docker build -f Matching.WebApi/Dockerfile -t qpq-matching-api .
#   docker run -p 8080:8080 -e MongoDb__ConnectionString="mongodb://host:27017" qpq-matching-api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спершу лише csproj: шар із restore кешується, поки не змінилися залежності.
COPY Matching.Domain/Matching.Domain.csproj Matching.Domain/
COPY Matching.Application/Matching.Application.csproj Matching.Application/
COPY Matching.Infrastructure/Matching.Infrastructure.csproj Matching.Infrastructure/
COPY Matching.WebApi/Matching.WebApi.csproj Matching.WebApi/
RUN dotnet restore Matching.WebApi/Matching.WebApi.csproj

COPY Matching.Domain/ Matching.Domain/
COPY Matching.Application/ Matching.Application/
COPY Matching.Infrastructure/ Matching.Infrastructure/
COPY Matching.WebApi/ Matching.WebApi/
RUN dotnet publish Matching.WebApi/Matching.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Matching.WebApi.dll"]
