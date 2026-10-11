using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SellFlowApi.Data;
using SellFlowApi.Dtos;
using SellFlowApi.Exceptions;
using SellFlowApi.Models;
 using Microsoft.EntityFrameworkCore;
using SellFlowApi.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace SellFlowApi.Implementation
{
   public class AuthService(SellFlowApiDbContext db, IJwtTokenService jwt, IPasswordHasher<User> hasher) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest r)
    {
        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        if (!email.Contains('@') || (r.Password ?? "").Length < 8)
            throw new DomainException("A valid email and a password of at least 8 characters are required.");
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new DomainException("This email is already registered.");

        var user = new User
        {
            FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Email = email
        };
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 8)
            throw new DomainException("Password must be at least 8 characters.");
        user.PasswordHash = hasher.HashPassword(user, r.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        // TODO: email/phone verification before allowing business creation
        return new AuthResponse(jwt.Create(user, null), null, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest r)
    {
        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
            throw new DomainException("Invalid email or password.");   // same message for both cases

        return await BuildAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");
        return await BuildAsync(user);
    }

    private async Task<AuthResponse> BuildAsync(User user)
    {
        var businessId = await db.Businesses.Where(b => b.OwnerId == user.Id)
            .Select(b => (Guid?)b.Id).FirstOrDefaultAsync();
        return new AuthResponse(jwt.Create(user, businessId), businessId, user.Email);
    }
}

}