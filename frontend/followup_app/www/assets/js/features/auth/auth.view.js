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
                <label class="checkbox-row"><input type="checkbox" checked /> Remember me</label>
                <button class="button button-ghost" type="button">Forgot password?</button>
              </div>
              <button class="button button-primary full-width" id="login-button" type="submit">
                Sign in
              </button>
              <button class="button button-secondary full-width" id="demo-button" type="button">
                Enter demo workspace
              </button>
            </form>
            <div class="demo-note">
              API mock mode is enabled. Real authentication will be connected after the login endpoint is built.
            </div>
          </div>
        </section>
      </main>
    `);

    function completeLogin(credentials) {
      const button = $("#login-button").prop("disabled", true).text("Signing in…");
      FollowUp.auth
        .login(credentials)
        .done((session) => {
          FollowUp.storage.setSession(session);
          FollowUp.router.navigate("/dashboard");
        })
        .fail((error) => FollowUp.toast(error.message || "Unable to sign in.", "error"))
        .always(() => button.prop("disabled", false).text("Sign in"));
    }

    $("#login-form").on("submit", function (event) {
      event.preventDefault();
      completeLogin({
        identification: $("#identification").val().trim(),
        password: $("#password").val()
      });
    });

    $("#demo-button").on("click", () =>
      completeLogin({ identification: "demo@followup.local", password: "demo-only" })
    );
  }

  FollowUp.authView = { render };
})(window);
