FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NaqelExpressCrawl/NaqelExpressCrawl.csproj", "NaqelExpressCrawl/"]
COPY ["CrawlingService/CrawlingService.csproj", "CrawlingService/"]
RUN dotnet restore "NaqelExpressCrawl/NaqelExpressCrawl.csproj"
COPY . .
WORKDIR "/src/NaqelExpressCrawl"
RUN dotnet build "NaqelExpressCrawl.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NaqelExpressCrawl.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
# to run chrome browser in docker
RUN apt-get update && apt-get install -y xorg openbox libnss3 libasound2
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NaqelExpressCrawl.dll"]
