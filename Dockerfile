# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY server_vehicle_parts_ms/server_vehicle_parts_ms.csproj server_vehicle_parts_ms/
RUN dotnet restore server_vehicle_parts_ms/server_vehicle_parts_ms.csproj

COPY server_vehicle_parts_ms/. server_vehicle_parts_ms/
RUN dotnet publish server_vehicle_parts_ms/server_vehicle_parts_ms.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "server_vehicle_parts_ms.dll"]
