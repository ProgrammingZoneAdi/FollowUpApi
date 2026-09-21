(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;
  function render() {
    const company = FollowUp.storage.getCompany();
    if (!company) {
      FollowUp.shell.render({
        title: "Team",
        content: `<section class="panel panel-body"><h2>Add company user</h2><p>${company
          ? "Only Owner or Admin can add users to this company."
          : "No active company is available. Contact your company administrator."}</p></section>`
      });
      return;
    }

    FollowUp.shell.render({
      title: "Team",
      subtitle: `Members of ${company.company_name}`,
      content: `
        <div id="team-list"></div>
        <section class="panel" ${FollowUp.team.canManage(company) ? "" : "hidden"}>
          <div class="panel-header"><div><h3>Add company user</h3>
            <p>This form saves real company membership.</p></div></div>
          <div class="panel-body">
            <form id="add-user-form" class="login-form">
              <div class="form-grid">
                <label class="form-group"><span class="form-label">Name</span>
                  <input class="field" name="name" maxlength="150" autocomplete="off" required /></label>
                <label class="form-group"><span class="form-label">Mobile</span>
                  <input class="field" name="mobile" type="tel" maxlength="30" autocomplete="off" required /></label>
                <label class="form-group"><span class="form-label">Email</span>
                  <input class="field" name="email" type="email" maxlength="254" autocomplete="off" required /></label>
                <label class="form-group"><span class="form-label">Role</span>
                  <select class="field" name="role"><option>Staff</option><option>Counsellor</option><option>Admin</option></select></label>
                <label class="form-group"><span class="form-label">Password (new users only)</span>
                  <input class="field" name="password" type="password" autocomplete="new-password" aria-describedby="user-password-help" /></label>
              </div>
              <p id="user-password-help">New user: enter at least 8 characters with uppercase, lowercase, number and special character. Existing user: leave password blank and use their exact email and mobile. Their existing profile and password will not change.</p>
              <p id="add-user-error" role="alert" hidden></p>
              <p id="add-user-success" role="status" hidden></p>
              <button class="button button-primary" type="submit">Add user</button>
            </form>
          </div>
        </section>`
    });

    const refreshList = FollowUp.teamList.mount($("#team-list"), company);
    if (!FollowUp.team.canManage(company)) return;

    const form = $("#add-user-form");
    const error = form.find("#add-user-error");
    const success = form.find("#add-user-success");
    const button = form.find("button[type=submit]");
    let submitting = false;

    form.on("submit", function (event) {
      event.preventDefault();
      if (submitting || !this.reportValidity()) return;
      success.prop("hidden", true).text("");
      if (!FollowUp.storage.getToken()) {
        FollowUp.router.navigate("/login");
        return;
      }
      const activeCompany = FollowUp.storage.getCompany();
      if (!FollowUp.team.canManage(activeCompany) || activeCompany.company_id !== company.company_id) {
        error.text("Your company session changed. Reload this page before adding a user.").prop("hidden", false);
        return;
      }

      const value = (name) => form.find(`[name="${name}"]`).val();
      const details = {
        name: value("name").trim(),
        mobile: value("mobile").replace(/\D/g, ""),
        email: value("email").trim().toLowerCase(),
        role: value("role"),
        password: value("password") || null
      };
      const message = FollowUp.team.validate(details);
      error.text(message).prop("hidden", !message);
      if (message) return;

      submitting = true;
      button.prop("disabled", true).text("Adding user…");
      form.attr("aria-busy", "true");
      FollowUp.team.add(company.company_id, details)
        .done((user) => {
          form[0].reset();
          refreshList();
          success.text(`${user.name} added as ${user.role}. ${user.is_new_user ? "New user account created." : "Existing account linked/reactivated; password unchanged."}`).prop("hidden", false);
        })
        .fail((failure) => error.text(failure.message || "Unable to add user.").prop("hidden", false))
        .always(() => {
          submitting = false;
          form.find('[name="password"]').val("");
          form.removeAttr("aria-busy");
          button.prop("disabled", false).text("Add user");
        });
    });
  }

  FollowUp.teamView = { render };
})(window);
