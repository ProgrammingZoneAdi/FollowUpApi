(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  function statusClass(status) {
    return String(status).toLowerCase().replace(/\s+/g, "-");
  }

  function render() {
    const data = FollowUp.dashboardData;
    const stats = data.stats
      .map(
        (item) => `
          <article class="stat-card">
            <div class="stat-top">
              <span class="stat-label">${item.label}</span>
              <span class="stat-icon" aria-hidden="true">${item.icon}</span>
            </div>
            <div class="stat-value">${item.value}</div>
            <div class="stat-foot"><span class="positive">${item.foot}</span></div>
          </article>`
      )
      .join("");

    const pipeline = data.pipeline
      .map(
        (item) => `
          <div class="pipeline-row">
            <strong>${item.label}</strong>
            <div class="bar-track"><div class="bar-fill w-${item.width}"></div></div>
            <span>${item.count}</span>
          </div>`
      )
      .join("");

    const followUps = data.followUps
      .map(
        (item) => `
          <div class="followup-item">
            <div class="time-badge">${item.time}</div>
            <div>
              <div class="followup-name">${item.name}</div>
              <div class="followup-detail">${item.detail}</div>
            </div>
            <span class="badge badge-${statusClass(item.status)}">${item.status}</span>
          </div>`
      )
      .join("");

    FollowUp.shell.render({
      title: "Dashboard",
      subtitle: "Saturday, 18 July · Here's what needs your attention.",
      content: `
        <div class="page-heading">
          <div>
            <h2>Good evening, Demo Owner</h2>
            <p>Your pipeline is moving well. Six follow-ups are overdue; clearing those first will keep the day on track.</p>
          </div>
          <button class="button button-primary" id="dashboard-add-lead">＋ Add lead</button>
        </div>
        <section class="stats-grid">${stats}</section>
        <section class="dashboard-grid">
          <article class="panel">
            <header class="panel-header">
              <div><h3>Lead pipeline</h3><p>Current distribution across stages</p></div>
              <a class="button button-ghost" href="#/leads">View all</a>
            </header>
            <div class="panel-body pipeline-bars">${pipeline}</div>
          </article>
          <article class="panel">
            <header class="panel-header">
              <div><h3>Next follow-ups</h3><p>Upcoming conversations today</p></div>
            </header>
            <div class="panel-body followup-list">${followUps}</div>
          </article>
        </section>
      `
    });

    $("#dashboard-add-lead").on("click", () => FollowUp.router.navigate("/leads"));
  }

  FollowUp.dashboardView = { render };
})(window);
