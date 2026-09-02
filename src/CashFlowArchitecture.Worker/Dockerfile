FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/CashFlowArchitecture.Core/CashFlowArchitecture.Core.csproj src/CashFlowArchitecture.Core/
COPY src/CashFlowArchitecture.Infrastructure/CashFlowArchitecture.Infrastructure.csproj src/CashFlowArchitecture.Infrastructure/
COPY src/CashFlowArchitecture.Worker/CashFlowArchitecture.Worker.csproj src/CashFlowArchitecture.Worker/

RUN dotnet restore src/CashFlowArchitecture.Worker/CashFlowArchitecture.Worker.csproj

COPY src/CashFlowArchitecture.Core src/CashFlowArchitecture.Core
COPY src/CashFlowArchitecture.Infrastructure src/CashFlowArchitecture.Infrastructure
COPY src/CashFlowArchitecture.Worker src/CashFlowArchitecture.Worker

RUN dotnet publish src/CashFlowArchitecture.Worker/CashFlowArchitecture.Worker.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CashFlowArchitecture.Worker.dll"]
