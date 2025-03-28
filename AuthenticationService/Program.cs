

using AuthenticationService.Data;
using AuthenticationService.Helpers.JwtHelper;
using AuthenticationService.Mapper;
using AuthenticationService.Repositories;
using AuthenticationService.Sevices.AuthSerrvice;
using AuthenticationService.Sevices.ProfileCompletionConsumerService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json.Serialization;
//using EventBus.Implementations;
using DotNetEnv;

namespace AuthenticationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            DotNetEnv.Env.Load();
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            var ConnectionString = Environment.GetEnvironmentVariable("DB-CONNECTION-STRING");

            builder.Services.AddDbContext<AuthenticationDbContext>(options =>

            options.UseSqlServer(
                ConnectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure()
                )
                );

            builder.Services.AddAutoMapper(typeof(MapperProfile));
            builder.Services.AddScoped<IJwtHelper, JwtHelper>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();




            //builder.Services.AddSingleton<RabbitMQConnection>(sp =>
            //{
            //    var config = sp.GetRequiredService<IConfiguration>();
            //    var connection = new RabbitMQConnection(config);
            //    connection.DeclareExchange("labourlink.events", ExchangeType.Direct);
            //    return connection;
            //});
            //builder.Services.AddHostedService<ProfileCompletionConsumer>();





            builder.Services.AddHostedService<ProfileCompletionConsumerService>();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();


            builder.Services.AddSwaggerGen();

            var secretKey = Encoding.UTF8.GetBytes(builder.Configuration["JWT-SECRET-KEY"]);
            var audience = builder.Configuration["JWT-AUDIENCE"];
            var issuer = builder.Configuration["JWT-ISSUER"];


            // Configure JWT Authentication
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
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.Zero
                };
            });


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173") // Allow frontend URL
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials(); // Allow cookies/auth tokens
                    });
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
