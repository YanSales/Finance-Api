FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Arquivos de build compartilhados e .csproj primeiro: preserva o cache da camada de restore.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Api/Api.csproj src/Api/
COPY src/Application/Application.csproj src/Application/
COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/

RUN dotnet restore src/Api/Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# As imagens oficiais do .NET definem APP_UID (usuario sem privilegios). Porta 8080 nao exige root.
USER $APP_UID
EXPOSE 8080

# Sem HEALTHCHECK: a imagem de runtime nao possui curl/wget. Orquestradores devem usar
# probes HTTP em /health/live (liveness) e /health/ready (readiness).
ENTRYPOINT ["dotnet", "Api.dll"]
