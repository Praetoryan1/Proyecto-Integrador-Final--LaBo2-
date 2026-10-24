"use strict";
const menu = document.querySelector(".mobile-toggle");
menu?.addEventListener("click", () => {
  const open = menu.getAttribute("aria-expanded") !== "true";
  menu.setAttribute("aria-expanded", String(open));
  document.getElementById("nav-links")?.classList.toggle("is-open", open);
});
document.querySelectorAll("form[data-confirm]").forEach((form) => {
  form.addEventListener("submit", (event) => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
  });
});
document.querySelectorAll("img[data-avatar-fallback]").forEach((img) => {
  const fallback = () => {
    img.src = "/favicon.svg";
  };
  img.addEventListener("error", fallback, { once: true });
  if (img.complete && img.naturalWidth === 0) fallback();
});
