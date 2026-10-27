using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartHaat.Application.Catalog;
using SmartHaat.Application.Inventory;
using SmartHaat.Application.Commerce;
using SmartHaat.Application.Pricing;
using SmartHaat.Application.Reporting;
using SmartHaat.Infrastructure.Catalog;
using SmartHaat.Infrastructure.Commerce;
using SmartHaat.Infrastructure.Pricing;
using SmartHaat.Infrastructure.Reporting;
using SmartHaat.Infrastructure.Identity;
using SmartHaat.Infrastructure.Inventory;
using SmartHaat.Infrastructure.Persistence;

namespace SmartHaat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("SmartHaat")
            ?? throw new InvalidOperationException("ConnectionStrings:SmartHaat is required.");
        services.AddDbContext<SmartHaatDbContext>(o => o.UseSqlServer(connection, sql => sql.EnableRetryOnFailure()));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.SignIn.RequireConfirmedEmail = false;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<SmartHaatDbContext>();
        services.AddScoped<ICatalogService, EfCatalogService>();
        services.AddSingleton<IProductImageStorage, LocalProductImageStorage>();
        services.AddScoped<IInventoryService, EfInventoryService>();
        services.AddScoped<ICommerceService, EfCommerceService>();
        services.AddScoped<ICouponService, EfCouponService>();
        services.AddScoped<IReportingService, EfReportingService>();
        return services;
    }
}
