using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Data;

public static class IdentitySeeder
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed role '{role}': {FormatErrors(result)}");
                }
            }
        }

        await SeedUserAsync(userManager, context, configuration, logger, RoleNames.Astronaut);
        await SeedUserAsync(userManager, context, configuration, logger, RoleNames.Scientist);
        await SeedUserAsync(userManager, context, configuration, logger, RoleNames.Manager);
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger logger,
        string role)
    {
        var section = configuration.GetSection($"SeedUsers:{role}");
        var userName = section["UserName"];
        var email = section["Email"] ?? userName;
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("No seed credentials configured for {Role}; role was seeded without a default user.", role);
            return;
        }

        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user '{userName}': {FormatErrors(createResult)}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{role}' to '{userName}': {FormatErrors(roleResult)}");
            }
        }

        await LinkStaffProfileAsync(context, configuration, logger, role, user);
    }

    private static async Task LinkStaffProfileAsync(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger logger,
        string role,
        ApplicationUser user)
    {
        var section = configuration.GetSection($"SeedUsers:{role}");
        var staffProfileId = TryParseProfileId(section["StaffProfileId"]);
        var staffFullName = section["StaffFullName"];

        if (staffProfileId == null && string.IsNullOrWhiteSpace(staffFullName))
        {
            return;
        }

        switch (role)
        {
            case RoleNames.Astronaut:
                await LinkAstronautProfileAsync(context, logger, user, staffProfileId, staffFullName);
                break;

            case RoleNames.Scientist:
                await LinkScientistProfileAsync(context, logger, user, staffProfileId, staffFullName);
                break;

            case RoleNames.Manager:
                await LinkManagerProfileAsync(context, logger, user, staffProfileId, staffFullName);
                break;
        }
    }

    private static async Task LinkAstronautProfileAsync(
        ApplicationDbContext context,
        ILogger logger,
        ApplicationUser user,
        int? staffProfileId,
        string? staffFullName)
    {
        var profile = staffProfileId.HasValue
            ? await context.Astronauts.FindAsync(staffProfileId.Value)
            : await context.Astronauts.SingleOrDefaultAsync(a => a.FullName == staffFullName);

        if (profile == null)
        {
            LogMissingProfile(logger, RoleNames.Astronaut, staffProfileId, staffFullName);
            return;
        }

        if (!TryLinkProfile(logger, RoleNames.Astronaut, profile.Id, profile.ApplicationUserId, user))
        {
            return;
        }

        profile.ApplicationUserId = user.Id;
        await context.SaveChangesAsync();
    }

    private static async Task LinkScientistProfileAsync(
        ApplicationDbContext context,
        ILogger logger,
        ApplicationUser user,
        int? staffProfileId,
        string? staffFullName)
    {
        var profile = staffProfileId.HasValue
            ? await context.Scientists.FindAsync(staffProfileId.Value)
            : await context.Scientists.SingleOrDefaultAsync(s => s.FullName == staffFullName);

        if (profile == null)
        {
            LogMissingProfile(logger, RoleNames.Scientist, staffProfileId, staffFullName);
            return;
        }

        if (!TryLinkProfile(logger, RoleNames.Scientist, profile.Id, profile.ApplicationUserId, user))
        {
            return;
        }

        profile.ApplicationUserId = user.Id;
        await context.SaveChangesAsync();
    }

    private static async Task LinkManagerProfileAsync(
        ApplicationDbContext context,
        ILogger logger,
        ApplicationUser user,
        int? staffProfileId,
        string? staffFullName)
    {
        var profile = staffProfileId.HasValue
            ? await context.Managers.FindAsync(staffProfileId.Value)
            : await context.Managers.SingleOrDefaultAsync(m => m.FullName == staffFullName);

        if (profile == null)
        {
            LogMissingProfile(logger, RoleNames.Manager, staffProfileId, staffFullName);
            return;
        }

        if (!TryLinkProfile(logger, RoleNames.Manager, profile.Id, profile.ApplicationUserId, user))
        {
            return;
        }

        profile.ApplicationUserId = user.Id;
        await context.SaveChangesAsync();
    }

    private static bool TryLinkProfile(
        ILogger logger,
        string role,
        int profileId,
        string? currentApplicationUserId,
        ApplicationUser user)
    {
        if (currentApplicationUserId == null || currentApplicationUserId == user.Id)
        {
            return true;
        }

        logger.LogWarning(
            "{Role} profile {ProfileId} is already linked to a different application user; leaving it unchanged.",
            role,
            profileId);

        return false;
    }

    private static void LogMissingProfile(
        ILogger logger,
        string role,
        int? staffProfileId,
        string? staffFullName)
    {
        logger.LogWarning(
            "No {Role} profile found for configured seed link. StaffProfileId={StaffProfileId}, StaffFullName={StaffFullName}",
            role,
            staffProfileId,
            staffFullName);
    }

    private static int? TryParseProfileId(string? value)
    {
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}
