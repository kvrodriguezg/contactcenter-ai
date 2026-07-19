using ContactCenterAI.Application.Tests.Common;
using ContactCenterAI.Application.Tickets.Commands.AssignTicket;
using ContactCenterAI.Application.Tickets.Commands.CreateTicket;
using ContactCenterAI.Application.Tickets.Commands.ResolveTicket;
using ContactCenterAI.Application.Tickets.Queries.GetTicketById;
using ContactCenterAI.Application.Tickets.Queries.ListTickets;
using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using ContactCenterAI.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Tickets;

public class TicketCommandTests
{
    private static TestApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase($"tickets-{Guid.NewGuid()}")
            .Options);

    private static Company SeedCompany(TestApplicationDbContext context, string name = "Acme")
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.SaveChanges();
        return company;
    }

    private static User SeedUser(
        TestApplicationDbContext context,
        Guid companyId,
        Role role,
        string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = email.Split('@')[0],
            Role = role,
            CompanyId = companyId,
            IsActive = true,
            AuthenticationProvider = AuthenticationProvider.Local,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private static Ticket SeedTicket(
        TestApplicationDbContext context,
        Guid companyId,
        Guid createdByUserId,
        string subject = "Ticket")
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedByUserId = createdByUserId,
            Subject = subject,
            Description = "Descripción",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Tickets.Add(ticket);
        context.SaveChanges();
        return ticket;
    }

    private static Conversation SeedConversation(
        TestApplicationDbContext context,
        Guid companyId,
        Guid userId)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId,
            Title = "Chat",
            CreatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        context.SaveChanges();
        return conversation;
    }

    private static TestCurrentUserService AsAgentUser(User agent) => new()
    {
        UserId = agent.Id,
        Role = Role.Agent,
        CompanyId = agent.CompanyId,
        Email = agent.Email,
        IsAuthenticated = true
    };

    private static TestCurrentUserService AsCompanyAdminUser(User admin) => new()
    {
        UserId = admin.Id,
        Role = Role.CompanyAdmin,
        CompanyId = admin.CompanyId,
        Email = admin.Email,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Agent_creates_ticket_for_own_company()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var agent = SeedUser(context, company.Id, Role.Agent, "agent@acme.com");
        var publisher = new FakeTicketEventPublisher();
        var handler = new CreateTicketCommandHandler(context, AsAgentUser(agent), publisher);

        var result = await handler.Handle(
            new CreateTicketCommand("Escalación", "No se resolvió en chat", nameof(TicketPriority.High)),
            CancellationToken.None);

        Assert.Equal(company.Id, result.CompanyId);
        Assert.Equal(agent.Id, result.CreatedByUserId);
        Assert.Equal(nameof(TicketStatus.Pending), result.Status);
        Assert.Equal(nameof(TicketPriority.High), result.Priority);
        Assert.Single(publisher.Published);
        Assert.Equal(result.Id, publisher.Published[0].TicketId);
        Assert.Equal(nameof(TicketPriority.High), publisher.Published[0].Priority);
        Assert.NotEqual(Guid.Empty, publisher.Published[0].CorrelationId);
    }

    [Fact]
    public async Task External_CompanyId_is_rejected()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var agent = SeedUser(context, companyA.Id, Role.Agent, "agent@a.com");
        var handler = new CreateTicketCommandHandler(
            context,
            AsAgentUser(agent),
            new FakeTicketEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new CreateTicketCommand(
                    "Asunto",
                    "Descripción",
                    nameof(TicketPriority.Low),
                    ConversationId: null,
                    CompanyId: companyB.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Agent_company_A_cannot_see_ticket_of_company_B()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var agentA = SeedUser(context, companyA.Id, Role.Agent, "agent@a.com");
        var agentB = SeedUser(context, companyB.Id, Role.Agent, "agent@b.com");
        SeedTicket(context, companyB.Id, agentB.Id, "Ticket B");

        var handler = new ListTicketsQueryHandler(context, AsAgentUser(agentA));
        var result = await handler.Handle(new ListTicketsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CompanyAdmin_company_A_cannot_manage_ticket_of_company_B()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var adminA = SeedUser(context, companyA.Id, Role.CompanyAdmin, "admin@a.com");
        var agentB = SeedUser(context, companyB.Id, Role.Agent, "agent@b.com");
        var ticketB = SeedTicket(context, companyB.Id, agentB.Id);

        var handler = new AssignTicketCommandHandler(context, AsCompanyAdminUser(adminA));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AssignTicketCommand(ticketB.Id, adminA.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task SuperAdmin_sees_all_tickets()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var agentA = SeedUser(context, companyA.Id, Role.Agent, "agent@a.com");
        var agentB = SeedUser(context, companyB.Id, Role.Agent, "agent@b.com");
        SeedTicket(context, companyA.Id, agentA.Id, "A");
        SeedTicket(context, companyB.Id, agentB.Id, "B");

        var superAdmin = TestCurrentUserService.AsSuperAdmin();
        superAdmin.UserId = Guid.NewGuid();
        var handler = new ListTicketsQueryHandler(context, superAdmin);

        var result = await handler.Handle(new ListTicketsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Agent_cannot_assign_ticket()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var agent = SeedUser(context, company.Id, Role.Agent, "agent@acme.com");
        var other = SeedUser(context, company.Id, Role.Agent, "other@acme.com");
        var ticket = SeedTicket(context, company.Id, agent.Id);

        var handler = new AssignTicketCommandHandler(context, AsAgentUser(agent));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new AssignTicketCommand(ticket.Id, other.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CompanyAdmin_assigns_user_of_own_company()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var admin = SeedUser(context, company.Id, Role.CompanyAdmin, "admin@acme.com");
        var agent = SeedUser(context, company.Id, Role.Agent, "agent@acme.com");
        var ticket = SeedTicket(context, company.Id, agent.Id);

        var handler = new AssignTicketCommandHandler(context, AsCompanyAdminUser(admin));
        var result = await handler.Handle(
            new AssignTicketCommand(ticket.Id, agent.Id),
            CancellationToken.None);

        Assert.Equal(agent.Id, result.AssignedToUserId);
        Assert.Equal(agent.Email, result.AssignedToEmail);
    }

    [Fact]
    public async Task CompanyAdmin_cannot_assign_user_of_other_company()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var adminA = SeedUser(context, companyA.Id, Role.CompanyAdmin, "admin@a.com");
        var agentA = SeedUser(context, companyA.Id, Role.Agent, "agent@a.com");
        var agentB = SeedUser(context, companyB.Id, Role.Agent, "agent@b.com");
        var ticket = SeedTicket(context, companyA.Id, agentA.Id);

        var handler = new AssignTicketCommandHandler(context, AsCompanyAdminUser(adminA));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new AssignTicketCommand(ticket.Id, agentB.Id), CancellationToken.None));
    }

    [Fact]
    public async Task ResolveTicket_saves_Resolution_and_ResolvedAt()
    {
        await using var context = CreateContext();
        var company = SeedCompany(context);
        var admin = SeedUser(context, company.Id, Role.CompanyAdmin, "admin@acme.com");
        var agent = SeedUser(context, company.Id, Role.Agent, "agent@acme.com");
        var ticket = SeedTicket(context, company.Id, agent.Id);

        var handler = new ResolveTicketCommandHandler(context, AsCompanyAdminUser(admin));
        var before = DateTime.UtcNow;

        var result = await handler.Handle(
            new ResolveTicketCommand(ticket.Id, "Problema corregido en KB"),
            CancellationToken.None);

        Assert.Equal("Problema corregido en KB", result.Resolution);
        Assert.Equal(nameof(TicketStatus.Resolved), result.Status);
        Assert.NotNull(result.ResolvedAt);
        Assert.True(result.ResolvedAt >= before);
    }

    [Fact]
    public async Task Foreign_ConversationId_is_rejected()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var agentA = SeedUser(context, companyA.Id, Role.Agent, "agent@a.com");
        var agentB = SeedUser(context, companyB.Id, Role.Agent, "agent@b.com");
        var conversationB = SeedConversation(context, companyB.Id, agentB.Id);

        var handler = new CreateTicketCommandHandler(
            context,
            AsAgentUser(agentA),
            new FakeTicketEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new CreateTicketCommand(
                    "Asunto",
                    "Descripción",
                    nameof(TicketPriority.Medium),
                    conversationB.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task GetTicketById_respects_company_isolation()
    {
        await using var context = CreateContext();
        var companyA = SeedCompany(context, "A");
        var companyB = SeedCompany(context, "B");
        var agentA = SeedUser(context, companyA.Id, Role.Agent, "agent@a.com");
        var agentB = SeedUser(context, companyB.Id, Role.Agent, "agent@b.com");
        var ticketB = SeedTicket(context, companyB.Id, agentB.Id);

        var handler = new GetTicketByIdQueryHandler(context, AsAgentUser(agentA));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetTicketByIdQuery(ticketB.Id), CancellationToken.None));
    }
}
