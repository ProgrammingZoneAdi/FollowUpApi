(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  FollowUp.router.register("/login", {
    requiresAuth: false,
    render: FollowUp.authView.render
  });
  FollowUp.router.register("/dashboard", { render: FollowUp.dashboardView.render });
  FollowUp.router.register("/leads", { render: FollowUp.leadsView.render });

  Object.keys(FollowUp.featurePages.definitions).forEach((path) => {
    FollowUp.router.register(path, {
      render: () => FollowUp.featurePages.render(path)
    });
  });

  FollowUp.router.register("/not-found", {
    requiresAuth: false,
    render: () => FollowUp.router.navigate(FollowUp.storage.getToken() ? "/dashboard" : "/login")
  });

  $(function () {
    FollowUp.router.start();

    if ("serviceWorker" in navigator && window.location.protocol !== "file:") {
      navigator.serviceWorker.register("./service-worker.js").catch(() => {
        FollowUp.toast("Offline support could not be enabled.", "error");
      });
    }
  });
})(window);
