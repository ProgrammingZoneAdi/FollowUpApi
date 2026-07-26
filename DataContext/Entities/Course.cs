namespace FollowUpApi.DataContext.Entities;

public class Course : CompanyBaseEntity
{
    public string CourseName { get; set; } = string.Empty;

    public decimal? Fees { get; set; }

    public string Duration { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";
}
