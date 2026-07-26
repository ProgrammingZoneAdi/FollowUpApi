(function (window) {
  "use strict";

  window.FollowUp = window.FollowUp || {};
  window.FollowUp.toast = function (message, type) {
    const toast = $("<div>", {
      class: `toast${type === "error" ? " is-error" : ""}`,
      text: message
    });

    $("#toast-region").append(toast);
    window.setTimeout(() => toast.fadeOut(180, () => toast.remove()), 3200);
  };
})(window);
