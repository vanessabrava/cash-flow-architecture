FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CashFlowArchitecture.slnx ./
COPY dotnet-tools.json ./
COPY src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj src/CashFlowArchitecture.Api/

RUN dotnet restore src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj
RUN dotnet tool restore

COPY src/CashFlowArchitecture.Api src/CashFlowArchitecture.Api

RUN dotnet publish src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migrations
WORKDIR /src

ENTRYPOINT ["dotnet", "tool", "run", "dotnet-ef", "database", "update", "--project", "src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj", "--startup-project", "src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CashFlowArchitecture.Api.dll"]
