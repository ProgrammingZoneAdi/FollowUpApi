(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  FollowUp.auth = {
    login(credentials) {
      if (FollowUp.config.useMockData) {
        const deferred = $.Deferred();
        window.setTimeout(
          () =>
            deferred.resolve({
              token: "demo-session-token",
              user: {
                id: "demo-owner",
                name: "Demo Owner",
                company: "Acme Learning"
              }
            }),
          350
        );
        return deferred.promise();
      }

      return FollowUp.api.post(FollowUp.config.endpoints.login, credentials);
    }
  };
})(window);
