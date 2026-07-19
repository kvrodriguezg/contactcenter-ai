using ContactCenterAI.Application.Companies.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(string Name) : IRequest<CompanyDto>;
