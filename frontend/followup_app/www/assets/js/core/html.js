(function (window) {
  "use strict";

  window.FollowUp = window.FollowUp || {};
  window.FollowUp.escapeHtml = function (value) {
    return $("<div>").text(value == null ? "" : String(value)).html();
  };
})(window);
