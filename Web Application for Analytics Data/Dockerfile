FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Web Application for Analytics Data/Web Application for Analytics Data.csproj", "Web Application for Analytics Data/"]
RUN dotnet restore "Web Application for Analytics Data/Web Application for Analytics Data.csproj"
COPY . .
WORKDIR "/src/Web Application for Analytics Data"
RUN dotnet build "Web Application for Analytics Data.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Web Application for Analytics Data.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web Application for Analytics Data.dll"]
