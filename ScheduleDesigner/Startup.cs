using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.Interfaces;

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

            services.AddControllers();

            services.AddOData();
            services.AddSignalR();

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
            services.AddScoped<IStaffRepo, SqlStaffRepo>();
            services.AddScoped<IProgrammeRepo, SqlProgrammeRepo>();
            services.AddScoped<IProgrammeStageRepo, SqlProgrammeStageRepo>();
            services.AddScoped<ICourseTypeRepo, SqlCourseTypeRepo>();
            services.AddScoped<ICourseRepo, SqlCourseRepo>();
            services.AddScoped<IProgrammeStageCourseRepo, SqlProgrammeStageCourseRepo>();
            services.AddScoped<IStudentRepo, SqlStudentRepo>();
            services.AddScoped<IClassRepo, SqlClassRepo>();
            services.AddScoped<IGroupRepo, SqlGroupRepo>();
            services.AddScoped<IStudentGroupRepo, SqlStudentGroupRepo>();
            services.AddScoped<ICourseEditionRepo, SqlCourseEditionRepo>();
            services.AddScoped<ICoordinatorRepo, SqlCoordinatorRepo>();
            services.AddScoped<ICoordinatorCourseEditionRepo, SqlCoordinatorCourseEdition>();
            services.AddScoped<IGroupCourseEditionRepo, SqlGroupCourseEditionRepo>();
            services.AddScoped<IRoomRepo, SqlRoomRepo>();
            services.AddScoped<ICourseRoomRepo, SqlCourseRoomRepo>();
            services.AddScoped<ITimestampRepo, SqlTimestampRepo>();
            services.AddScoped<IScheduleSlotRepo, SqlScheduleSlotRepo>();
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

            services.AddSingleton<UsosAuthenticationService>();
            services
                .AddAuthentication("Usos")
                .AddScheme<UsosAuthenticationOptions, UsosAuthenticationHandler>("Usos", null);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.Select().Filter().OrderBy().Count().Expand().MaxTop(100);
                endpoints.MapODataRoute("api", "api", GetEdmModel());
                endpoints.MapHub<ScheduleHub>("/scheduleHub");
            });
        }

        private IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<Settings>("Settings");
            builder.EntitySet<Staff>("Staffs");
            builder.EntitySet<Programme>("Programmes");
            
            builder.EntitySet<ProgrammeStage>("ProgrammeStages")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId });

            builder.EntitySet<CourseType>("CourseTypes");
            
            builder.EntitySet<Course>("Courses")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.CourseId });

            builder.EntitySet<ProgrammeStageCourse>("ProgrammeStageCourses")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.CourseId });

            builder.EntitySet<Class>("Classes")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.ClassId });

            builder.EntitySet<Group>("Groups")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.ClassId, e.GroupId });
            
            builder.EntitySet<Student>("Students")
                .EntityType
                .HasKey(e => e.StudentId);

            builder.EntitySet<StudentGroup>("StudentGroups")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.ClassId, e.GroupId, e.StudentId });

            builder.EntitySet<Coordinator>("Coordinators")
                .EntityType
                .HasKey(e => e.CoordinatorId);

            builder.EntitySet<CourseEdition>("CourseEditions")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.CourseId, e.CourseEditionId });
            
            builder.EntitySet<CoordinatorCourseEdition>("CoordinatorCourseEditions")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.CourseId, e.CourseEditionId, e.CoordinatorId });

            builder.EntitySet<GroupCourseEdition>("GroupCourseEditions")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.CourseId, e.CourseEditionId, e.ClassId, e.GroupId });

            builder.EntitySet<Room>("Rooms");
            
            builder.EntitySet<CourseRoom>("CourseRooms")
                .EntityType
                .HasKey(e => new { e.ProgrammeId, e.CourseId, e.RoomId });

            builder.EntitySet<Timestamp>("Timestamps");
            
            builder.EntitySet<ScheduleSlot>("ScheduleSlots")
                .EntityType
                .HasKey(e => new { e.RoomId, e.TimestampId });

            builder.EntitySet<SchedulePosition>("SchedulePositions")
                .EntityType
                .HasKey(e => new { e.RoomId, e.TimestampId });


            builder.EntitySet<ScheduledMove>("ScheduledMoves")
                .EntityType
                .HasKey(e => new { e.RoomId_1, e.TimestampId_1, e.RoomId_2, e.TimestampId_2 });


            return builder.GetEdmModel();
        }
    }
}
