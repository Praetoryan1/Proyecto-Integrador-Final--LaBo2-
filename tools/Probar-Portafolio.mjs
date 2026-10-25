// Obras temporales y preferencias demo restauradas al finalizar. No guarda formularios ni credenciales en evidencias.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
const base = "https://localhost:7280", output = path.resolve("docs/evidencias/incremento-08"), videoDir = path.resolve("tmp/video-portafolio");
await mkdir(output, { recursive: true }); await mkdir(videoDir, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
const options = { ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } };
const owner = await browser.newContext({ ...options, recordVideo: { dir: videoDir, size: options.viewport } });
const visitor = await browser.newContext(options);
const page = await owner.newPage(), publicPage = await visitor.newPage();
const posts = [], errors = [], network = [], contrast = [];
let csrf, originalProfile, changedProfile = false, completed = false;
for (const p of [page, publicPage]) {
    p.setDefaultTimeout(20_000); p.on("pageerror", e => errors.push(e.message));
    p.on("response", r => { const u = new URL(r.url()); if (u.pathname.startsWith("/mi-portafolio/")) network.push({ metodo: r.request().method(), ruta: u.pathname, estado: r.status() }); });
}
async function submit(target, button) { await Promise.all([target.waitForNavigation(), button.click()]); }
const capture = (p, name) => p.screenshot({ path: path.join(output, name + ".png"), fullPage: true });
try {
    await page.goto(base + "/cuenta/ingresar"); await page.getByLabel("Correo de acceso").fill("ana@trazo.test");
    await page.getByLabel("Contraseña", { exact: true }).fill("Trazo.Demo2026!"); await page.getByRole("button", { name: "Ingresar →" }).click(); await page.waitForURL(base + "/panel");
    csrf = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue();
    await page.goto(base + "/mi-portafolio/destacados");
    const used = await page.locator(".featured-choice .eyebrow").allTextContents();
    const occupied = used.map(t => Number(t.match(/POSICIÓN\s+(\d+)/)[1]));
    const free = [1, 2, 3, 4, 5, 6].filter(p => !occupied.includes(p));
    assert.ok(free.length >= 2, "La evidencia requiere dos posiciones libres; no reemplaza obras preexistentes.");
    for (const [i, title] of ["Ritmo y forma", "Un mundo de color"].entries()) {
        const create = await owner.request.post(base + "/panel-json/publicaciones", { headers: { "X-CSRF-TOKEN": csrf }, data: {
            titulo: title + " · prueba de portafolio", texto: "Obra temporal de demostración para comprobar selección, apariencia y permisos.", tipo: 0, estadoEditorial: 0, visibilidad: i === 0 ? 0 : 1, etiquetaIds: []
        } });
        assert.equal(create.status(), 201); const created = await create.json(); posts.push(created.id);
        const media = await owner.request.post(`${base}/panel-json/publicaciones/${created.id}/recursos/youtube`, { headers: { "X-CSRF-TOKEN": csrf }, data: {
            version: created.version, url: "https://www.youtube.com/watch?v=abcdefghijk", textoAlternativo: "Referencia ficticia, sin cargar el reproductor", creditos: "Demostración académica"
        } });
        assert.equal(media.status(), 200);
        const draft = await media.json();
        assert.equal((await owner.request.put(`${base}/panel-json/publicaciones/${created.id}`, { headers: { "X-CSRF-TOKEN": csrf }, data: { ...draft, estadoEditorial: 1, etiquetaIds: [] } })).status(), 200);
        await page.goto(base + "/mi-portafolio/destacados?q=" + encodeURIComponent(title));
        const card = page.locator(`.featured-candidate[data-id="${created.id}"]`);
        await card.locator("select").selectOption(String(free[i]));
        await submit(page, card.getByRole("button", { name: "Destacar obra" }));
    }
    await capture(page, "seleccion");
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false); await capture(page, "seleccion-movil");
    await page.setViewportSize(options.viewport);
    await page.goto(base + "/mi-perfil");
    originalProfile = await page.locator('form[action="/mi-perfil"]').evaluate(form => new URLSearchParams(new FormData(form)).toString());
    const colors = ["#c4f566", "#c4b5fd", "#7dd3fc", "#fda4af", "#fdba74"];
    for (const theme of ["0", "1"]) for (const color of colors) {
        await page.locator("#Tema").selectOption(theme); await page.locator("#ColorAcento").selectOption(color);
        const ratio = await page.locator("#vista-previa-perfil").evaluate(el => {
            const luminance = rgb => { const parts = rgb.match(/[\d.]+/g).slice(0, 3).map(Number).map(n => n / 255).map(n => n <= 0.04045 ? n / 12.92 : ((n + 0.055) / 1.055) ** 2.4); return parts[0] * .2126 + parts[1] * .7152 + parts[2] * .0722; };
            const a = luminance(getComputedStyle(el.querySelector(".accent")).color), b = luminance(getComputedStyle(el).backgroundColor);
            return (Math.max(a, b) + .05) / (Math.min(a, b) + .05);
        });
        assert.ok(ratio >= 4.5); contrast.push({ tema: theme === "0" ? "Oscuro" : "Claro", color, contrasteAcentoFondo: Number(ratio.toFixed(2)) });
    }
    await page.locator("#Tema").selectOption("1"); await page.locator("#Disposicion").selectOption("1"); await page.locator("#ColorAcento").selectOption("#fda4af");
    assert.ok((await page.locator("#vista-previa-perfil").getAttribute("class")).includes("theme-claro layout-lista accent-3"));
    await page.locator("#vista-previa-perfil").screenshot({ path: path.join(output, "vista-previa.png") });
    changedProfile = true; await submit(page, page.getByRole("button", { name: "Guardar cambios →" }));
    await publicPage.goto(base + "/p/ana");
    const cover = publicPage.locator("#obras-destacadas .work-cover").first();
    const inkBefore = await cover.evaluate(el => getComputedStyle(el).color); await cover.hover();
    assert.equal(await cover.evaluate(el => getComputedStyle(el).color), inkBefore);
    await publicPage.locator(".brand").first().hover();
    assert.equal(await publicPage.locator(`#obras-destacadas a[href="/publicaciones/${posts[0]}"]`).count(), 1);
    assert.equal(await publicPage.locator(`#obras-destacadas a[href="/publicaciones/${posts[1]}"]`).count(), 0);
    await capture(publicPage, "perfil-claro-lista");
    await publicPage.setViewportSize({ width: 390, height: 844 });
    assert.equal(await publicPage.evaluate(() => document.documentElement.scrollWidth > innerWidth), false); await capture(publicPage, "perfil-movil");
    await page.goto(base + "/p/ana"); assert.equal(await page.locator(`#obras-destacadas a[href="/publicaciones/${posts[1]}"]`).count(), 1);
    const current = await (await owner.request.get(`${base}/panel-json/publicaciones/${posts[0]}`)).json();
    assert.equal((await owner.request.put(`${base}/panel-json/publicaciones/${posts[0]}`, { headers: { "X-CSRF-TOKEN": csrf }, data: { ...current, visibilidad: 2, etiquetaIds: [] } })).status(), 200);
    await page.goto(base + "/mi-portafolio/destacados");
    const hidden = page.locator(`.featured-choice[data-id="${posts[0]}"]`);
    assert.ok((await hidden.textContent()).includes("NO SE MUESTRA ACTUALMENTE")); await capture(page, "obra-privada");
    await hidden.locator("select").selectOption("0"); await submit(page, hidden.getByRole("button", { name: "Guardar posición" }));
    assert.deepEqual(errors, []); completed = true;
    console.log("Portafolio verificado: destacados, privacidad, apariencia, contraste y móvil.");
} finally {
    if (changedProfile && originalProfile) {
        const restored = await owner.request.post(base + "/mi-perfil", { headers: { "Content-Type": "application/x-www-form-urlencoded" }, data: originalProfile, maxRedirects: 0 });
        assert.equal(restored.status(), 302);
    }
    for (const id of posts) {
        const current = await owner.request.get(`${base}/panel-json/publicaciones/${id}`);
        if (current.ok()) {
            const latest = await current.json(); const removed = await owner.request.delete(`${base}/panel-json/publicaciones/${id}`, { headers: { "X-CSRF-TOKEN": csrf }, data: { version: latest.version } });
            assert.equal(removed.status(), 204);
        }
    }
    await writeFile(path.join(output, "recorrido.json"), JSON.stringify({ fechaUtc: new Date().toISOString(), completado: completed,
        obrasTemporalesEliminadas: posts, preferenciasRestauradas: changedProfile, erroresJavaScript: errors, contrastes: contrast, peticiones: network }, null, 2));
    const video = page.video(); await owner.close(); if (video) await video.saveAs(path.join(output, "portafolio.webm"));
    await visitor.close(); await browser.close();
}
