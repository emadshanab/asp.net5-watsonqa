using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.StaticFiles;

public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddMvc();
	}

    public void Configure(IApplicationBuilder app)
    {
        app.UseFileServer(new FileServerOptions()
        {
            EnableDirectoryBrowsing = false,
        });

        app.UseMvc(routes =>
        {
            routes.MapRoute(
                name: "Default",
                template: "{controller=Home}/{action=Index}/{id?}");
            routes.MapRoute(
                name: "QAWatsonRedirect",
                template: "{controller=Home}/{action=QAWatsonRedirect}/{id?}");
            routes.MapRoute(
                name: "QAWatsonError",
                template: "{controller=Home}/{action=QAWatsonError}/{id?}");
        });
    }

}
