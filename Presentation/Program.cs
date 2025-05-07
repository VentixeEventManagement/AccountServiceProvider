using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Presentation.Data.Contexts;
using Presentation.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

builder.Services.AddDbContext<DataContext>(x => x.UseSqlServer(builder.Configuration.GetConnectionString("AuthenticationServer")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(option =>
{
    option.SignIn.RequireConfirmedEmail = true;
    option.User.RequireUniqueEmail = true;
    option.Password.RequiredLength = 8;
}).AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();


var app = builder.Build();
app.MapGrpcService<AccountService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
