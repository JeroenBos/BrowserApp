using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JBSnorro.Logging;
using JBSnorro.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (Environment.IsDevelopment())
            {
                services.AddMvc(config => config.Filters.Add(typeof(CustomExceptionFilter)));
            }
            else
            {
                services.AddMvc();
            }

            var logger = new Logger();
            var userSessionManager = UserSessionManager.Create(new TempUserSessionsStorage(), stream => new TempViewModel(), logger);
            services.AddTransient(serviceProvider => userSessionManager);
        }
        class TempUserSessionsStorage : IUserSessionsStorage<Stream>
        {
            public void CreateOrUpdate(string user, Stream data)
            {
            }
            public Task<Stream> TryOpen(string user)
            {
                return Task.FromResult<Stream>(new MemoryStream());
            }

            void IUserSessionsStorage.CreateOrUpdate(string user, object data) => CreateOrUpdate(user, (Stream)data);
            Task<object> IUserSessionsStorage.TryOpen(string user) => TryOpen(user).Cast<object, Stream>();
        }
        class TempViewModel : INotifyPropertyChanged
        {
            private int count;
            public event PropertyChangedEventHandler PropertyChanged;
            public int Count
            {
                get => count;
                set
                {
                    if (count != value)
                    {
                        this.count = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                    }
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
