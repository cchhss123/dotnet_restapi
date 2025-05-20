using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 加入 Configuration
builder.Services.AddSingleton(builder.Configuration);

// 註冊 DatabaseService
builder.Services.AddScoped<DatabaseService>();

// 註冊 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

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
});

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
});

app.Run();

// 定義 UserDto
public record UserDto(string Name, string Email);
