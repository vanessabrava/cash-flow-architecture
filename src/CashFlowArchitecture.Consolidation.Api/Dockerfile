FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CashFlowArchitecture.slnx ./
COPY dotnet-tools.json ./
COPY src/CashFlowArchitecture.Core/CashFlowArchitecture.Core.csproj src/CashFlowArchitecture.Core/
COPY src/CashFlowArchitecture.Infrastructure/CashFlowArchitecture.Infrastructure.csproj src/CashFlowArchitecture.Infrastructure/
COPY src/CashFlowArchitecture.Consolidation.Api/CashFlowArchitecture.Consolidation.Api.csproj src/CashFlowArchitecture.Consolidation.Api/

RUN dotnet restore src/CashFlowArchitecture.Consolidation.Api/CashFlowArchitecture.Consolidation.Api.csproj

COPY src/CashFlowArchitecture.Core src/CashFlowArchitecture.Core
COPY src/CashFlowArchitecture.Infrastructure src/CashFlowArchitecture.Infrastructure
COPY src/CashFlowArchitecture.Consolidation.Api src/CashFlowArchitecture.Consolidation.Api

RUN dotnet publish src/CashFlowArchitecture.Consolidation.Api/CashFlowArchitecture.Consolidation.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CashFlowArchitecture.Consolidation.Api.dll"]
