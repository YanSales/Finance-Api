using Application.Abstractions.Persistence;
using Contracts.Users;
using MediatR;

namespace Application.Features.Users.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(IUserRepository users)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse?>
{
    public async Task<CurrentUserResponse?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(request.UserId, cancellationToken);

        return user is { IsActive: true }
            ? new CurrentUserResponse(user.Id, user.Email, user.Role.ToString(), user.CreatedAt)
            : null;
    }
}
