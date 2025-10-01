using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnlineShoppingSystem.Infrastructure.Services;
using OnlineShoppingSystem.Persistence.Services;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly JWTSettings _jwtSettings;
    private readonly RedisRefreshTokenService _redisTokenService;

    public TokenService(
        IConfiguration configuration,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<JWTSettings> jwtOptions,
        RedisRefreshTokenService redisTokenService)
    {
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtSettings = jwtOptions.Value;
        _redisTokenService = redisTokenService;
    }

    public async Task<TokenDto> CreateTokenAsync(AppUser user)
    {
        var claims = await BuildJwtClaimsAsync(user);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            claims: claims,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await _redisTokenService.StoreAsync(refreshToken, user.Id);

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Expires = token.ValidTo
        };
    }

    public async Task<string> GenerateAccessTokenAsync(AppUser user)
    {
        var claims = await BuildJwtClaimsAsync(user);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            claims: claims,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<IList<Claim>> BuildJwtClaimsAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        
        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var uc in userClaims)
        {
            if (uc.Type == Permissions.ClaimType)
                permissionSet.Add(uc.Value);
            else if (uc.Type != ClaimTypes.Role)
                claims.Add(uc);
        }

       
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var rc in roleClaims)
            {
                if (rc.Type == Permissions.ClaimType)
                    permissionSet.Add(rc.Value);
            }
        }

        
        claims.AddRange(permissionSet.Select(p => new Claim(Permissions.ClaimType, p)));
        return claims;
    }
}
