using System.Text.Json;
using Ekiphan.Infrastructure.CatalogPdfImport;
using Ekiphan.Infrastructure.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Deliberately local-only maintenance: no application startup, seeding or remote storage.
var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__EkiphanDatabase")
    ?? throw new InvalidOperationException("Local connection configuration is required."));
if (!string.Equals(connection.DataSource, @".\SQLEXPRESS", StringComparison.OrdinalIgnoreCase)
    || connection.InitialCatalog != "EkiphanDevelopmentLocal"
    || Environment.GetEnvironmentVariable("MediaStorage__Provider") != "Local")
    throw new InvalidOperationException("Only LOCAL SQLEXPRESS / EkiphanDevelopmentLocal / Local storage is allowed.");
if (!args.Contains("--apply")) throw new ArgumentException("Pass --apply to backfill missing local covers.");
var root = Environment.GetEnvironmentVariable("MediaStorage__LocalRoot")
    ?? throw new InvalidOperationException("Local media root is required.");
await using var db = new EkiphanDbContext(new DbContextOptionsBuilder<EkiphanDbContext>()
    .UseSqlServer(connection.ConnectionString).Options);
using var logging = LoggerFactory.Create(builder => builder.AddConsole());
var result = await new CatalogPdfImportService(db, new LocalMediaFileStorage(root),
    logger: logging.CreateLogger<CatalogPdfImportService>()).BackfillCoversAsync();
Console.WriteLine(JsonSerializer.Serialize(result));
