(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  function render() {
    $("#app").html(`
      <main class="login-page">
        <section class="login-visual">
          <div class="login-brand"><span class="brand-mark">F</span> FollowUp</div>
          <div class="login-story">
            <div class="eyebrow">Your team's next chapter</div>
            <h1>Give every enquiry a place to grow.</h1>
            <p>Create your company and its owner account to get started.</p>
          </div>
        </section>
        <section class="login-form-side">
          <div class="login-card">
            <h2>Create your company</h2>
            <p>This registers a real company and owner account.</p>
            <form class="login-form" id="onboard-form">
              <label class="form-group"><span class="form-label">Company name</span>
                <input class="field" name="company_name" autocomplete="organization" maxlength="200" required /></label>
              <label class="form-group"><span class="form-label">Owner name</span>
                <input class="field" name="owner_name" autocomplete="name" maxlength="150" required /></label>
              <label class="form-group"><span class="form-label">Mobile number</span>
                <input class="field" name="mobile" type="tel" autocomplete="tel" maxlength="30" required /></label>
              <label class="form-group"><span class="form-label">Email</span>
                <input class="field" name="email" type="email" autocomplete="email" maxlength="254" required /></label>
              <label class="form-group"><span class="form-label">Password</span>
                <input class="field" name="password" type="password" autocomplete="new-password" minlength="8" required aria-describedby="password-help" /></label>
              <small id="password-help">At least 8 characters, including uppercase, lowercase, a number and a special character.</small>
              <label class="form-group"><span class="form-label">Confirm password</span>
                <input class="field" name="confirm_password" type="password" autocomplete="new-password" required /></label>
              <p id="onboard-error" role="alert" hidden></p>
              <button class="button button-primary full-width" type="submit">Create company</button>
            </form>
            <section id="onboard-success" role="status" hidden>
              <h3>Company created successfully</h3>
              <p>Your owner account is ready. Sign in with the email/mobile and password you just registered.</p>
              <p>Company ID: <span id="created-company-id"></span></p>
            </section>
            <p><a href="#/login">Back to sign in</a></p>
          </div>
        </section>
      </main>
    `);

    const form = $("#onboard-form");
    const error = $("#onboard-error");
    const button = form.find("button[type=submit]");
    let submitting = false;

    form.on("submit", function (event) {
      event.preventDefault();
      if (submitting || !this.reportValidity()) return;

      const value = (name) => form.find(`[name="${name}"]`).val();
      const details = {
        company_name: value("company_name").trim(),
        owner_name: value("owner_name").trim(),
        mobile: value("mobile").replace(/\D/g, ""),
        email: value("email").trim().toLowerCase(),
        password: value("password")
      };
      let message = "";
      if (!details.company_name || !details.owner_name) message = "Company and owner names are required.";
      else if (!/^\d{10,15}$/.test(details.mobile)) message = "Mobile number must contain 10 to 15 digits.";
      else if (details.password.length < 8 || !/\p{Lu}/u.test(details.password) || !/\p{Ll}/u.test(details.password)
        || !/\p{Nd}/u.test(details.password) || !/[^\p{L}\p{N}]/u.test(details.password)) message = "Password must meet all the requirements shown above.";
      else if (details.password !== value("confirm_password")) message = "Passwords do not match.";

      error.text(message).prop("hidden", !message);
      if (message) return;

      submitting = true;
      button.prop("disabled", true).text("Creating company…");
      form.attr("aria-busy", "true");
      FollowUp.auth.onboard(details)
        .done((result) => {
          form[0].reset();
          form.prop("hidden", true);
          $("#created-company-id").text(result.company_id);
          $("#onboard-success").prop("hidden", false);
        })
        .fail((failure) => error.text(failure.message || "Unable to create company.").prop("hidden", false))
        .always(() => {
          submitting = false;
          form.removeAttr("aria-busy");
          button.prop("disabled", false).text("Create company");
        });
    });
  }

  FollowUp.onboardView = { render };
})(window);
