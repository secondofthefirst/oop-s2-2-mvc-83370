namespace Library.Domain;

public class FollowUp
{
    public int Id { get; set; }
    public int InspectionId { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Open"; // Open, Closed
    public DateTime? ClosedDate { get; set; }

    // Navigation
    public Inspection? Inspection { get; set; }
}
