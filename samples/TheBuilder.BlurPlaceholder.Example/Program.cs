WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (string.Equals(
        builder.Configuration.GetConnectionString("umbracoDbDSN_ProviderName"),
        "Microsoft.Data.Sqlite",
        StringComparison.OrdinalIgnoreCase))
{
    string dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data");
    Directory.CreateDirectory(dataDirectory);
    File.Open(Path.Combine(dataDirectory, "Umbraco.sqlite.db"), FileMode.OpenOrCreate).Dispose();
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
