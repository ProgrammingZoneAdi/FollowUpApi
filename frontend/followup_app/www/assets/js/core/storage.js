(function (window) {
  "use strict";

  const keys = {
    session: "followup.auth.session",
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
      return this.getSession()?.access_token || "";
    },
    getSession() {
      try {
        const raw = window.sessionStorage.getItem(keys.session) || window.localStorage.getItem(keys.session);
        const session = raw ? JSON.parse(raw) : null;
        if (!session || !session.access_token || !session.user || !Array.isArray(session.companies)
          || !Number.isFinite(Date.parse(session.expires_on)) || Date.parse(session.expires_on) <= Date.now()) {
          this.clearSession();
          return null;
        }
        return session;
      } catch {
        return null;
      }
    },
    setSession(session, remember = false) {
      if (!session?.access_token || !session.user || !Array.isArray(session.companies)
        || !Number.isFinite(Date.parse(session.expires_on)) || Date.parse(session.expires_on) <= Date.now()) {
        throw new Error("Invalid login response. Please try again.");
      }
      this.clearSession();
      const storage = remember ? window.localStorage : window.sessionStorage;
      storage.setItem(keys.session, JSON.stringify({
        access_token: session.access_token,
        expires_on: session.expires_on,
        user: session.user,
        companies: session.companies,
        active_company_id: session.companies[0]?.company_id || null
      }));
    },
    getUser() {
      const session = this.getSession();
      const company = this.getCompany();
      return { ...session?.user, name: session?.user.name || "User", company: company?.company_name || "No active company" };
    },
    getCompany() {
      const session = this.getSession();
      return session?.companies.find((item) => item.company_id === session.active_company_id) || null;
    },
    clearSession() {
      for (const storage of [window.localStorage, window.sessionStorage]) {
        storage.removeItem(keys.session);
        storage.removeItem("followup.auth.token");
        storage.removeItem("followup.auth.user");
      }
    }
  };
})(window);
