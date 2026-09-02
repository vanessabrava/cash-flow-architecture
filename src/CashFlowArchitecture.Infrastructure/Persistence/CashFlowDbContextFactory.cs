using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CashFlowArchitecture.Infrastructure.Persistence;

internal sealed class CashFlowDbContextFactory : IDesignTimeDbContextFactory<CashFlowDbContext>
{
    public CashFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password";

        var options = new DbContextOptionsBuilder<CashFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CashFlowDbContext(options);
    }
}
