(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;
  const navigation = [
    { path: "/dashboard", label: "Dashboard", icon: "⌂" },
    { path: "/leads", label: "Leads", icon: "◎" },
    { path: "/follow-ups", label: "Follow-ups", icon: "↗" },
    { path: "/courses", label: "Courses", icon: "▤" },
    { path: "/sources", label: "Lead sources", icon: "◇" },
    { path: "/team", label: "Team", icon: "♙" },
    { path: "/settings", label: "Settings", icon: "⚙" }
  ];

  function initials(name) {
    return String(name || "User")
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0))
      .join("")
      .toUpperCase();
  }

  function navMarkup(activePath) {
    return navigation
      .map(
        (item) => `
          <a class="nav-link${activePath === item.path ? " is-active" : ""}" href="#${item.path}">
            <span class="nav-icon" aria-hidden="true">${item.icon}</span>
            <span>${item.label}</span>
          </a>`
      )
      .join("");
  }

  FollowUp.shell = {
    render(options) {
      const user = FollowUp.storage.getUser();
      const activePath = FollowUp.router.currentPath();

      $("#app").html(`
        <div class="app-frame">
          <aside class="sidebar" aria-label="Primary navigation">
            <div class="brand">
              <div class="brand-mark">F</div>
              <div class="brand-copy"><strong>FollowUp</strong><span>Lead workspace</span></div>
            </div>
            <nav class="nav-list">
              <div class="nav-label">Workspace</div>
              ${navMarkup(activePath)}
            </nav>
            <div class="sidebar-footer">
              <div class="workspace-chip">
                <small>Active company</small>
                <strong>${FollowUp.escapeHtml(user.company || "Your company")}</strong>
              </div>
            </div>
          </aside>
          <button class="sidebar-scrim" id="sidebar-scrim" aria-label="Close navigation"></button>
          <main class="main-area">
            <header class="topbar">
              <button class="icon-button mobile-menu-button" id="mobile-menu" aria-label="Open navigation">☰</button>
              <div class="topbar-title">
                <h1>${FollowUp.escapeHtml(options.title)}</h1>
                <p>${FollowUp.escapeHtml(options.subtitle || "Keep every next action visible.")}</p>
              </div>
              <div class="topbar-actions">
                <button class="icon-button" aria-label="Notifications">♢</button>
                <button class="avatar-button" id="profile-button" type="button">
                  <span class="avatar">${initials(user.name)}</span>
                  <span class="avatar-label">${FollowUp.escapeHtml(user.name)}</span>
                </button>
              </div>
            </header>
            <div class="page-content" id="page-content">${options.content}</div>
          </main>
        </div>
      `);

      $("#mobile-menu").on("click", () => $("body").addClass("sidebar-open"));
      $("#sidebar-scrim").on("click", () => $("body").removeClass("sidebar-open"));
      $(".nav-link").on("click", () => $("body").removeClass("sidebar-open"));
      $("#profile-button").on("click", () => {
        if (window.confirm("Log out of FollowUp?")) {
          FollowUp.storage.clearSession();
          FollowUp.router.navigate("/login");
        }
      });
    }
  };
})(window);
