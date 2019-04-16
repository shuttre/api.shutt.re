FROM microsoft/dotnet:2.2-sdk AS build-env-dotnet

# Copy csproj and restore projects
WORKDIR /app/sqldb.shutt.re
COPY sqldb.shutt.re/*.csproj ./
RUN dotnet restore

WORKDIR /app/api.shutt.re
COPY api.shutt.re/*.csproj ./
RUN dotnet restore


# Copy everything else and build
WORKDIR /app/sqldb.shutt.re
COPY sqldb.shutt.re ./
RUN dotnet build

WORKDIR /app/api.shutt.re
COPY api.shutt.re ./
RUN dotnet publish -c Release -o /app/out


# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env-dotnet /app/out .

ENTRYPOINT ["dotnet", "api.shutt.re.dll"]