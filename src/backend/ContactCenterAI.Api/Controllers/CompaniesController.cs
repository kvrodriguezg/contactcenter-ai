using ContactCenterAI.Application.Companies.Commands.CreateCompany;
using ContactCenterAI.Application.Companies.Commands.SetCompanyStatus;
using ContactCenterAI.Application.Companies.Commands.UpdateCompany;
using ContactCenterAI.Application.Companies.Queries.GetCompanyById;
using ContactCenterAI.Application.Companies.Queries.ListCompanies;
using ContactCenterAI.Domain.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactCenterAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompaniesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListCompaniesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCompanyById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCompanyCommand(request.Name), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCompany(
        Guid id,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCompanyCommand(id, request.Name, request.Status),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateCompany(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetCompanyStatusCommand(id, CompanyStatus.Active),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCompany(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetCompanyStatusCommand(id, CompanyStatus.Inactive),
            cancellationToken);
        return Ok(result);
    }
}

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateCompanyRequest
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
