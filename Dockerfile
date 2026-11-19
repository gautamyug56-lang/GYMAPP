# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Rackroom.sln ./
COPY Rackroom.Web/Rackroom.Web.csproj Rackroom.Web/
COPY Rackroom.Application/Rackroom.Application.csproj Rackroom.Application/
COPY Rackroom.Domain/Rackroom.Domain.csproj Rackroom.Domain/
COPY Rackroom.Infrastructure/Rackroom.Infrastructure.csproj Rackroom.Infrastructure/
COPY Rackroom.Tests/Rackroom.Tests.csproj Rackroom.Tests/

RUN dotnet restore Rackroom.sln

COPY . .
RUN dotnet publish Rackroom.Web/Rackroom.Web.csproj     --configuration Release     --no-restore     --output /app/publish     /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/rackroom.db"

COPY --from=build /app/publish .

RUN mkdir -p /app/data /app/App_Data/invoices /app/App_Data/ml

VOLUME ["/app/data", "/app/App_Data"]

EXPOSE 8080

ENTRYPOINT ["dotnet", "Rackroom.Web.dll"]
