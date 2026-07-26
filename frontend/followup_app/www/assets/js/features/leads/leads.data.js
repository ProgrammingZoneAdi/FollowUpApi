(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;
  const seed = [
    {
      id: "L-1048",
      name: "Riya Sharma",
      mobile: "98765 43210",
      course: "BCA",
      source: "Website",
      assignedTo: "Priya",
      status: "Follow-up",
      nextAction: "18 Jul, 10:30"
    },
    {
      id: "L-1047",
      name: "Akash Verma",
      mobile: "98111 22003",
      course: "MBA",
      source: "Referral",
      assignedTo: "Amit",
      status: "Contacted",
      nextAction: "18 Jul, 11:15"
    },
    {
      id: "L-1046",
      name: "Neha Joshi",
      mobile: "99102 76540",
      course: "B.Com",
      source: "Walk-in",
      assignedTo: "Priya",
      status: "Qualified",
      nextAction: "18 Jul, 13:00"
    },
    {
      id: "L-1045",
      name: "Varun Mehta",
      mobile: "98900 11452",
      course: "BBA",
      source: "Instagram",
      assignedTo: "Amit",
      status: "New",
      nextAction: "19 Jul, 09:30"
    },
    {
      id: "L-1044",
      name: "Sara Khan",
      mobile: "98204 11862",
      course: "MCA",
      source: "Website",
      assignedTo: "Priya",
      status: "Won",
      nextAction: "Completed"
    }
  ];

  FollowUp.leadsData = {
    all() {
      return FollowUp.storage.get(FollowUp.storage.keys.leads, seed);
    },
    add(lead) {
      const leads = this.all();
      const nextNumber = 1049 + leads.length;
      leads.unshift({
        id: `L-${nextNumber}`,
        name: lead.name,
        mobile: lead.mobile,
        course: lead.course,
        source: lead.source,
        assignedTo: "Unassigned",
        status: "New",
        nextAction: lead.nextAction || "Not scheduled"
      });
      FollowUp.storage.set(FollowUp.storage.keys.leads, leads);
      return leads;
    }
  };
})(window);
