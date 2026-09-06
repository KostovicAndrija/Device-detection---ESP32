using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Auth;

public sealed class AuthBootstrapper(
    AppDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    IOptions<DevelopmentUserOptions> options)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        var username = configured.Username.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Username == username, cancellationToken))
        {
            return;
        }

        var user = AppUser.Create(username);
        user.SetPasswordHash(passwordHasher.HashPassword(user, configured.Password));
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
