(function (window) {
  "use strict";
  const FollowUp = window.FollowUp;
  const escape = FollowUp.escapeHtml;

  function rowsMarkup(rows) {
    return rows.map((user) => {
      const joined = new Date(user.joined_on);
      return `<tr><td>${escape(user.name)}${user.is_primary_owner ? " <small>(Primary owner)</small>" : ""}</td>
        <td>${escape(user.email)}</td><td>${escape(user.mobile)}</td><td>${escape(user.role)}</td>
        <td>${escape(user.status)}</td><td>${Number.isNaN(joined.getTime()) ? "—" : escape(joined.toLocaleDateString("en-IN"))}</td></tr>`;
    }).join("");
  }

  function mount(root, company) {
    root.html(`<section class="panel">
      <div class="panel-header"><h3>Company members</h3></div>
      <div class="panel-body">
        <form class="toolbar" id="team-filters">
          <label class="form-group"><span class="form-label">Search name, email or mobile</span>
            <input class="field" name="search" maxlength="100" type="search" /></label>
          <label class="form-group"><span class="form-label">Role</span>
            <select class="field" name="role"><option>All</option><option>Owner</option><option>Admin</option><option>Counsellor</option><option>Staff</option></select></label>
          <label class="form-group"><span class="form-label">Status</span>
            <select class="field" name="status"><option>Active</option><option>Inactive</option><option>All</option></select></label>
          <label class="form-group"><span class="form-label">Per page</span>
            <select class="field" name="page_size"><option>10</option><option selected>20</option><option>50</option><option>100</option></select></label>
          <button class="button button-primary" type="submit">Apply filters</button>
        </form>
        <p class="team-list-message" role="status" aria-live="polite"></p>
        <p class="team-list-error" role="alert" hidden></p>
        <button class="button button-secondary team-retry" type="button" hidden>Retry</button>
        <div class="table-wrap" hidden><table class="data-table"><caption>Company members</caption>
          <thead><tr><th scope="col">Name</th><th scope="col">Email</th><th scope="col">Mobile</th><th scope="col">Role</th><th scope="col">Status</th><th scope="col">Joined</th></tr></thead><tbody></tbody></table></div>
        <div class="toolbar"><button class="button button-secondary team-prev" type="button" disabled>Previous</button>
          <span class="team-page" aria-live="polite"></span>
          <button class="button button-secondary team-next" type="button" disabled>Next</button></div>
      </div></section>`);

    const filters = root.find("#team-filters");
    const message = root.find(".team-list-message");
    const error = root.find(".team-list-error");
    const retry = root.find(".team-retry");
    const previous = root.find(".team-prev");
    const next = root.find(".team-next");
    const table = root.find(".table-wrap");
    let state = { page: 1, page_size: 20, search: "", role: "All", status: "Active" };
    let sequence = 0;
    let totalPages = 0;
    let loading = false;

    function load() {
      if (!root[0].isConnected) return;
      const requestId = ++sequence;
      if (!FollowUp.storage.getToken()) {
        FollowUp.router.navigate("/login");
        return;
      }
      if (FollowUp.storage.getCompany()?.company_id !== company.company_id) {
        FollowUp.router.navigate("/team");
        return;
      }
      loading = true;
      root.attr("aria-busy", "true");
      message.text("Loading members…");
      error.prop("hidden", true).text("");
      retry.prop("hidden", true);
      table.prop("hidden", true);
      root.find("tbody").empty();
      root.find(".team-page").text("");
      previous.add(next).prop("disabled", true);
      const current = () => requestId === sequence && root[0].isConnected;
      FollowUp.team.list(company.company_id, { ...state })
        .done((data) => {
          if (!current()) return;
          if (!data || !Array.isArray(data.rows) || !Number.isInteger(data.total_count) || data.total_count < 0) {
            message.text("");
            error.text("Unexpected member-list response. Please retry.").prop("hidden", false);
            retry.prop("hidden", false);
            return;
          }
          totalPages = Math.ceil(data.total_count / state.page_size);
          if (state.page > Math.max(1, totalPages)) {
            state.page = Math.max(1, totalPages);
            load();
            return;
          }
          root.find("tbody").html(rowsMarkup(data.rows));
          table.prop("hidden", data.rows.length === 0);
          message.text(data.rows.length ? `${data.total_count} member(s) found.` : "No members match these filters.");
          root.find(".team-page").text(totalPages ? `Page ${state.page} of ${totalPages}` : "0 results");
          previous.prop("disabled", state.page <= 1);
          next.prop("disabled", state.page >= totalPages);
        })
        .fail((failure) => {
          if (!current()) return;
          message.text("");
          error.text(failure.message || "Unable to load members.").prop("hidden", false);
          retry.prop("hidden", false);
        })
        .always(() => {
          if (!current()) return;
          loading = false;
          root.removeAttr("aria-busy");
        });
    }

    filters.on("submit", function (event) {
      event.preventDefault();
      if (!this.reportValidity()) return;
      state = {
        page: 1,
        page_size: Number(filters.find('[name="page_size"]').val()),
        search: filters.find('[name="search"]').val().trim(),
        role: filters.find('[name="role"]').val(),
        status: filters.find('[name="status"]').val()
      };
      load();
    });
    previous.on("click", () => { if (!loading && state.page > 1) { state.page--; load(); } });
    next.on("click", () => { if (!loading && state.page < totalPages) { state.page++; load(); } });
    retry.on("click", load);
    load();
    return () => { state.page = 1; load(); };
  }

  FollowUp.teamList = { mount, rowsMarkup };
})(window);
