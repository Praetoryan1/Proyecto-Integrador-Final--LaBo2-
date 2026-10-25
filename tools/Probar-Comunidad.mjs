// Recorrido local: crea una publicación propia de prueba y conserva relaciones preexistentes.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

const base = "https://localhost:7280";
const output = path.resolve("docs/evidencias/incremento-05");
const temporary = path.resolve("tmp/video-comunidad");
await mkdir(output, { recursive: true }); await mkdir(temporary, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
const author = await browser.newContext({ ignoreHTTPSErrors: true });
const viewer = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 }, recordVideo: { dir: temporary, size: { width: 1440, height: 1000 } } });
const anonymous = await browser.newContext({ ignoreHTTPSErrors: true });
const page = await viewer.newPage(); page.setDefaultTimeout(20_000);
const authorPage = await author.newPage(); authorPage.setDefaultTimeout(20_000);
const errors = [], network = [];
let post, authorCsrf, viewerCsrf, followAdded = false;
page.on("pageerror", e => errors.push(e.message));
page.on("console", msg => { if (msg.type() === "error") errors.push(msg.text()); });
page.on("response", r => { const url = new URL(r.url()); if (/^\/(mis-favoritos|mis-seguidos|favoritos|seguimientos)/.test(url.pathname)) network.push({ method: r.request().method(), path: url.pathname + url.search, status: r.status() }); });
async function login(target, email) {
    await target.goto(base + "/cuenta/ingresar");
    await target.getByLabel("Correo de acceso").fill(email);
    await target.getByLabel("Contraseña", { exact: true }).fill("Trazo.Demo2026!");
    await target.getByRole("button", { name: "Ingresar →" }).click();
    await target.waitForURL(base + "/panel");
    return target.locator('input[name="__RequestVerificationToken"]').first().inputValue();
}
async function submit(name) {
    await Promise.all([page.waitForNavigation(), page.getByRole("button", { name, exact: true }).click()]);
}
const capture = name => page.screenshot({ path: path.join(output, name + ".png"), fullPage: true });
try {
    authorCsrf = await login(authorPage, "bruno@trazo.test");
    const title = "Cuaderno de referencias · prueba de comunidad " + Date.now();
    const created = await author.request.post(base + "/panel-json/publicaciones", {
        headers: { "X-CSRF-TOKEN": authorCsrf },
        data: { titulo: title, texto: "Un registro de ideas, composición y movimiento. Publicación temporal para comprobar favoritos y privacidad.", tipo: 1, estadoEditorial: 1, visibilidad: 0, etiquetaIds: [] }
    });
    assert.equal(created.status(), 201); post = await created.json();
    const publicResponse = await anonymous.request.get(`${base}/publicaciones/${post.id}`);
    assert.equal(publicResponse.status(), 200);
    assert.ok((await publicResponse.text()).includes("Ingresar para guardar"));
    assert.equal((await anonymous.request.get(base + "/mis-favoritos", { maxRedirects: 0 })).status(), 302);
    viewerCsrf = await login(page, "ana@trazo.test");
    await page.goto(`${base}/publicaciones/${post.id}`);
    await submit("Guardar en favoritos");
    assert.equal(await page.getByRole("button", { name: "Quitar de favoritos", exact: true }).count(), 1);
    await capture("publicacion-guardada");
    await page.getByRole("link", { name: "Ver mis favoritos ↗" }).click();
    assert.ok(await page.locator(`a[href='/publicaciones/${post.id}']`).count());
    await capture("favoritos");
    await page.getByLabel("Buscar favoritos por título").fill("Cuaderno de referencias");
    await submit("Buscar");
    assert.ok(await page.locator(`a[href='/publicaciones/${post.id}']`).count());
    await page.setViewportSize({ width: 390, height: 844 });
    await capture("favoritos-movil");
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.goto(base + "/p/bruno");
    const existed = await page.getByRole("button", { name: "Dejar de seguir", exact: true }).count() > 0;
    if (!existed) { await submit("Seguir creador"); followAdded = true; }
    await capture("perfil-seguido");
    await page.getByRole("link", { name: "Ver mis seguidos ↗" }).click();
    assert.ok(await page.locator('.follow-card a[href="/p/bruno"]').count());
    await capture("seguidos");
    await page.setViewportSize({ width: 390, height: 844 });
    await capture("seguidos-movil");
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await page.setViewportSize({ width: 1440, height: 1000 });
    // El propietario cambia la privacidad: seguirlo y haber guardado su obra no conceden acceso.
    const changed = await author.request.put(`${base}/panel-json/publicaciones/${post.id}`, {
        headers: { "X-CSRF-TOKEN": authorCsrf }, data: { ...post, etiquetaIds: [], visibilidad: 2 }
    });
    assert.equal(changed.status(), 200); post = await changed.json();
    assert.equal((await viewer.request.get(`${base}/publicaciones/${post.id}`)).status(), 404);
    await page.goto(base + "/mis-favoritos?q=" + encodeURIComponent(title));
    assert.equal(await page.locator(`a[href='/publicaciones/${post.id}']`).count(), 0);
    await capture("favorito-oculto");
    // Quitar por ID solo la relación temporal, sin limpiar favoritos existentes del usuario.
    assert.equal((await viewer.request.post(`${base}/favoritos/${post.id}/quitar`, { headers: { "X-CSRF-TOKEN": viewerCsrf }, maxRedirects: 0 })).status(), 302);
    if (followAdded) {
        await page.goto(base + "/p/bruno"); await submit("Dejar de seguir"); followAdded = false;
        assert.equal(await page.getByRole("button", { name: "Seguir creador", exact: true }).count(), 1);
    }
    assert.deepEqual(errors, []);
    await writeFile(path.join(output, "recorrido.json"), JSON.stringify({ fechaUtc: new Date().toISOString(), flujo: "MVC con formularios POST, antiforgery y redirección local", comprobaciones: ["alta y baja de favorito", "filtro privado de favoritos", "seguir y dejar de seguir (conserva relaciones anteriores)", "privacidad efectiva tras guardar y seguir", "visitantes sin acceso a listas", "móvil 390 px sin desborde"], seguimientoPreexistente: existed, erroresJavaScript: errors, peticiones: network }, null, 2));
    console.log("Comunidad verificada: favoritos, seguimiento, privacidad y móvil.");
} finally {
    if (post && viewerCsrf) await viewer.request.post(`${base}/favoritos/${post.id}/quitar`, { headers: { "X-CSRF-TOKEN": viewerCsrf }, maxRedirects: 0 });
    if (followAdded) await viewer.request.post(base + "/seguimientos/bruno/quitar", { headers: { "X-CSRF-TOKEN": viewerCsrf }, maxRedirects: 0 });
    if (post && authorCsrf) {
        const current = await author.request.get(`${base}/panel-json/publicaciones/${post.id}`);
        if (current.ok()) {
            const latest = await current.json();
            const deleted = await author.request.delete(`${base}/panel-json/publicaciones/${post.id}`, { headers: { "X-CSRF-TOKEN": authorCsrf }, data: { version: latest.version } });
            assert.equal(deleted.status(), 204);
        }
    }
    const video = page.video(); await viewer.close();
    if (video) await video.saveAs(path.join(output, "comunidad.webm"));
    await author.close(); await anonymous.close(); await browser.close();
}
