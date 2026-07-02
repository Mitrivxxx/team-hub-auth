FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY team-hub-auth/team-hub-auth.csproj team-hub-auth/
RUN dotnet restore team-hub-auth/team-hub-auth.csproj

COPY team-hub-auth/ team-hub-auth/
RUN dotnet publish team-hub-auth/team-hub-auth.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "team-hub-auth.dll"]
