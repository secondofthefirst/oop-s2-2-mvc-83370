namespace Library.Domain;

public class Premises
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Town { get; set; } = string.Empty;
    public string RiskRating { get; set; } = "Medium"; // Low, Medium, High

    // Navigation
    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
}
