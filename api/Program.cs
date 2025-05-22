using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using System.Security.Claims; // For Claims
using System.Text; // For Encoding
using Microsoft.IdentityModel.Tokens; // For SymmetricSecurityKey
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT Bearer Authentication

var builder = WebApplication.CreateBuilder(args);

// 加入 Configuration
builder.Services.AddSingleton(builder.Configuration);

// 註冊 DatabaseService
builder.Services.AddScoped<DatabaseService>();

// ✅ 註冊 JwtService
builder.Services.AddSingleton<JwtService>();

// ====================================================================================
// ✅ JWT 認證配置 (ASP.NET Core框架處理)
// ====================================================================================
// 獲取 JWT 相關設定 (從 appsettings.json)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var response = new
            {
                error = new
                {
                    code = 401,
                    message = "Unauthorized",
                    details = "Authentication required. Please provide a valid JWT token."
                }
            };
            await context.Response.WriteAsJsonAsync(response);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var response = new
            {
                error = new
                {
                    code = 403,
                    message = "Forbidden",
                    details = "You do not have permission to access this resource."
                }
            };
            await context.Response.WriteAsJsonAsync(response);
        }
    };
});

builder.Services.AddAuthorization(); // 啟用授權

// ====================================================================================

// 註冊 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User API",
        Version = "v1",
        Description = "使用者管理 API，支援 CRUD 操作"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "請輸入 JWT token (格式：Bearer YOUR_TOKEN)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 註冊 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// ====================================================================================
// ✅ 啟用認證和授權中介軟體
// ====================================================================================
app.UseAuthentication();
app.UseAuthorization();
// ====================================================================================

// 啟用 Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "User API v1");
    options.RoutePrefix = "swagger"; // 設定 Swagger UI 在 `/swagger`
});

// ====================================================================================
// 🔹 驗證api帳號 端點 (auth Endpoint) - 使用 JwtService
// ====================================================================================
app.MapPost("/auth", (ApiAuthDto AuthDto, JwtService jwtService) => // ✅ 注入 JwtService
{
    // 模擬驗證 api帳號 (實際應用，可改為從資料庫查詢 api帳號，驗證密碼)
    if (AuthDto.Account == "api" && AuthDto.Password == "test")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"), // 假設使用者 ID 是 1
            new Claim(ClaimTypes.Name, AuthDto.Account),
            new Claim(ClaimTypes.Role, "Admin") // 假設是管理員角色
        };

        var jwtToken = jwtService.GenerateToken(claims); // ✅ 使用 JwtService 生成 token
        return Results.Json(new { message = "Login successful", token = jwtToken });
    }
    else
    {
        return Results.Json(new { error = new { code = 401, message = "Invalid credentials" } }, statusCode: 401);
    }
});

// ====================================================================================

// 🔹 取得所有使用者列表 (GET /users)
app.MapGet("/users", async (DatabaseService dbService) =>
{
    try
    {
        var users = await dbService.QueryAsync(
            "SELECT id, name, email FROM users",
            reader => new { Id = reader.GetInt64(0), Name = reader.GetString(1), Email = reader.GetString(2) }
        );
        return Results.Json(new { message = "Success", data = users });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = new { code = 500, message = "Internal Server Error", details = ex.Message } }, statusCode: 500);
    }
});

// 🔹 透過 ID 查詢使用者 (GET /users/{id})
app.MapGet("/users/{id}", async (long id, DatabaseService dbService) =>
{
    try
    {
        var user = await dbService.QuerySingleAsync(
            "SELECT id, name, email FROM users WHERE id = @id",
            reader => new { Id = reader.GetInt64(0), Name = reader.GetString(1), Email = reader.GetString(2) },
            new SqlParameter("@id", id)
        );

        return user != null
            ? Results.Json(new { message = "Success", data = user })
            : Results.Json(new { error = new { code = 404, message = "User not found", details = $"No user exists with ID {id}" } }, statusCode: 404);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = new { code = 500, message = "Internal Server Error", details = ex.Message } }, statusCode: 500);
    }
});

// 🔹 新增使用者 (POST /users)
app.MapPost("/users", async (UserDto user, DatabaseService dbService) =>
{
    try
    {
        string sql = "INSERT INTO users (name, email) VALUES (@name, @email); SELECT SCOPE_IDENTITY();";
        var newUserId = await dbService.ExecuteScalarAsync(sql,
            new SqlParameter("@name", user.Name),
            new SqlParameter("@email", user.Email));

        return newUserId > 0
            ? Results.Json(new { message = "User created successfully", data = new { id = newUserId, name = user.Name, email = user.Email } }, statusCode: 201)
            : Results.Json(new { error = new { code = 400, message = "Failed to create user" } }, statusCode: 400);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = new { code = 500, message = "Internal Server Error", details = ex.Message } }, statusCode: 500);
    }
});

// 🔹 更新使用者 (PUT /users/{id}) - 保護此端點
app.MapPut("/users/{id}", async (long id, UserDto user, DatabaseService dbService) =>
{
    try
    {
        string sql = "UPDATE users SET name = @name, email = @email WHERE id = @id";
        var rowsAffected = await dbService.ExecuteNonQueryAsync(sql,
            new SqlParameter("@name", user.Name),
            new SqlParameter("@email", user.Email),
            new SqlParameter("@id", id));

        return rowsAffected > 0
            ? Results.Json(new { message = "User updated successfully" })
            : Results.Json(new { error = new { code = 404, message = "User not found" } }, statusCode: 404);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = new { code = 500, message = "Internal Server Error", details = ex.Message } }, statusCode: 500);
    }
}).RequireAuthorization();

// 🔹 刪除使用者 (DELETE /users/{id}) - 保護此端點
app.MapDelete("/users/{id}", async (long id, DatabaseService dbService) =>
{
    try
    {
        string sql = "DELETE FROM users WHERE id = @id";
        var rowsAffected = await dbService.ExecuteNonQueryAsync(sql, new SqlParameter("@id", id));

        return rowsAffected > 0
            ? Results.Json(new { message = "User deleted successfully" })
            : Results.Json(new { error = new { code = 404, message = "User not found" } }, statusCode: 404);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = new { code = 500, message = "Internal Server Error", details = ex.Message } }, statusCode: 500);
    }
}).RequireAuthorization();

app.Run();

// 🔹 定義 UserDto (用於解析 API Request Body)
public record UserDto(string Name, string Email);

// 🔹 定義 ApiAuthDto (用於API驗證請求)
public record ApiAuthDto(string Account, string Password);
