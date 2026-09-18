(function (window) {
  "use strict";

  window.FollowUp.dashboardData = {
    stats: [
      { label: "Active leads", value: "148", foot: "+12 this week", icon: "◎" },
      { label: "Due today", value: "18", foot: "6 need attention", icon: "↗" },
      { label: "Converted", value: "32", foot: "+8.4% vs last month", icon: "✓" },
      { label: "Conversion rate", value: "21.6%", foot: "+2.1% improvement", icon: "↟" }
    ],
    pipeline: [
      { label: "New", count: 58, width: 100 },
      { label: "Contacted", count: 42, width: 72 },
      { label: "Qualified", count: 28, width: 48 },
      { label: "Converted", count: 20, width: 34 }
    ],
    followUps: [
      { time: "10:30", name: "Riya Sharma", detail: "BCA admission enquiry", status: "Follow-up" },
      { time: "11:15", name: "Akash Verma", detail: "Requested fee structure", status: "Contacted" },
      { time: "13:00", name: "Neha Joshi", detail: "Campus visit confirmation", status: "Qualified" },
      { time: "15:30", name: "Varun Mehta", detail: "Second counselling call", status: "Follow-up" }
    ]
  };
})(window);
