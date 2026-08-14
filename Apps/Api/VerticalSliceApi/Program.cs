using System.Reflection;
using Database.EntityFramework;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VerticalSliceApi.Features;

// ------------------------------------------------------------------------------------------------
// VERTICAL SLICE API — Program.cs
// ------------------------------------------------------------------------------------------------
// Compare this with SeniorApi/Program.cs:
//   SeniorApi: registers repositories, UoW, authentication, many layers of abstraction.
//   VerticalSliceApi: registers DbContext, MediatR, and the endpoints. That's it.
//
// There are no repository interfaces here. Each Handler in Features/ accesses AppDbContext
// directly. If a feature needs something unusual, it does it in its own handler — it doesn't
// need to add a method to a shared interface.
// ------------------------------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// NuGet packages (VerticalSliceApi.csproj):
//   MediatR — dispatcher + handler pipeline. AddMediatR scans the assembly, finds every
//             IRequestHandler<TRequest, TResponse>, and registers it in DI. ISender (injected
//             into endpoints) is what dispatches a request to its handler at runtime.
//
//   Microsoft.EntityFrameworkCore.SqlServer (via Database project reference) — same AppDbContext
//   used by SeniorApi, pointing at the same SQL Server container. In a truly standalone VS project
//   you would have your own DbContext; here we share to avoid duplicating migrations.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:SqlServer")));

// Scans this assembly for classes implementing IEndpoint and registers them as transient services.
// Each feature's nested Endpoint class gets picked up automatically — no manual wiring needed.
// See Features/EndpointExtensions.cs for the implementation.
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Resolves every registered IEndpoint from DI and calls MapEndpoint on each.
// Adding a new feature = adding a new file in Features/. Program.cs stays untouched.
app.MapEndpoints();

app.Run();
