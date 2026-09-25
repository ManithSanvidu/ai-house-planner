FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HousePlanner.API/HousePlanner.API.csproj", "HousePlanner.API/"]
RUN dotnet restore "./HousePlanner.API/HousePlanner.API.csproj"
COPY . .
WORKDIR "/src/HousePlanner.API"
RUN dotnet build "./HousePlanner.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HousePlanner.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HousePlanner.API.dll"]
