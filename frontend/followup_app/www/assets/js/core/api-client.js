(function (window) {
  "use strict";

  const FollowUp = window.FollowUp;

  function buildUrl(path) {
    const base = FollowUp.config.apiBaseUrl.replace(/\/+$/, "");
    const endpoint = path.startsWith("/") ? path : `/${path}`;
    return `${base}${endpoint}`;
  }

  function request(options) {
    const token = FollowUp.storage.getToken();

    return $.ajax({
      url: buildUrl(options.path),
      method: options.method || "GET",
      timeout: FollowUp.config.requestTimeoutMs,
      contentType: "application/json",
      dataType: "json",
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      data: options.data === undefined ? undefined : JSON.stringify(options.data)
    }).then((response) => {
      if (response && Object.prototype.hasOwnProperty.call(response, "success")) {
        if (!response.success) {
          return $.Deferred().reject(new Error(response.message || "Request failed")).promise();
        }
        return response.data;
      }
      return response;
    });
  }

  FollowUp.api = {
    get(path) {
      return request({ path });
    },
    post(path, data) {
      return request({ path, method: "POST", data });
    },
    put(path, data) {
      return request({ path, method: "PUT", data });
    },
    delete(path) {
      return request({ path, method: "DELETE" });
    }
  };
})(window);
