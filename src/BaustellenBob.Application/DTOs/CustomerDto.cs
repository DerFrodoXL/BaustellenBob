namespace BaustellenBob.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Street { get; set; }
    public string? Zip { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }

    public string DisplayName => string.IsNullOrEmpty(Company) ? Name : $"{Name} ({Company})";
    public string AddressLine => string.Join(", ", new[] { Street, $"{Zip} {City}".Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
