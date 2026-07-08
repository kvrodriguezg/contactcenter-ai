using ContactCenterAI.Application.Auth.DTOs;
using ContactCenterAI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        if (!_passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name
        };
    }
}
