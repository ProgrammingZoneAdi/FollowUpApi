(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  function statusClass(status) {
    return String(status).toLowerCase().replace(/\s+/g, "-");
  }

  function rowMarkup(lead) {
    return `
      <tr>
        <td><span class="lead-name">${FollowUp.escapeHtml(lead.name)}</span>
          <span class="lead-contact">${FollowUp.escapeHtml(lead.id)} · ${FollowUp.escapeHtml(lead.mobile)}</span></td>
        <td>${FollowUp.escapeHtml(lead.course)}</td>
        <td>${FollowUp.escapeHtml(lead.source)}</td>
        <td>${FollowUp.escapeHtml(lead.assignedTo)}</td>
        <td><span class="badge badge-${statusClass(lead.status)}">${FollowUp.escapeHtml(lead.status)}</span></td>
        <td>${FollowUp.escapeHtml(lead.nextAction)}</td>
        <td><button class="button button-ghost" type="button" aria-label="Open ${FollowUp.escapeHtml(lead.name)}">•••</button></td>
      </tr>`;
  }

  function renderRows() {
    const query = String($("#lead-search").val() || "").toLowerCase();
    const status = $("#lead-status").val();
    const rows = FollowUp.leadsData
      .all()
      .filter((lead) => {
        const matchesText = [lead.name, lead.mobile, lead.course, lead.source]
          .join(" ")
          .toLowerCase()
          .includes(query);
        return matchesText && (!status || lead.status === status);
      });

    $("#lead-table-body").html(
      rows.length
        ? rows.map(rowMarkup).join("")
        : `<tr><td colspan="7"><div class="empty-state"><strong>No leads found</strong>Try another search or status.</div></td></tr>`
    );
  }

  function render() {
    FollowUp.shell.render({
      title: "Leads",
      subtitle: "Track ownership, status and the next promised action.",
      content: `
        <div class="page-heading">
          <div><h2>Lead workspace</h2><p>Every lead should have a clear owner, current status and next follow-up date.</p></div>
          <button class="button button-primary" id="open-lead-dialog">＋ Add lead</button>
        </div>
        <section class="panel">
          <div class="toolbar">
            <div class="toolbar-group">
              <label><span class="sr-only">Search leads</span>
                <input class="field search-field" id="lead-search" placeholder="Search name, mobile or course" />
              </label>
              <label><span class="sr-only">Filter by status</span>
                <select class="select" id="lead-status">
                  <option value="">All statuses</option>
                  <option>New</option><option>Contacted</option><option>Follow-up</option>
                  <option>Qualified</option><option>Won</option><option>Lost</option>
                </select>
              </label>
            </div>
            <span class="stat-label" id="lead-count"></span>
          </div>
          <div class="table-wrap">
            <table class="data-table">
              <thead><tr><th>Lead</th><th>Course</th><th>Source</th><th>Owner</th>
                <th>Status</th><th>Next action</th><th></th></tr></thead>
              <tbody id="lead-table-body"></tbody>
            </table>
          </div>
        </section>
        <dialog class="dialog" id="lead-dialog">
          <form id="lead-form">
            <header class="dialog-header"><h3>Add a new lead</h3>
              <button class="icon-button" id="close-lead-dialog" type="button" aria-label="Close">×</button>
            </header>
            <div class="dialog-body form-grid">
              <label class="form-group"><span class="form-label">Lead name</span>
                <input class="field" name="name" required /></label>
              <label class="form-group"><span class="form-label">Mobile</span>
                <input class="field" name="mobile" inputmode="tel" required /></label>
              <label class="form-group"><span class="form-label">Course</span>
                <select class="select" name="course" required>
                  <option value="">Choose course</option><option>BCA</option><option>BBA</option>
                  <option>B.Com</option><option>MBA</option><option>MCA</option>
                </select></label>
              <label class="form-group"><span class="form-label">Lead source</span>
                <select class="select" name="source" required>
                  <option value="">Choose source</option><option>Website</option><option>Referral</option>
                  <option>Walk-in</option><option>Instagram</option>
                </select></label>
              <label class="form-group is-wide"><span class="form-label">Next follow-up</span>
                <input class="field" name="nextAction" type="datetime-local" /></label>
            </div>
            <footer class="dialog-footer">
              <button class="button button-secondary" id="cancel-lead-dialog" type="button">Cancel</button>
              <button class="button button-primary" type="submit">Save lead</button>
            </footer>
          </form>
        </dialog>
      `
    });

    function refresh() {
      renderRows();
      $("#lead-count").text(`${FollowUp.leadsData.all().length} total leads`);
    }

    $("#lead-search").on("input", refresh);
    $("#lead-status").on("change", refresh);
    $("#open-lead-dialog").on("click", () => $("#lead-dialog").get(0).showModal());
    $("#close-lead-dialog, #cancel-lead-dialog").on("click", () => $("#lead-dialog").get(0).close());
    $("#lead-form").on("submit", function (event) {
      event.preventDefault();
      const values = Object.fromEntries(new window.FormData(event.currentTarget).entries());
      FollowUp.leadsData.add(values);
      event.currentTarget.reset();
      $("#lead-dialog").get(0).close();
      refresh();
      FollowUp.toast("Lead added to the local demo workspace.");
    });

    refresh();
  }

  FollowUp.leadsView = { render };
})(window);
