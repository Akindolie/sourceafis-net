# Use the official .NET image as a base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["FingerprintService/FingerprintService.csproj", "FingerprintService/"]
COPY ["SourceAFIS/SourceAFIS.csproj", "SourceAFIS/"]
RUN dotnet restore "FingerprintService/FingerprintService.csproj"
COPY . .
WORKDIR "/src/FingerprintService"
RUN dotnet build "FingerprintService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FingerprintService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FingerprintService.dll"]
