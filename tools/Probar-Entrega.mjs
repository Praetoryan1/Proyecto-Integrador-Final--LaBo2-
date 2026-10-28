// Prueba de lectura de la demo: no crea ni borra obras. No guarda cookies ni JWT.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
const restored = process.argv.includes("--restaurada");
const base = restored ? "https://localhost:7282" : "https://localhost:7281";
const output = path.resolve("docs/evidencias/incremento-09", restored ? "restaurada" : "semilla");
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
const checks = [], errors = [], contexts = [];
function check(name, actual, expected) { assert.deepEqual(actual, expected, name); checks.push({ comprobacion: name, resultado: actual }); }
async function context() {
    const c = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } }); contexts.push(c); return c;
}
async function login(c, user) {
    const p = await c.newPage();
    await p.goto(base + "/cuenta/ingresar");
    await p.getByLabel("Correo de acceso").fill(user + "@trazo.test");
    await p.getByLabel("Contraseña", { exact: true }).fill("Trazo.Demo2026!");
    await p.getByRole("button", { name: "Ingresar →" }).click(); await p.waitForURL(base + "/panel");
    checks.push({ comprobacion: "Login " + user, resultado: true }); await p.close();
}
try {
    const anon = await context(), ana = await context(), bruno = await context(), admin = await context();
    const p = await anon.newPage(); p.on("pageerror", e => errors.push(e.message));
    await p.goto(base + "/"); check("Página pública 1", await p.locator(".work-card").count(), 12);
    await p.locator("img").evaluateAll(images => Promise.all(images.map(img => { img.loading = "eager"; return img.decode(); })));
    check("Portadas decodificadas", await p.locator(".work-card img").count(), 4);
    await p.screenshot({ path: path.join(output, "explorar.png"), fullPage: true });
    await p.goto(base + "/?pagina=2"); check("Página pública 2", await p.locator(".work-card").count(), 10);
    await p.goto(base + "/p/ana"); check("Destacados públicos Ana", await p.locator("#obras-destacadas .work-card").count(), 2);
    await p.locator("img").evaluateAll(images => Promise.all(images.map(img => { img.loading = "eager"; return img.decode(); })));
    await p.setViewportSize({ width: 390, height: 844 });
    check("Perfil móvil sin desborde", await p.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await p.screenshot({ path: path.join(output, "perfil-movil.png"), fullPage: true });
    for (const [c, u] of [[ana, "ana"], [bruno, "bruno"], [admin, "admin"]]) await login(c, u);
    const editor = await ana.newPage(); editor.on("pageerror", e => errors.push(e.message));
    await editor.goto(base + "/panel/publicaciones");
    await editor.locator(".post-row").first().waitFor();
    check("Bundle Vue incluido renderiza el listado", await editor.locator(".post-row").count(), 12);
    await editor.close();
    check("Creador sin rol administrador", (await ana.request.get(base + "/administracion/cuentas", { maxRedirects: 0 })).status(), 302);
    for (const route of ["/administracion/cuentas", "/administracion/reportes"]) check(route, (await admin.request.get(base + route)).status(), 200);
    for (const route of ["/mis-favoritos", "/mis-seguidos", "/mi-portafolio/destacados", "/panel/publicaciones"]) check(route, (await ana.request.get(base + route)).status(), 200);
    for (const u of ["ana", "bruno", "admin"]) check("Avatar " + u, (await anon.request.get(base + `/p/${u}/avatar`)).status(), 200);
    const works = [
        [ana, "Atlas de formas", true], [ana, "Ritmo circular", true], [ana, "Proceso para la comunidad", false],
        [bruno, "Movimiento en un segundo", true], [bruno, "Identidad en color", true],
        [bruno, "Propuesta reservada", false], [bruno, "Obra en revisión", false]
    ];
    for (const [owner, title, visible] of works) {
        const listing = await (await owner.request.get(base + "/panel-json/publicaciones?q=" + encodeURIComponent(title))).json();
        const item = listing.items.find(i => i.titulo === title); assert.ok(item, title);
        const post = await (await owner.request.get(base + "/panel-json/publicaciones/" + item.id)).json();
        const resource = post.recursos[0]; assert.ok(resource);
        const url = base + `/medios/${resource.id}/archivo`;
        check(title + ": visitante", (await anon.request.get(url)).status(), visible ? 200 : 404);
        check(title + ": propietario", (await owner.request.get(url)).status(), 200);
        check(title + ": miniatura visitante", (await anon.request.get(base + `/medios/${resource.id}/vista-previa`)).status(), visible ? 200 : 404);
        if (title === "Propuesta reservada") check("Admin no abre obra privada", (await admin.request.get(url)).status(), 404);
        if (title === "Proceso para la comunidad") check("Otra cuenta accede a registrados", (await bruno.request.get(url)).status(), 200);
        if (title === "Movimiento en un segundo") {
            const partial = await anon.request.get(url, { headers: { Range: "bytes=0-99" } });
            check("MP4 rango HTTP", partial.status(), 206); check("MP4 bytes recibidos", (await partial.body()).length, 100);
            await p.goto(base + "/publicaciones/" + item.id);
            await p.locator("video").evaluate(async v => { await v.play(); });
            await p.waitForFunction(() => document.querySelector("video").ended);
            check("Video reproducido en Edge", await p.locator("video").evaluate(v => v.duration > 0 && !v.error), true);
        }
        if (title === "Ritmo circular") {
            await p.goto(base + "/publicaciones/" + item.id);
            await p.getByRole("button", { name: "Reproducir GIF" }).click();
            check("GIF activado", await p.locator("[data-gif-toggle]").getAttribute("aria-pressed"), "true");
        }
    }
    check("Paquete demo fuera de raíz pública", (await anon.request.get(base + "/DemoAssets/medios/" + "b".repeat(31) + "6.png")).status(), 404);
    check("API rechaza cookies sin JWT", (await ana.request.get(base + "/api/v1/publicaciones")).status(), 401);
    const token = await anon.request.post(base + "/api/v1/auth/token", { data: { email: "ana@trazo.test", password: "Trazo.Demo2026!" } });
    check("Emisión JWT local", token.status(), 200);
    const { accessToken } = await token.json();
    check("Lectura REST Bearer", (await anon.request.get(base + "/api/v1/publicaciones", { headers: { Authorization: "Bearer " + accessToken } })).status(), 200);
    check("Errores JavaScript", errors, []);
    await writeFile(path.join(output, "resultado.json"), JSON.stringify({ fecha: new Date().toISOString(), origen: restored ? "SQL en copia limpia de archivos versionables" : "Semilla completa aislada", comprobaciones: checks }, null, 2));
    console.log(`${checks.length} comprobaciones aprobadas (${restored ? "restaurada" : "semilla"}).`);
} finally {
    for (const c of contexts) await c.close();
    await browser.close();
}
