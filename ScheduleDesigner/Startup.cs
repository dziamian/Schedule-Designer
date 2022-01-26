using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Services;
using ScheduleDesigner.Helpers;

namespace ScheduleDesigner
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ScheduleDesignerDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SchedulingDatabase"));
                //options.ConfigureWarnings(options => options.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddControllers()
                .AddNewtonsoftJson();

            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            services.AddOData();
            services.AddSignalR(options =>
            {
                
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                {
                    Version = "v1",
                    Title = "Schedule Designer API",
                    Description = "ASP.NET Core Web API for designing class schedule by many users simultaneously."
                });
            });

            services.AddScoped<IUnitOfWork, SqlUnitOfWork>();
            
            services.AddHostedService<FullBackupService>();
            services.AddHostedService<DifferentialBackupService>();

            services.Configure<DatabaseConnectionOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<ApplicationOptions>(Configuration.GetSection("ApplicationOptions"));
            services.Configure<Consumer>(Configuration.GetSection("UsosConsumer"));
            services.Configure<FullBackup>(Configuration.GetSection("FullBackup"));
            services.Configure<DifferentialBackup>(Configuration.GetSection("DifferentialBackup"));

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins(Configuration.GetSection("CorsPolicy:AllowedOriginsList").Get<string[]>())
                        .WithExposedHeaders("Content-Disposition");
                });
            });

            services.AddScoped<UsosAuthenticationService>();
            services
                .AddAuthentication("Usos")
                .AddScheme<UsosAuthenticationOptions, UsosAuthenticationHandler>("Usos", null);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdministratorOnly", policy => policy.RequireClaim(claimType: ClaimTypes.Role, "Administrator"));
                
                options.AddPolicy("Proposing", policy => policy.RequireAssertion(context => 
                    context.User.HasClaim(c => c.Type == ClaimTypes.Role && 
                        (c.Value == "Administrator" || c.Value == "Representative"))));
                
                options.AddPolicy("Designer", policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == ClaimTypes.Role && 
                        (c.Value == "Administrator" || c.Value == "Coordinator"))));
                
                options.AddPolicy("Assistant", policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == ClaimTypes.Role && 
                        (c.Value == "Administrator" || c.Value == "Representative" || c.Value == "Coordinator"))));
                
                options.AddPolicy("Recipient", policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == ClaimTypes.Role && 
                        (c.Value == "Student" || c.Value == "Coordinator"))));
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseODataBatching();

            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                var defaultBatchHandler = new DefaultODataBatchHandler
                {
                    MessageQuotas =
                    {
                        MaxNestingDepth = 2,
                        MaxOperationsPerChangeset = 10,
                        MaxReceivedMessageSize = 100
                    }
                };

                endpoints.MapControllers();
                endpoints.Select().Filter().OrderBy().Count().Expand().MaxTop(10000);
                endpoints.MapODataRoute(
                    routeName: "api", 
                    routePrefix: "api", 
                    model: GetEdmModel(),
                    batchHandler: defaultBatchHandler
                );
                endpoints.MapHub<ScheduleHub>("/scheduleHub");
            });
        }

        private IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<User>("Users")
                .EntityType
                .HasKey(e => e.UserId);

            builder.EntitySet<Staff>("Staffs")
                .EntityType
                .HasKey(e => e.UserId);

            builder.EntitySet<Coordinator>("Coordinators")
                .EntityType
                .HasKey(e => e.UserId);

            builder.EntitySet<Student>("Students")
                .EntityType
                .HasKey(e => e.UserId);

            builder.EntitySet<Settings>("Settings");

            builder.EntitySet<CourseType>("CourseTypes");

            builder.EntitySet<Course>("Courses");

            builder.EntitySet<Group>("Groups");

            builder.EntitySet<StudentGroup>("StudentGroups")
                .EntityType
                .HasKey(e => new { e.GroupId, e.StudentId });

            builder.EntitySet<CourseEdition>("CourseEditions")
                .EntityType
                .HasKey(e => new { e.CourseId, e.CourseEditionId });
            
            builder.EntitySet<CoordinatorCourseEdition>("CoordinatorCourseEditions")
                .EntityType
                .HasKey(e => new { e.CourseId, e.CourseEditionId, e.CoordinatorId });

            builder.EntitySet<GroupCourseEdition>("GroupCourseEditions")
                .EntityType
                .HasKey(e => new { e.CourseId, e.CourseEditionId, e.GroupId });

            builder.EntitySet<RoomType>("RoomTypes");
            
            builder.EntitySet<Room>("Rooms");
            
            builder.EntitySet<CourseRoom>("CourseRooms")
                .EntityType
                .HasKey(e => new { e.CourseId, e.RoomId });

            builder.EntitySet<Timestamp>("Timestamps");

            builder.EntitySet<SchedulePosition>("SchedulePositions")
                .EntityType
                .HasKey(e => new { e.RoomId, e.TimestampId, e.CourseId });

            builder.EntitySet<ScheduledMovePosition>("ScheduledMovePositions")
                .EntityType
                .HasKey(e => new { e.MoveId, e.RoomId_1, e.TimestampId_1, e.RoomId_2, e.TimestampId_2, e.CourseId });

            builder.EntitySet<ScheduledMove>("ScheduledMoves");

            builder.EntitySet<Message>("Messages")
                .EntityType
                .HasKey(e => new { e.MoveId });

            builder.EntitySet<Authorization>("Authorizations")
                .EntityType
                .HasKey(e => new { e.UserId });
            
            builder.Namespace = "Service";
            builder.EntityType<Settings>().Collection
                .Function("GetPeriods")
                .Returns<string[]>();

            builder.EntityType<User>().Collection
                .Function("GetMyAccount")
                .ReturnsFromEntitySet<User>("Users");

            builder.EntityType<User>().Collection
                .Action("CreateMyAccount")
                .ReturnsFromEntitySet<User>("Users");

            builder.EntityType<User>().Collection
                .Action("CreateAccountFromUsos")
                .ReturnsFromEntitySet<User>("Users")
                .Parameter<int>("UserId");

            builder.EntityType<Group>()
                .Function("GetGroupFullName")
                .Returns<GroupFullName>();

            builder.EntityType<Group>().Collection
                .Function("GetGroupsFullNames")
                .ReturnsCollection<GroupFullName>()
                .CollectionParameter<int>("GroupsIds");

            builder.EntityType<Room>().Collection
                .Function("GetRoomsNames")
                .ReturnsCollection<RoomName>()
                .CollectionParameter<int>("RoomsIds");

            var getMyCourseEditionsFunction = builder.EntityType<CourseEdition>().Collection
                .Function("GetFilteredCourseEditions")
                .ReturnsCollectionFromEntitySet<CourseEdition>("CourseEditions");
            getMyCourseEditionsFunction
                .CollectionParameter<int>("CoordinatorsIds");
            getMyCourseEditionsFunction
                .CollectionParameter<int>("GroupsIds");
            getMyCourseEditionsFunction
                .CollectionParameter<int>("RoomsIds");
            getMyCourseEditionsFunction
                .Parameter<double>("Frequency");

            var getMyCourseEditionFunction = builder.EntityType<CourseEdition>()
                .Function("GetFilteredCourseEdition")
                .ReturnsFromEntitySet<CourseEdition>("CourseEditions");
            getMyCourseEditionFunction
                .CollectionParameter<int>("CoordinatorsIds");
            getMyCourseEditionFunction
                .CollectionParameter<int>("GroupsIds");
            getMyCourseEditionFunction
                .CollectionParameter<int>("RoomsIds");
            getMyCourseEditionFunction
                .Parameter<int>("Frequency");

            builder.EntityType<CourseEdition>()
                .Function("GetBusyPeriods")
                .ReturnsCollectionFromEntitySet<Timestamp>("Timestamps")
                .CollectionParameter<int>("Weeks");

            var isPeriodBusyFunction = builder.EntityType<CourseEdition>()
                .Function("IsPeriodBusy")
                .Returns<bool>();
            isPeriodBusyFunction
                .Parameter<int>("PeriodIndex");
            isPeriodBusyFunction
                .Parameter<int>("Day");
            isPeriodBusyFunction
                .CollectionParameter<int>("Weeks");

            builder.EntityType<CourseEdition>()
                .Function("GetCourseEditionGroupsSize")
                .Returns<int>();

            var getSchedulePositionFunction = builder.EntityType<SchedulePosition>().Collection
                .Function("GetSchedulePositions")
                .ReturnsCollectionFromEntitySet<SchedulePosition>("SchedulePositions");
            getSchedulePositionFunction
                .Parameter<int>("RoomId");
            getSchedulePositionFunction
                .Parameter<int>("PeriodIndex");
            getSchedulePositionFunction
                .Parameter<int>("Day");
            getSchedulePositionFunction
                .CollectionParameter<int>("Weeks");

            var getScheduleAmountFunction = builder.EntityType<SchedulePosition>().Collection
                .Function("GetScheduleAmount")
                .ReturnsCollection<ScheduleAmount>();
            getScheduleAmountFunction
                .CollectionParameter<int>("CourseEditionIds");

            var getScheduleForModification = builder.EntityType<SchedulePosition>().Collection
                .Function("GetFilteredSchedule")
                .ReturnsFromEntitySet<SchedulePosition>("SchedulePositions");
            getScheduleForModification
                .CollectionParameter<int>("CoordinatorsIds");
            getScheduleForModification
                .CollectionParameter<int>("GroupsIds");
            getScheduleForModification
                .CollectionParameter<int>("RoomsIds");
            getScheduleForModification
                .CollectionParameter<int>("Weeks");

            var getRoomsAvailabilityFunction = builder.EntityType<SchedulePosition>().Collection
                .Function("GetRoomsAvailability")
                .ReturnsCollection<RoomAvailability>();
            getRoomsAvailabilityFunction
                .CollectionParameter<int>("RoomsIds");
            getRoomsAvailabilityFunction
                .Parameter<int>("PeriodIndex");
            getRoomsAvailabilityFunction
                .Parameter<int>("Day");
            getRoomsAvailabilityFunction
                .CollectionParameter<int>("Weeks");

            builder.EntityType<ScheduledMove>().Collection
                .Function("GetConcreteScheduledMoves")
                .ReturnsCollection<ScheduledMoveRead>()
                .CollectionParameter<int>("MovesIds");

            builder.EntityType<SchedulePosition>().Collection
                .Action("ClearSchedule");

            builder.EntityType<CourseEdition>().Collection
                .Action("ClearCourseEditions");

            builder.EntityType<Group>().Collection
                .Action("ClearGroups");

            builder.EntityType<Course>().Collection
                .Action("ClearCourses");

            builder.EntityType<CourseType>().Collection
                .Action("ClearCourseTypes");

            builder.EntityType<Room>().Collection
                .Action("ClearRooms");

            builder.EntityType<RoomType>().Collection
                .Action("ClearRoomTypes");

            builder.EntityType<StudentGroup>().Collection
                .Action("ClearStudentGroups");

            return builder.GetEdmModel();
        }
    }
}
