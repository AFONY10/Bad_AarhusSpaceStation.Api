using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Dtos.Auth;
using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        IJwtTokenService jwtTokenService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _jwtTokenService = jwtTokenService;
        _context = context;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName)
            ?? await _userManager.FindByEmailAsync(dto.UserName);

        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = await _jwtTokenService.CreateTokenAsync(user);
        var staffProfiles = await GetStaffProfilesAsync(user.Id);

        return Ok(new LoginResponseDto
        {
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Roles = token.Roles,
            StaffProfiles = staffProfiles
        });
    }

    private async Task<List<StaffProfileDto>> GetStaffProfilesAsync(string applicationUserId)
    {
        var profiles = new List<StaffProfileDto>();

        var astronautProfile = await _context.Astronauts
            .AsNoTracking()
            .Where(a => a.ApplicationUserId == applicationUserId)
            .Select(a => new StaffProfileDto
            {
                ProfileType = "Astronaut",
                Id = a.Id,
                FullName = a.FullName
            })
            .SingleOrDefaultAsync();

        if (astronautProfile != null)
        {
            profiles.Add(astronautProfile);
        }

        var scientistProfile = await _context.Scientists
            .AsNoTracking()
            .Where(s => s.ApplicationUserId == applicationUserId)
            .Select(s => new StaffProfileDto
            {
                ProfileType = "Scientist",
                Id = s.Id,
                FullName = s.FullName
            })
            .SingleOrDefaultAsync();

        if (scientistProfile != null)
        {
            profiles.Add(scientistProfile);
        }

        var managerProfile = await _context.Managers
            .AsNoTracking()
            .Where(m => m.ApplicationUserId == applicationUserId)
            .Select(m => new StaffProfileDto
            {
                ProfileType = "Manager",
                Id = m.Id,
                FullName = m.FullName
            })
            .SingleOrDefaultAsync();

        if (managerProfile != null)
        {
            profiles.Add(managerProfile);
        }

        return profiles;
    }
}
