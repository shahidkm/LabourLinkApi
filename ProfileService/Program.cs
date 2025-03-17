    using CloudinaryDotNet;
using EventBus.Abstractions;
using EventBus.Implementations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;
using ProfileService.Data;
using ProfileService.Helper.CloudinaryHelper;
using ProfileService.Mapper;
using ProfileService.Middlewares;
using ProfileService.Repositories.EmployerRepository;
using ProfileService.Repositories.LabourRepository;
using ProfileService.Repositories.ReviewRepository;
using ProfileService.Services.EmployerService;
using ProfileService.Services.LabourService;

using RabbitMQ.Client;

using ProfileService.Services.ReviewService;

using System.Text;
using System.Text.Json.Serialization;
using ProfileService.Repositories.LabourWithinEmployer;
using ProfileService.Repositories.LabourPrefferedRepositorys;
using ProfileService.Repositories.ChatConversationRepository;
using ProfileService.Services.ConversationService;


namespace ProfileService
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

            var ConnectionString = Environment.GetEnvironmentVariable("LABOURLINK-DB");

            Console.WriteLine(ConnectionString);
            // Add services to the container.

            builder.Services.AddDbContext<LabourLinkProfileDbContext>(options =>
            options.UseSqlServer(
                ConnectionString,
                 sqlOptions => sqlOptions.EnableRetryOnFailure()

                )
            );

            builder.Services.AddAutoMapper(typeof(MapperProfile));
            builder.Services.AddScoped<ILabourRepository, LabourRepository>();
            builder.Services.AddScoped<ILabourService, LabourService>();
            builder.Services.AddScoped<ICloudinaryHelper, CloudinaryHelper>();
            builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
            builder.Services.AddScoped<IEmployerService, EmployerService>();
              builder.Services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
            builder.Services.AddScoped<IConversationService, ConversationService>();




            builder.Services.AddSingleton<RabbitMQConnection>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connection = new RabbitMQConnection(config);
                connection.DeclareExchange("labourlink.events", ExchangeType.Direct);
                return connection;
            });


            builder.Services.AddScoped<IEventPublisher, EventPublisher>();


            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<IReviewService, ReviewService>();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:5173")
                               .AllowCredentials()
                               .AllowAnyMethod()
                               .AllowAnyHeader();

                    });
            });


            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "ProfileService", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token"
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

            var secret = Encoding.UTF8.GetBytes("Laboulink21345665432@354*(45234567876543fgbfgnh");
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
                    ValidIssuer = "Labourlink-Api",
                    ValidAudience = "Labourlink-Frontend",
                    IssuerSigningKey = new SymmetricSecurityKey(secret),
                    ClockSkew = TimeSpan.Zero // Optional: Removes the default 5-minute clock skew
                };
            });


            var app = builder.Build();


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseCors("AllowSpecificOrigin");
            app.UseMiddleware<TokenAccessingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<UserIdentificationMiddleware>();


            app.MapControllers();

            app.Run();
        }
    }
}