using Ekiphan.Domain.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ekiphan.Infrastructure.Identity;

public static class AdminBootstrapper
{
    public static async Task BootstrapAdminAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var email = configuration["Authentication:Bootstrap:Email"];
        var password = configuration["Authentication:Bootstrap:Password"];
        var displayName =
            configuration["Authentication:Bootstrap:DisplayName"] ??
            "Ekiphan Administrator";
        if (string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await using var seedScope = services.CreateAsyncScope();
        var seedDb = seedScope.ServiceProvider.GetRequiredService<EkiphanDbContext>();
        await SeedAuthorizationAsync(seedDb, cancellationToken);

        if (string.IsNullOrWhiteSpace(email) ||
            !AdminPasswordPolicy.IsStrong(password))
        {
            throw new InvalidOperationException(
                "Bootstrap admin requires an email and a password of at least " +
                "14 characters containing upper, lower, digit and symbol.");
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<EkiphanDbContext>();
        if (await dbContext.AdminUsers.AnyAsync(cancellationToken))
        {
            return;
        }

        var user = new AdminUser(Guid.NewGuid(), email, displayName);
        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<AdminUser>>();
        user.SetPasswordHash(hasher.HashPassword(user, password!));
        user.SetPermissions(AdminPermissionCode.All.ToArray());
        dbContext.AdminUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var superAdmin = await dbContext.AdminRoles.SingleAsync(x => x.NormalizedName == "SUPERADMIN", cancellationToken);
        dbContext.AdminUserRoles.Add(new AdminUserRole(user.Id, superAdmin.Id, user.Id, DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAuthorizationAsync(EkiphanDbContext db,CancellationToken ct)
    {
        var existing=await db.AdminPermissions.ToDictionaryAsync(x=>x.Code,StringComparer.Ordinal,ct);
        foreach(var code in AdminPermissionCode.Granular)
        {
            var group=code.Split('.')[0];var name=string.Join(' ',code.Split('.').Select(x=>char.ToUpperInvariant(x[0])+x[1..]));
            if(existing.TryGetValue(code,out var permission))permission.Refresh(name,$"Allows {code} operations.",group);
            else db.AdminPermissions.Add(new PermissionDefinition(StableId("permission:"+code),code,name,$"Allows {code} operations.",group));
        }
        await db.SaveChangesAsync(ct);
        await UpsertRole("SuperAdmin","System administrator with all permissions.",AdminPermissionCode.Granular,true);
        await UpsertRole("ProductManager","Product and media operations.",AdminPermissionCode.ProductsSet.Concat(AdminPermissionCode.CategoriesSet).Concat(AdminPermissionCode.BrandsSet).Concat(AdminPermissionCode.MediaSet).Except([AdminPermissionCode.Products.Delete,AdminPermissionCode.Categories.Delete,AdminPermissionCode.Brands.Delete,AdminPermissionCode.Media.Delete]),true);
        await UpsertRole("ContentEditor","Content, showroom and SEO operations.",AdminPermissionCode.ContentSet.Concat(AdminPermissionCode.ReferencesSet).Concat(AdminPermissionCode.ShowroomSet).Concat(AdminPermissionCode.BannersSet).Concat([AdminPermissionCode.Media.Read,AdminPermissionCode.Media.Upload,AdminPermissionCode.Media.Update,AdminPermissionCode.Seo.Read,AdminPermissionCode.Seo.Manage]),true);
        await UpsertRole("QuoteViewer","Read-only quote workflow.",[AdminPermissionCode.Quotes.Read,AdminPermissionCode.Quotes.NotesCreate,AdminPermissionCode.Quotes.HistoryRead,AdminPermissionCode.Quotes.Export,AdminPermissionCode.Quotes.PdfGenerate],true);
        await UpsertRole("QuoteManager","Quote operations without deletion.",AdminPermissionCode.QuotesSet.Except([AdminPermissionCode.Quotes.Delete]),true);
        async Task UpsertRole(string name,string description,IEnumerable<string> codes,bool system)
        {
            var normalizedName=name.ToUpperInvariant();
            var role=await db.AdminRoles.Include(x=>x.Permissions).SingleOrDefaultAsync(x=>x.NormalizedName==normalizedName,ct);
            role??=new AdminRole(StableId("role:"+name),name,description,system);
            if(db.Entry(role).State==EntityState.Detached)db.AdminRoles.Add(role);
            var ids=await db.AdminPermissions.Where(x=>codes.Contains(x.Code)).Select(x=>x.Id).ToArrayAsync(ct);
            var current=role.Permissions.Select(x=>x.PermissionId).Order().ToArray();
            if(!current.SequenceEqual(ids.Order()))role.SetPermissions(ids,Guid.Empty);
            await db.SaveChangesAsync(ct);
        }
    }

    private static Guid StableId(string value)
    {
        var bytes=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}
