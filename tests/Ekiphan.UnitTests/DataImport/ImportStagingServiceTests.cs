using System.Text;
using Ekiphan.Application.DataImport;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportStagingServiceTests
{
    [Fact]
    public async Task StageAsyncCompletesValidDryRunAndPersistsOnce()
    {
        var repository = new StubRepository();
        var service = CreateService(
            repository,
            Document(
                new Dictionary<string, string?>
                {
                    ["Ürün Kodu"] = "abc-1",
                    ["Ürün Adı"] = "Tabak"
                }));

        var job = await service.StageAsync(Command("source-a"));

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Equal(ImportSourceType.Csv, job.SourceType);
        Assert.Equal(1, job.ValidRowCount);
        Assert.Equal("ABC-1", Assert.Single(job.Rows).SKU);
        Assert.Equal(1, repository.SaveCount);
        Assert.Same(job, repository.AddedJob);
    }

    [Fact]
    public async Task StageAsyncRecordsRequiredFieldIssues()
    {
        var repository = new StubRepository();
        var service = CreateService(
            repository,
            Document(
                new Dictionary<string, string?>
                {
                    ["Marka"] = "Ekiphan"
                }));

        var job = await service.StageAsync(Command("source-b"));

        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Equal(1, job.InvalidRowCount);
        Assert.Equal(
            ["SKU_REQUIRED", "PRODUCT_NAME_REQUIRED"],
            Assert.Single(job.Rows).Issues.Select(issue => issue.Code));
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task StageAsyncRejectsDuplicateBeforeReadingOrSaving()
    {
        var repository = new StubRepository
        {
            SourceExists = true
        };
        var reader = new StubReader(
            Document(new Dictionary<string, string?>()));
        var service = new ImportStagingService(
            reader,
            repository,
            new StubReferenceResolver(),
            TimeProvider.System);

        await Assert.ThrowsAsync<DuplicateImportSourceException>(
            () => service.StageAsync(Command("same-content")));

        Assert.Equal(0, reader.ReadCount);
        Assert.Equal(0, repository.SaveCount);
        Assert.Null(repository.AddedJob);
    }

    [Fact]
    public async Task StageAsyncPersistsFailedJobWhenReaderRejectsFile()
    {
        var repository = new StubRepository();
        var reader = new StubReader(
            new ImportFileReadException("Header names cannot be empty."));
        var service = new ImportStagingService(
            reader,
            repository,
            new StubReferenceResolver(),
            TimeProvider.System);

        var job = await service.StageAsync(Command("bad-content"));

        Assert.Equal(ImportJobStatus.Failed, job.Status);
        Assert.Equal("Header names cannot be empty.", job.FailureReason);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task StageAsyncProducesStableChecksumForSameBytes()
    {
        var firstRepository = new StubRepository();
        var secondRepository = new StubRepository();
        var document = Document(
            new Dictionary<string, string?>
            {
                ["SKU"] = "A",
                ["Ürün Adı"] = "Ürün"
            });

        var first = await CreateService(firstRepository, document)
            .StageAsync(Command("stable"));
        var second = await CreateService(secondRepository, document)
            .StageAsync(Command("stable"));

        Assert.Equal(first.SourceSha256Checksum, second.SourceSha256Checksum);
        Assert.Equal(64, first.SourceSha256Checksum.Length);
    }

    [Fact]
    public async Task StageAsyncMarksEveryDuplicateSkuRowInvalid()
    {
        var repository = new StubRepository();
        var document = new TabularImportDocument(
            [
                new TabularImportSheet(
                    "Products",
                    ["SKU", "Ürün Adı"],
                    [
                        new TabularImportRow(
                            2,
                            new Dictionary<string, string?>
                            {
                                ["SKU"] = "DUP-1",
                                ["Ürün Adı"] = "Bir"
                            }),
                        new TabularImportRow(
                            3,
                            new Dictionary<string, string?>
                            {
                                ["SKU"] = "dup-1",
                                ["Ürün Adı"] = "İki"
                            })
                    ])
            ]);

        var job = await CreateService(repository, document)
            .StageAsync(Command("duplicate-source"));

        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Equal(2, job.InvalidRowCount);
        Assert.All(
            job.Rows,
            row => Assert.Contains(
                row.Issues,
                issue => issue.Code == "DUPLICATE_SKU_IN_SOURCE"));
    }

    [Fact]
    public async Task StageAsyncRejectsUnknownBrandAndCategory()
    {
        var repository = new StubRepository();
        var document = Document(
            new Dictionary<string, string?>
            {
                ["SKU"] = "REF-1",
                ["Ürün Adı"] = "Ürün",
                ["Marka"] = "Bilinmeyen",
                ["Kategori"] = "Olmayan"
            });

        var job = await CreateService(repository, document)
            .StageAsync(Command("unknown-references"));

        var row = Assert.Single(job.Rows);
        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Contains(row.Issues, issue => issue.Code == "UNKNOWN_BRAND");
        Assert.Contains(row.Issues, issue => issue.Code == "UNKNOWN_CATEGORY");
    }

    [Fact]
    public async Task StageAsyncEnrichesResolvedReferenceIdentifiers()
    {
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var materialAttributeId = Guid.NewGuid();
        var materialOptionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var repository = new StubRepository();
        var document = Document(
            new Dictionary<string, string?>
            {
                ["SKU"] = "REF-2",
                ["Ürün Adı"] = "Ürün",
                ["Marka"] = "Ekiphan",
                ["Kategori"] = "Servis",
                ["Malzeme"] = "Çelik",
                ["Etiketler"] = "Premium"
            });
        var resolver = new StubReferenceResolver(
            new ImportReferenceResolution(
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Ekiphan"] = brandId
                },
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Servis"] = categoryId
                },
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                materialAttributeId,
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Çelik"] = materialOptionId
                },
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Premium"] = tagId
                },
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)));
        var service = new ImportStagingService(
            new StubReader(document),
            repository,
            resolver,
            TimeProvider.System);

        var job = await service.StageAsync(Command("resolved-references"));

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        var payload = Assert.Single(job.Rows).NormalizedPayload!;
        Assert.Contains(brandId.ToString(), payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(categoryId.ToString(), payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            materialAttributeId.ToString(),
            payload,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            materialOptionId.ToString(),
            payload,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(tagId.ToString(), payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StageAsyncRejectsAmbiguousTurkishCategoryName()
    {
        var repository = new StubRepository();
        var document = Document(
            new Dictionary<string, string?>
            {
                ["SKU"] = "AMB-1",
                ["Ürün Adı"] = "Ürün",
                ["Kategori"] = "Servis"
            });
        var resolver = new StubReferenceResolver(
            new ImportReferenceResolution(
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(
                    ["Servis"],
                    StringComparer.OrdinalIgnoreCase),
                null,
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)));
        var service = new ImportStagingService(
            new StubReader(document),
            repository,
            resolver,
            TimeProvider.System);

        var job = await service.StageAsync(Command("ambiguous-category"));

        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Contains(
            Assert.Single(job.Rows).Issues,
            issue => issue.Code == "AMBIGUOUS_CATEGORY");
    }

    [Fact]
    public async Task StageAsyncRejectsUnknownMaterialOption()
    {
        var repository = new StubRepository();
        var document = Document(
            new Dictionary<string, string?>
            {
                ["SKU"] = "MAT-1",
                ["Ürün Adı"] = "Ürün",
                ["Malzeme"] = "Tanımsız Alaşım"
            });
        var resolver = new StubReferenceResolver(
            new ImportReferenceResolution(
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase),
                Guid.NewGuid(),
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Guid>(
                    StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)));
        var service = new ImportStagingService(
            new StubReader(document),
            repository,
            resolver,
            TimeProvider.System);

        var job = await service.StageAsync(Command("unknown-material"));

        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Contains(
            Assert.Single(job.Rows).Issues,
            issue => issue.Code == "UNKNOWN_MATERIAL");
    }

    [Fact]
    public async Task StageAsyncRejectsUnknownTag()
    {
        var repository = new StubRepository();
        var document = Document(
            new Dictionary<string, string?>
            {
                ["SKU"] = "TAG-1",
                ["Ürün Adı"] = "Ürün",
                ["Etiketler"] = "Tanımsız"
            });

        var job = await CreateService(repository, document)
            .StageAsync(Command("unknown-tag"));

        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Contains(
            Assert.Single(job.Rows).Issues,
            issue => issue.Code == "UNKNOWN_TAG");
    }

    private static ImportStagingService CreateService(
        StubRepository repository,
        TabularImportDocument document) =>
        new(
            new StubReader(document),
            repository,
            new StubReferenceResolver(),
            TimeProvider.System);

    private static StageImportFileCommand Command(string content) =>
        new(
            new MemoryStream(Encoding.UTF8.GetBytes(content)),
            "products.csv",
            IsDryRun: true);

    private static TabularImportDocument Document(
        IReadOnlyDictionary<string, string?> values) =>
        new(
            [
                new TabularImportSheet(
                    "Products",
                    values.Keys.ToArray(),
                    [new TabularImportRow(2, values)])
            ]);

    private sealed class StubReader : ITabularImportFileReader
    {
        private readonly TabularImportDocument? _document;
        private readonly ImportFileReadException? _exception;

        public StubReader(TabularImportDocument document)
        {
            _document = document;
        }

        public StubReader(ImportFileReadException exception)
        {
            _exception = exception;
        }

        public int ReadCount { get; private set; }

        public Task<TabularImportDocument> ReadAsync(
            Stream content,
            string fileName,
            ImportFileReadOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_document!);
        }
    }

    private sealed class StubRepository : IImportJobRepository
    {
        public bool SourceExists { get; init; }

        public int SaveCount { get; private set; }

        public ImportJob? AddedJob { get; private set; }

        public Task<bool> SourceExistsAsync(
            string sourceSha256Checksum,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SourceExists);

        public void Add(ImportJob job)
        {
            AddedJob = job;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubReferenceResolver(
        ImportReferenceResolution? resolution = null)
        : IImportReferenceResolver
    {
        public Task<ImportReferenceResolution> ResolveAsync(
            IReadOnlyCollection<string> brandNames,
            IReadOnlyCollection<string> categoryNames,
            IReadOnlyCollection<string> materialNames,
            IReadOnlyCollection<string> tagNames,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                resolution ??
                new ImportReferenceResolution(
                    new Dictionary<string, Guid>(
                        StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, Guid>(
                        StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase),
                    null,
                    new Dictionary<string, Guid>(
                        StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, Guid>(
                        StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase)));
    }
}
