using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Services;

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
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
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

            services.AddScoped<ISettingsRepo, SqlSettingsRepo>();
            services.AddScoped<IAuthorizationRepo, SqlAuthorizationRepo>();
            services.AddScoped<IUserRepo, SqlUserRepo>();
            services.AddScoped<IStudentRepo, SqlStudentRepo>();
            services.AddScoped<ICoordinatorRepo, SqlCoordinatorRepo>();
            services.AddScoped<IStaffRepo, SqlStaffRepo>();
            services.AddScoped<ICourseTypeRepo, SqlCourseTypeRepo>();
            services.AddScoped<ICourseRepo, SqlCourseRepo>();
            services.AddScoped<IGroupRepo, SqlGroupRepo>();
            services.AddScoped<IStudentGroupRepo, SqlStudentGroupRepo>();
            services.AddScoped<ICourseEditionRepo, SqlCourseEditionRepo>();
            services.AddScoped<ICoordinatorCourseEditionRepo, SqlCoordinatorCourseEdition>();
            services.AddScoped<IGroupCourseEditionRepo, SqlGroupCourseEditionRepo>();
            services.AddScoped<IRoomTypeRepo, SqlRoomTypeRepo>();
            services.AddScoped<IRoomRepo, SqlRoomRepo>();
            services.AddScoped<ICourseRoomRepo, SqlCourseRoomRepo>();
            services.AddScoped<ITimestampRepo, SqlTimestampRepo>();
            services.AddScoped<ICourseRoomTimestampRepo, SqlCourseRoomTimestampRepo>();
            services.AddScoped<ISchedulePositionRepo, SqlSchedulePositionRepo>();
            services.AddScoped<IScheduledMoveRepo, SqlScheduledMoveRepo>();

            services.Configure<ApplicationInfo>(Configuration.GetSection("ApplicationInfo"));
            services.Configure<Consumer>(Configuration.GetSection("UsosConsumer"));

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins(Configuration.GetSection("CorsPolicy:AllowedOriginsList").Get<string[]>());
                });
            });

            services.AddScoped<UsosAuthenticationService>();
            services
                .AddAuthentication("Usos")
                .AddScheme<UsosAuthenticationOptions, UsosAuthenticationHandler>("Usos", null);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireClaim("admin"));
                options.AddPolicy("Coordinator", policy => policy.RequireClaim("coordinator"));
                options.AddPolicy("Representative", policy => policy.RequireClaim("representative"));
                options.AddPolicy("Staff", policy => policy.RequireClaim("staff"));
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
                endpoints.Select().Filter().OrderBy().Count().Expand().MaxTop(100);
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
            
            builder.EntitySet<CourseRoomTimestamp>("CourseRoomTimestamps")
                .EntityType
                .HasKey(e => new { e.RoomId, e.TimestampId, e.CourseId });

            builder.EntitySet<SchedulePosition>("SchedulePositions")
                .EntityType
                .HasKey(e => new { e.RoomId, e.TimestampId, e.CourseId });

            builder.EntitySet<ScheduledMove>("ScheduledMoves")
                .EntityType
                .HasKey(e => new { e.MoveId, e.RoomId_1, e.TimestampId_1, e.RoomId_2, e.TimestampId_2, e.CourseId });

            
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

            builder.EntityType<CourseEdition>().Collection
                .Function("GetMyCourseEditions")
                .ReturnsFromEntitySet<CourseEdition>("CourseEditions")
                .Parameter<double>("Frequency");

            builder.EntityType<CourseEdition>()
                .Function("GetBusyPeriods")
                .ReturnsCollectionFromEntitySet<Timestamp>("Timestamps")
                .CollectionParameter<int>("Weeks");

            var getSchedulePositionFunction = builder.EntityType<SchedulePosition>().Collection
                .Function("GetSchedulePosition")
                .ReturnsFromEntitySet<SchedulePosition>("SchedulePositions");
            getSchedulePositionFunction
                .Parameter<int>("RoomId");
            getSchedulePositionFunction
                .Parameter<int>("PeriodIndex");
            getSchedulePositionFunction
                .Parameter<int>("Day");
            getSchedulePositionFunction
                .Parameter<int>("Week");

            builder.EntityType<SchedulePosition>().Collection
                .Function("GetScheduleAsCoordinator")
                .ReturnsFromEntitySet<SchedulePosition>("SchedulePositions")
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

            /*var addSchedulePositionsAction = builder.EntityType<SchedulePosition>().Collection
                .Action("AddSchedulePositions");

            addSchedulePositionsAction
                .Parameter<int>("CourseId");
            addSchedulePositionsAction
                .Parameter<int>("CourseEditionId");
            addSchedulePositionsAction
                .Parameter<int>("RoomId");
            addSchedulePositionsAction
                .Parameter<int>("PeriodIndex");
            addSchedulePositionsAction
                .Parameter<int>("Day");
            addSchedulePositionsAction
                .CollectionParameter<int>("Weeks");*/

            return builder.GetEdmModel();
        }
    }
}
