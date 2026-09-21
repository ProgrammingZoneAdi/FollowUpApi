(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;
  FollowUp.team = {
    canManage(company) {
      return !!company && ["owner", "admin"].includes(String(company.role).toLowerCase());
    },
    validate(details) {
      if (!details.name || details.name.length > 150) return "Name is required and cannot exceed 150 characters.";
      if (!/^\d{10,15}$/.test(details.mobile)) return "Mobile number must contain 10 to 15 digits.";
      if (!details.email || details.email.length > 254) return "A valid email is required (maximum 254 characters).";
      if (!["Admin", "Counsellor", "Staff"].includes(details.role)) return "Choose Admin, Counsellor or Staff.";
      const password = details.password;
      if (password && (password.length < 8 || !/\p{Lu}/u.test(password) || !/\p{Ll}/u.test(password)
        || !/\p{Nd}/u.test(password) || !/[^\p{L}\p{N}]/u.test(password))) {
        return "Password must contain at least 8 characters with uppercase, lowercase, number and special character.";
      }
      return "";
    },
    add(companyId, details) {
      return FollowUp.api.post(`/api/companies/${encodeURIComponent(companyId)}/users`, details);
    },
    list(companyId, filters = {}) {
      const query = new URLSearchParams({
        page: filters.page || 1,
        page_size: filters.page_size || 20,
        search: (filters.search || "").trim(),
        role: filters.role || "All",
        status: filters.status || "Active"
      });
      return FollowUp.api.get(`/api/companies/${encodeURIComponent(companyId)}/users?${query}`);
    }
  };
})(window);
