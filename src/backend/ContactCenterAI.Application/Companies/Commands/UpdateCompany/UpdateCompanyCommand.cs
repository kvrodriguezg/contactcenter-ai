using ContactCenterAI.Application.Companies.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand(Guid Id, string Name, string Status) : IRequest<CompanyDto>;
