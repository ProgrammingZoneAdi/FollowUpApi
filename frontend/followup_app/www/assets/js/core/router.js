(function (window) {
  "use strict";

  const routes = {};

  function normalize(hash) {
    const path = (hash || "#/dashboard").replace(/^#/, "");
    return path.startsWith("/") ? path : `/${path}`;
  }

  function currentPath() {
    return normalize(window.location.hash);
  }

  function render() {
    const path = currentPath();
    const route = routes[path] || routes["/not-found"];

    if (!route) return;

    if (route.requiresAuth !== false && !window.FollowUp.storage.getToken()) {
      navigate("/login");
      return;
    }

    route.render(path);
  }

  function navigate(path) {
    const target = path.startsWith("/") ? path : `/${path}`;
    if (currentPath() === target) {
      render();
      return;
    }
    window.location.hash = target;
  }

  window.FollowUp = window.FollowUp || {};
  window.FollowUp.router = {
    register(path, definition) {
      routes[path] = definition;
    },
    navigate,
    currentPath,
    start() {
      $(window).on("hashchange", render);
      if (!window.location.hash) {
        navigate(window.FollowUp.storage.getToken() ? "/dashboard" : "/login");
      } else {
        render();
      }
    }
  };
})(window);
