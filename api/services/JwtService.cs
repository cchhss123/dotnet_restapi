using System;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        var jwtSettings = _configuration.GetSection("Jwt");
        _secretKey = jwtSettings["Key"] ?? throw new ArgumentNullException("JWT Key is not configured.");
        _issuer = jwtSettings["Issuer"] ?? throw new ArgumentNullException("JWT Issuer is not configured.");
        _audience = jwtSettings["Audience"] ?? throw new ArgumentNullException("JWT Audience is not configured.");
    }

    /// <summary>
    /// Generates a JWT token based on the provided claims.
    /// </summary>
    /// <param name="claims">A collection of claims to include in the token.</param>
    /// <returns>A signed JWT token string.</returns>
    public string GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(30), // Token 30 分鐘後過期
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // You could add other methods here if needed, e.g., for token validation outside of middleware.
}