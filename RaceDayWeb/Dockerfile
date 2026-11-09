# ---- Build stage: restore + publish ----------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the restore layer is cached
# until the dependencies actually change.
COPY RaceDayWeb.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage: small image with just the ASP.NET runtime --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Listen on 8080 inside the container and don't run as root
ENV ASPNETCORE_URLS=http://+:8080
USER app
EXPOSE 8080

# Where the Part 2 API lives. Override at run time with -e ApiSettings__BaseUrl=...
ENV ApiSettings__BaseUrl=http://host.docker.internal:5080/

ENTRYPOINT ["dotnet", "RaceDayWeb.dll"]
