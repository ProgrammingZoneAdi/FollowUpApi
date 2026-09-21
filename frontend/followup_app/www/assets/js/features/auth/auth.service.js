(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  FollowUp.auth = {
    onboard(details) {
      // Onboarding always uses the real API, independently of demo screens.
      return FollowUp.api.post(FollowUp.config.endpoints.onboard, details);
    },

    login(credentials) {
      return FollowUp.api.post(FollowUp.config.endpoints.login, credentials);
    }
  };
})(window);
