namespace Library.Domain;

public class Inspection
{
    public int Id { get; set; }
    public int PremisesId { get; set; }
    public DateTime InspectionDate { get; set; }
    public int Score { get; set; } // 0-100
    public string Outcome { get; set; } = "Pass"; // Pass, Fail
    public string Notes { get; set; } = string.Empty;

    // Navigation
    public Premises? Premises { get; set; }
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
