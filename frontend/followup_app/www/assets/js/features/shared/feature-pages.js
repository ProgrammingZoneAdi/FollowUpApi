(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;
  const pages = {
    "/follow-ups": {
      title: "Follow-ups",
      icon: "↗",
      text: "The next endpoint will power an agenda of overdue, today and upcoming follow-ups."
    },
    "/courses": {
      title: "Courses",
      icon: "▤",
      text: "Manage the courses or services that leads are interested in, including fee and duration."
    },
    "/sources": {
      title: "Lead sources",
      icon: "◇",
      text: "Configure channels such as website, referral, walk-in and campaigns for conversion reporting."
    },
    "/team": {
      title: "Team",
      icon: "♙",
      text: "Company members, roles and lead assignment will live here after authentication is implemented."
    },
    "/settings": {
      title: "Settings",
      icon: "⚙",
      text: "Company branding, feature flags, statuses and other client-specific configuration belong here."
    }
  };

  FollowUp.featurePages = {
    definitions: pages,
    render(path) {
      const page = pages[path];
      FollowUp.shell.render({
        title: page.title,
        content: `
          <section class="panel feature-placeholder">
            <div>
              <div class="feature-placeholder-mark">${page.icon}</div>
              <h2>${page.title} foundation is ready</h2>
              <p>${page.text} The navigation and responsive shell already support this feature; API work can be added without restructuring the app.</p>
            </div>
          </section>`
      });
    }
  };
})(window);
