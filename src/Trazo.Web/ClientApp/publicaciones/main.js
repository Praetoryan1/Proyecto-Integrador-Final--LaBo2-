import { createApp } from "vue";
import PublicacionesApp from "./PublicacionesApp.vue";

const root = document.getElementById("publicaciones-app");
const csrf = root.querySelector(
  'input[name="__RequestVerificationToken"]',
).value;
createApp(PublicacionesApp, { csrf }).mount(root);
