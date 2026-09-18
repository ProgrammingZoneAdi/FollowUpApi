(function (window) {
  "use strict";

  const keys = {
    token: "followup.auth.token",
    user: "followup.auth.user",
    leads: "followup.demo.leads"
  };

  window.FollowUp = window.FollowUp || {};
  window.FollowUp.storage = {
    keys,
    get(key, fallback) {
      try {
        const value = window.localStorage.getItem(key);
        return value === null ? fallback : JSON.parse(value);
      } catch {
        return fallback;
      }
    },
    set(key, value) {
      window.localStorage.setItem(key, JSON.stringify(value));
    },
    remove(key) {
      window.localStorage.removeItem(key);
    },
    getToken() {
      return this.get(keys.token, "");
    },
    setSession(session) {
      this.set(keys.token, session.token);
      this.set(keys.user, session.user);
    },
    getUser() {
      return this.get(keys.user, { name: "Demo Owner", company: "Acme Learning" });
    },
    clearSession() {
      this.remove(keys.token);
      this.remove(keys.user);
    }
  };
})(window);
