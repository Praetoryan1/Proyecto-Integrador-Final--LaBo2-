"use strict";
(() => {
  const preview = document.getElementById("vista-previa-perfil");
  if (!preview) return;
  const accents = ["#c4f566", "#c4b5fd", "#7dd3fc", "#fda4af", "#fdba74"];
  const controls = ["Tema", "Disposicion", "ColorAcento", "NombreVisible", "Especialidad"].map(id => document.getElementById(id));
  function update() {
    const [theme, layout, accent, name, specialty] = controls;
    const color = Math.max(0, accents.indexOf(accent.value));
    preview.className = `profile-surface appearance-preview theme-${theme.value === "1" ? "claro" : "oscuro"} layout-${layout.value === "1" ? "lista" : "cuadricula"} accent-${color}`;
    preview.querySelector("[data-preview-name]").textContent = name.value.trim() || "Tu portafolio";
    preview.querySelector("[data-preview-specialty]").textContent = specialty.value.trim() || "Tu especialidad";
  }
  controls.forEach(control => control.addEventListener("input", update));
  update();
})();
