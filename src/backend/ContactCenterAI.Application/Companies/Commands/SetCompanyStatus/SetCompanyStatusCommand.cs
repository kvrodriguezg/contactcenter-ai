using ContactCenterAI.Application.Companies.DTOs;
using ContactCenterAI.Domain.Tenancy;
using MediatR;

namespace ContactCenterAI.Application.Companies.Commands.SetCompanyStatus;

public record SetCompanyStatusCommand(Guid Id, CompanyStatus Status) : IRequest<CompanyDto>;
