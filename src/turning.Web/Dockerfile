# ============================
# Etapa 1: Build
# ============================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copiar csproj y restaurar
COPY turning.Web.csproj ./
RUN dotnet restore turning.Web.csproj

# Copiar todo el resto del proyecto
COPY . ./

# Publicar en Release
RUN dotnet publish turning.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ============================
# Etapa 2: Runtime
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Copiar artefactos publicados
COPY --from=build /app/publish ./

# Variables de entorno para producción
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "turning.Web.dll"]