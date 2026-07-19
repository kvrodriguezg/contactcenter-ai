using ContactCenterAI.Application.Companies.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Companies.Queries.GetCompanyById;

public record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyDto>;
