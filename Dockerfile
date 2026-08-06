FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY aspire/TeamHub.ServiceDefaults/TeamHub.ServiceDefaults.csproj aspire/TeamHub.ServiceDefaults/
COPY building-blocks/TeamHub.Redis/TeamHub.Redis.csproj building-blocks/TeamHub.Redis/
COPY building-blocks/TeamHub.Observability/TeamHub.Observability.csproj building-blocks/TeamHub.Observability/
COPY building-blocks/TeamHub.GrpcContracts/TeamHub.GrpcContracts.csproj building-blocks/TeamHub.GrpcContracts/
COPY services/team-hub-auth/team-hub-auth/team-hub-auth.csproj services/team-hub-auth/team-hub-auth/
RUN dotnet restore services/team-hub-auth/team-hub-auth/team-hub-auth.csproj

COPY aspire/TeamHub.ServiceDefaults/ aspire/TeamHub.ServiceDefaults/
COPY building-blocks/TeamHub.Redis/ building-blocks/TeamHub.Redis/
COPY building-blocks/TeamHub.Observability/ building-blocks/TeamHub.Observability/
COPY building-blocks/TeamHub.GrpcContracts/ building-blocks/TeamHub.GrpcContracts/
COPY services/team-hub-auth/team-hub-auth/ services/team-hub-auth/team-hub-auth/
RUN dotnet publish services/team-hub-auth/team-hub-auth/team-hub-auth.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .
RUN chown -R app:app /app

USER app

ENV ASPNETCORE_URLS=
EXPOSE 8080
EXPOSE 8081

HEALTHCHECK --interval=120s --timeout=5s --start-period=15s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "team-hub-auth.dll"]
