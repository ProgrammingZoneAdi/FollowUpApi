(function (window) {
  "use strict";

  window.FollowUp = window.FollowUp || {};
  window.FollowUp.config = {
    appName: "FollowUp",
    apiBaseUrl: "http://localhost:5000",
    useMockData: true,
    requestTimeoutMs: 20000,
    endpoints: {
      login: "/api/auth/login",
      dashboard: "/api/dashboard",
      leads: "/api/leads",
      createLead: "/api/leads"
    }
  };
})(window);
