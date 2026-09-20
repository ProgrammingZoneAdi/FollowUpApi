using System.Text.Json.Serialization;
using FollowUpApi.Features.Leads;

namespace FollowUpApi.Features.Dashboard;

public sealed class DashboardSummaryResponse
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("summary_date")]
    public DateOnly SummaryDate { get; set; }

    [JsonPropertyName("time_zone")]
    public string TimeZone { get; set; } = "Asia/Kolkata";

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("total_leads")]
    public int TotalLeads { get; set; }

    [JsonPropertyName("active_leads")]
    public int ActiveLeads { get; set; }

    [JsonPropertyName("converted_leads")]
    public int ConvertedLeads { get; set; }

    [JsonPropertyName("today_follow_ups")]
    public int TodayFollowUps { get; set; }

    [JsonPropertyName("overdue_follow_ups")]
    public int OverdueFollowUps { get; set; }

    [JsonPropertyName("upcoming_follow_ups")]
    public int UpcomingFollowUps { get; set; }

    [JsonPropertyName("unscheduled_leads")]
    public int UnscheduledLeads { get; set; }

    [JsonPropertyName("conversion_rate")]
    public decimal ConversionRate { get; set; }

    [JsonPropertyName("pipeline")]
    public List<DashboardStatusResponse> Pipeline { get; set; } = new();

    [JsonPropertyName("next_follow_ups_today")]
    public List<LeadResponse> NextFollowUpsToday { get; set; } = new();
}
