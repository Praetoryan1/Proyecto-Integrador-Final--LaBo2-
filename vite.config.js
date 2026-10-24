import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

// Un único módulo servido por MVC, sin servidor Node en ejecución.
// El nombre estable permite asp-append-version; el contenido compilado se versiona.
export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: "src/Trazo.Web/wwwroot/js/publicaciones",
    emptyOutDir: false,
    sourcemap: false,
    lib: {
      entry: "src/Trazo.Web/ClientApp/publicaciones/main.js",
      formats: ["es"],
      fileName: () => "editor.js",
    },
  },
  define: { "process.env.NODE_ENV": JSON.stringify("production") },
});
