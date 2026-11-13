// RaceDay MVC progressive enhancements. Every feature here degrades to a normal form post without JavaScript.
(function () {
  "use strict";

  // Confirm destructive actions: <form data-confirm="Are you sure?">
  document.addEventListener("submit", function (event) {
    var form = event.target;
    if (form.matches && form.matches("form[data-confirm]")) {
      if (!window.confirm(form.getAttribute("data-confirm"))) {
        event.preventDefault();
      }
    }
  });

  // Category builder on the create event form.
  var list = document.getElementById("category-list");
  var template = document.getElementById("category-template");
  var addButton = document.querySelector("[data-add-category]");

  function renumber() {
    var rows = list.querySelectorAll(".rd-cat-row");
    rows.forEach(function (row, position) {
      row.querySelectorAll("[name]").forEach(function (field) {
        if (field.name === "Categories.index") {
          field.value = position;
        } else {
          field.name = field.name.replace(/Categories\[[^\]]*\]/, "Categories[" + position + "]");
        }
      });
    });
  }

  if (list && template && addButton) {
    addButton.addEventListener("click", function () {
      var next = parseInt(list.getAttribute("data-next-index") || "0", 10);
      var html = template.innerHTML.split("__index__").join(String(next));
      var wrapper = document.createElement("div");
      wrapper.innerHTML = html.trim();
      list.appendChild(wrapper.firstElementChild);
      list.setAttribute("data-next-index", String(next + 1));
      renumber();
      var firstField = list.lastElementChild && list.lastElementChild.querySelector("input[type=text], input:not([type=hidden])");
      if (firstField) { firstField.focus(); }
    });

    list.addEventListener("click", function (event) {
      var button = event.target.closest("[data-remove-category]");
      if (!button) { return; }
      var rows = list.querySelectorAll(".rd-cat-row");
      if (rows.length <= 1) {
        window.alert("An event needs at least one category.");
        return;
      }
      button.closest(".rd-cat-row").remove();
      renumber();
    });
  }

  // Show/hide the organiser-only fields on the register form.
  document.querySelectorAll("[data-role-radio]").forEach(function (radio) {
    radio.addEventListener("change", function () {
      document.querySelectorAll("[data-role-field]").forEach(function (block) {
        block.hidden = block.getAttribute("data-role-field") !== radio.value;
      });
    });
  });

  // Show/hide password text.
  document.querySelectorAll("[data-toggle-password]").forEach(function (button) {
    button.addEventListener("click", function () {
      var input = document.getElementById(button.getAttribute("data-toggle-password"));
      if (!input) { return; }
      var show = input.type === "password";
      input.type = show ? "text" : "password";
      button.setAttribute("aria-pressed", show ? "true" : "false");
    });
  });

  // Light / dark theme toggle. The choice is remembered per browser; storage may be unavailable, so it is optional.
  var themeToggle = document.getElementById("rdThemeToggle");
  if (themeToggle) {
    var root = document.documentElement;

    var syncToggle = function () {
      var dark = root.getAttribute("data-theme") === "dark";
      var icon = themeToggle.querySelector("i");
      themeToggle.setAttribute("aria-label", dark ? "Switch to light mode" : "Switch to dark mode");
      if (icon) {
        icon.className = dark ? "fa-solid fa-sun" : "fa-solid fa-moon";
      }
    };

    themeToggle.addEventListener("click", function () {
      var next = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      root.setAttribute("data-theme", next);
      try { window.localStorage.setItem("raceday_theme_v1", next); } catch (e) { /* private mode: keep the in-memory choice only */ }
      syncToggle();
    });

    syncToggle();
  }
})();
