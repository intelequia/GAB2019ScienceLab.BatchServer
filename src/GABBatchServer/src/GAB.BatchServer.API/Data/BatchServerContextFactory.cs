using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GAB.BatchServer.API.Data
{
    /// <inheritdoc />
    public class BatchServerContextFactory : IDesignTimeDbContextFactory<BatchServerContext>
    {
        /// <inheritdoc />
        public BatchServerContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var optionsBuilder = new DbContextOptionsBuilder<BatchServerContext>();
            var connectionString = configuration.GetConnectionString("BatchServer");
            optionsBuilder.UseSqlServer(connectionString);
            return new BatchServerContext(optionsBuilder.Options);
        }
    }
}
