using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Application.Documents.Commands.UploadDocument;
using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Application.Tests.Documents;

public class UploadDocumentPublishTests
{
    private sealed class FakeDocumentStorage : IDocumentStorageService
    {
        public Task<string> SaveAsync(
            Guid companyId,
            Guid documentId,
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"companies/{companyId}/{documentId}/{fileName}");

        public string GetFullPath(string storagePath) => storagePath;

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"docs-{Guid.NewGuid()}")
            .Options);

    private static (Company company, User user) SeedTenant(TestApplicationDbContext context)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "agent@acme.com",
            Name = "Agent",
            Role = Role.CompanyAdmin,
            CompanyId = company.Id,
            IsActive = true,
            AuthenticationProvider = AuthenticationProvider.Local,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.Users.Add(user);
        context.SaveChanges();
        return (company, user);
    }

    private static UploadDocumentCommandHandler CreateHandler(
        TestApplicationDbContext context,
        User user,
        FakeEventPublisher publisher,
        bool messagingEnabled)
    {
        var currentUser = new TestCurrentUserService
        {
            UserId = user.Id,
            Role = user.Role,
            CompanyId = user.CompanyId,
            Email = user.Email,
            IsAuthenticated = true
        };

        return new UploadDocumentCommandHandler(
            context,
            currentUser,
            new FakeDocumentStorage(),
            publisher,
            Options.Create(new MessagingSettings { Enabled = messagingEnabled }),
            NullLogger<UploadDocumentCommandHandler>.Instance);
    }

    [Fact]
    public async Task Publishes_DocumentUploadedEvent_after_persist_when_messaging_enabled()
    {
        await using var context = CreateContext();
        var (company, user) = SeedTenant(context);
        var publisher = new FakeEventPublisher();
        var handler = CreateHandler(context, user, publisher, messagingEnabled: true);

        await using var stream = new MemoryStream("pdf"u8.ToArray());
        var result = await handler.Handle(
            new UploadDocumentCommand(stream, "doc.pdf", "application/pdf", stream.Length, null),
            CancellationToken.None);

        var published = Assert.IsType<DocumentUploadedEvent>(Assert.Single(publisher.Published));
        Assert.Equal(result.Id, published.DocumentId);
        Assert.Equal(company.Id, published.CompanyId);
        Assert.Equal(user.Id, published.UploadedByUserId);
        Assert.NotEqual(Guid.Empty, published.CorrelationId);

        var document = await context.Documents.SingleAsync(d => d.Id == result.Id);
        Assert.Equal(DocumentStatus.PendingProcessing, document.Status);
    }

    [Fact]
    public async Task Messaging_disabled_still_persists_and_publishes_via_noop_path()
    {
        await using var context = CreateContext();
        var (_, user) = SeedTenant(context);
        var publisher = new FakeEventPublisher();
        var handler = CreateHandler(context, user, publisher, messagingEnabled: false);

        await using var stream = new MemoryStream("pdf"u8.ToArray());
        var result = await handler.Handle(
            new UploadDocumentCommand(stream, "doc.pdf", "application/pdf", stream.Length, null),
            CancellationToken.None);

        Assert.Single(publisher.Published);
        var document = await context.Documents.SingleAsync(d => d.Id == result.Id);
        Assert.Equal(DocumentStatus.Uploaded, document.Status);
    }

    [Fact]
    public async Task Publish_failure_does_not_lose_persisted_document()
    {
        await using var context = CreateContext();
        var (_, user) = SeedTenant(context);
        var publisher = new FakeEventPublisher
        {
            ThrowOnPublish = new InvalidOperationException("RabbitMQ unavailable")
        };
        var handler = CreateHandler(context, user, publisher, messagingEnabled: true);

        await using var stream = new MemoryStream("pdf"u8.ToArray());
        var result = await handler.Handle(
            new UploadDocumentCommand(stream, "doc.pdf", "application/pdf", stream.Length, null),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(await context.Documents.AnyAsync(d => d.Id == result.Id));
        Assert.Empty(publisher.Published);
    }
}
