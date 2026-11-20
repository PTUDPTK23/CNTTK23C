# syntax=docker/dockerfile:1
# Shared multi-stage build for every .NET service.
# Build args:
#   PROJECT  - csproj path relative to backend/ (e.g. Auction.API/Auction.API.csproj)
#   ASSEMBLY - assembly name without .dll (e.g. Auction.API)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
ARG ASSEMBLY
WORKDIR /src
COPY . .
RUN dotnet restore "$PROJECT"
RUN dotnet publish "$PROJECT" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG ASSEMBLY
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASSEMBLY=${ASSEMBLY}
ENTRYPOINT ["sh", "-c", "exec dotnet \"$ASSEMBLY.dll\""]
