# syntax=docker/dockerfile:1
# ---- Build ----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the project files, so this layer is cached until dependencies change.
COPY Directory.Build.props Directory.Packages.props ClinicSpace.slnx ./
COPY src/ClinicSpace.Domain/ClinicSpace.Domain.csproj src/ClinicSpace.Domain/
COPY src/ClinicSpace.Application/ClinicSpace.Application.csproj src/ClinicSpace.Application/
COPY src/ClinicSpace.Infrastructure/ClinicSpace.Infrastructure.csproj src/ClinicSpace.Infrastructure/
COPY src/ClinicSpace.Api/ClinicSpace.Api.csproj src/ClinicSpace.Api/
RUN dotnet restore src/ClinicSpace.Api/ClinicSpace.Api.csproj

COPY src/ src/
RUN dotnet publish src/ClinicSpace.Api/ClinicSpace.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# ---- Runtime --------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=Europe/London
COPY --from=build /app .
# Folder for data protection keys, owned by the non-root user so a mounted volume can be written.
RUN mkdir -p /app/keys && chown "$APP_UID" /app/keys

# Run as the image's built-in non-root user.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClinicSpace.Api.dll"]
