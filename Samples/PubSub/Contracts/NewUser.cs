namespace Contracts;

public class NewUser
{
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = default!;
    public string Login { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}