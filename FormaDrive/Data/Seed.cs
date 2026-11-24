using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Data;

public static class Seed
{
    public static async Task Run(FleetDb db, IConfiguration config)
    {
        await db.Database.MigrateAsync();
        var email = config["Staff:Email"]?.Trim().ToLowerInvariant();
        var password = config["Staff:Password"];
        if (!string.IsNullOrEmpty(email) && password?.Length >= 12 && !await db.Members.AnyAsync(u => u.Email == email))
        {
            var member = new Member { Name = "Сотрудник проката", Email = email, Staff = true };
            member.PasswordHash = new PasswordHasher<Member>().HashPassword(member, password);
            db.Members.Add(member);
        }
        await db.SaveChangesAsync();
    }
}
