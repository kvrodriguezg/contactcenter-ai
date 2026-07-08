using ContactCenterAI.Application.Companies.DTOs;
using MediatR;

namespace ContactCenterAI.Application.Companies.Queries.ListCompanies;

public record ListCompaniesQuery : IRequest<IReadOnlyList<CompanyDto>>;
