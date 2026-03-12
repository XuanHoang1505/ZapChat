using System.Text;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ZapChat.Api.Data;
using ZapChat.Api.Hubs;
using ZapChat.Api.Common.Response;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Services;
using ZapChat.Api.Services.Interfaces;
using ZapChat.Api.Common.Helpers;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ─────────────────────────────────────────
// Database — MySQL
// ─────────────────────────────────────────
var connStr = config.GetConnectionString("Default");
var serverVersion = ServerVersion.AutoDetect(connStr);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, serverVersion)
       .LogTo(Console.WriteLine, LogLevel.Information) // xem query khi dev
       .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
);


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => {
        opt.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),

            ValidateIssuer = true,
            ValidIssuer    = config["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience    = config["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero
        };

        opt.Events = new JwtBearerEvents {
            OnMessageReceived = ctx => {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) &&
                    path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = config
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()!;

builder.Services.AddCors(opt =>
    opt.AddPolicy("ZapChatPolicy", p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()  // bắt buộc cho SignalR
    )
);

// ─────────────────────────────────────────
// Cloudinary
// ─────────────────────────────────────────
builder.Services.AddSingleton(_ => {
    var account = new Account(
        config["Cloudinary:CloudName"],
        config["Cloudinary:ApiKey"],
        config["Cloudinary:ApiSecret"]
    );
    return new Cloudinary(account) { Api = { Secure = true } };
});

// ─────────────────────────────────────────
// SignalR
// ─────────────────────────────────────────
builder.Services.AddSignalR(opt => {
    opt.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ─────────────────────────────────────────
// AutoMapper
// ─────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<JwtHelper>();

// ─────────────────────────────────────────
// DI — Services
// ─────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IUserService, UserService>();
// builder.Services.AddScoped<IMessageService, MessageService>();
// builder.Services.AddScoped<IConversationService, ConversationService>();

// Các service khác (gửi mail, upload ảnh, OTP)
builder.Services.AddScoped<ISendMailService, SendMailService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<OtpService>();

// 
builder.Services.AddTransient<GlobalExceptionHandler>();
// ─────────────────────────────────────────
// Controllers + Swagger
// ─────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opt => {
        opt.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(opt => {
        opt.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var response = ApiResponse<object>.Fail("Dữ liệu không hợp lệ.", errors);
            return new BadRequestObjectResult(response);
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ═════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseCors("ZapChatPolicy");      
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");  // SignalR endpoint

app.Run();
