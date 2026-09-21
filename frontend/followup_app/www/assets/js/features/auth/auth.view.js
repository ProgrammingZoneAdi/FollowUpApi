(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  function render() {
    if (FollowUp.storage.getToken()) {
      FollowUp.router.navigate("/dashboard");
      return;
    }

    $("#app").html(`
      <main class="login-page">
        <section class="login-visual">
          <div class="login-brand"><span class="brand-mark">F</span> FollowUp</div>
          <div class="login-story">
            <div class="eyebrow">Conversations that convert</div>
            <h1>Turn every enquiry into a clear next step.</h1>
            <p>Bring leads, reminders and your team into one calm workspace—available on web and mobile.</p>
          </div>
        </section>
        <section class="login-form-side">
          <div class="login-card">
            <h2>Welcome back</h2>
            <p>Sign in to continue to your company workspace.</p>
            <form class="login-form" id="login-form">
              <label class="form-group">
                <span class="form-label">Email or mobile number</span>
                <input class="field" id="identification" autocomplete="username" required
                  placeholder="owner@example.com" />
              </label>
              <label class="form-group">
                <span class="form-label">Password</span>
                <input class="field" id="password" type="password" autocomplete="current-password"
                  minlength="8" required placeholder="Enter your password" />
              </label>
              <div class="login-meta">
                <label class="checkbox-row"><input id="remember-me" type="checkbox" /> Remember me</label>
              </div>
              <button class="button button-primary full-width" id="login-button" type="submit">
                Sign in
              </button>
              <p id="login-error" role="alert" hidden></p>
            </form>
            <div class="demo-note">
              Login and company registration are live. Other workspace data is still demo data.
            </div>
            <p><a href="#/onboard">Create your company account</a></p>
          </div>
        </section>
      </main>
    `);

    let submitting = false;
    function completeLogin(credentials) {
      if (submitting) return;
      submitting = true;
      const remember = $("#remember-me").prop("checked");
      const errorMessage = $("#login-error").text("").prop("hidden", true);
      const button = $("#login-button").prop("disabled", true).text("Signing in…");
      FollowUp.auth
        .login(credentials)
        .done((session) => {
          try {
            FollowUp.storage.setSession(session, remember);
            $("#password").val("");
            FollowUp.router.navigate("/dashboard");
          } catch (error) {
            errorMessage.text(error.message || "Unable to save session. Check browser storage settings.").prop("hidden", false);
          }
        })
        .fail((error) => errorMessage.text(error.message || "Unable to sign in.").prop("hidden", false))
        .always(() => {
          submitting = false;
          button.prop("disabled", false).text("Sign in");
        });
    }

    $("#login-form").on("submit", function (event) {
      event.preventDefault();
      if (!this.reportValidity()) return;
      completeLogin({
        identification: $("#identification").val().trim(),
        password: $("#password").val()
      });
    });

  }

  FollowUp.authView = { render };
})(window);
