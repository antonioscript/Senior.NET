using Application.Abstractions.Persistence;
using Domain.Customers; // ICustomerRepository agora vive aqui (Microsoft Repository Pattern)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeniorApi.Controllers;

/// <summary>
/// Ties the Keycloak setup back into the persistence work from before: real repository calls
/// (EF Core or Dapper, whichever AddXPersistence is active in Program.cs - see KeycloakAuthenticationExtensions.cs
/// for the auth half) behind authorization rules. Reading customers only requires being logged in;
/// creating one requires the "admin" realm role, the same way a real admin-only write endpoint would.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(ICustomerRepository customers, IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await customers.GetAllAsync(cancellationToken);
        return Ok(result.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(ToResponse(customer));
    }

    /// <summary>Only "admin" can create customers - everyone else gets 403, including users with a valid token.</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name, request.Email);

        await customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, ToResponse(customer));
    }

    // A small response DTO instead of returning Customer directly: the wire shape (flat Email
    // string, no Rehydrate/AttachExisting* internals) should be controlled independently of how
    // the domain model is structured internally.
    private static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email.Value, customer.CreatedAtUtc);
}

public record CreateCustomerRequest(string Name, string Email);

public record CustomerResponse(Guid Id, string Name, string Email, DateTime CreatedAtUtc);
