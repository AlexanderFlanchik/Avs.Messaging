namespace Contracts;

public class CreateAccount
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid LocationId { get; set; }
}

public class SupervisorRequest
{
    public Guid RequestId { get; set; }
    public Guid LocationId { get; set; }
}

public class SupervisorResponse
{
    public Guid RequestId { get; set; }
    public Guid SupervisorId { get; set; }
}
