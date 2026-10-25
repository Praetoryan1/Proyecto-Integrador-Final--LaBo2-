// Evidencia local: conserva dos reportes resueltos y archiva la publicación demo al terminar.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

const base = "https://localhost:7280";
const output = path.resolve("docs/evidencias/incremento-06");
const videoDir = path.resolve("tmp/video-reportes");
await mkdir(output, { recursive: true }); await mkdir(videoDir, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
const options = { ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } };
const admin = await browser.newContext({ ...options, recordVideo: { dir: videoDir, size: options.viewport } });
const author = await browser.newContext(options);
const viewer = await browser.newContext(options);
const anonymous = await browser.newContext(options);
const page = await admin.newPage(), ownerPage = await author.newPage(), reporterPage = await viewer.newPage();
const errors = [], network = [];
for (const p of [page, ownerPage, reporterPage]) {
    p.setDefaultTimeout(20_000);
    p.on("pageerror", e => errors.push(e.message));
    p.on("response", r => { const u = new URL(r.url()); if (/reportes|reportar/.test(u.pathname)) network.push({ metodo: r.request().method(), ruta: u.pathname, estado: r.status() }); });
}
async function login(p, email) {
    await p.goto(base + "/cuenta/ingresar");
    await p.getByLabel("Correo de acceso").fill(email);
    await p.getByLabel("Contraseña", { exact: true }).fill("Trazo.Demo2026!");
    await p.getByRole("button", { name: "Ingresar →" }).click();
    await p.waitForURL(base + "/panel");
    return p.locator('input[name="__RequestVerificationToken"]').first().inputValue();
}
async function submit(p, name) { await Promise.all([p.waitForNavigation(), p.getByRole("button", { name, exact: true }).click()]); }
const capture = (p, name) => p.screenshot({ path: path.join(output, name + ".png"), fullPage: true });
let post, csrf, reportId, appealId;
try {
    csrf = await login(ownerPage, "bruno@trazo.test");
    const created = await author.request.post(base + "/panel-json/publicaciones", {
        headers: { "X-CSRF-TOKEN": csrf }, data: {
            titulo: "Estudio de movimiento · demostración de moderación", texto: "Material ficticio del proyecto académico. Esta publicación permite demostrar una denuncia, la revisión del autor y una resolución administrativa.",
            tipo: 1, estadoEditorial: 1, visibilidad: 0, etiquetaIds: []
        }
    });
    assert.equal(created.status(), 201); post = await created.json();
    await login(reporterPage, "ana@trazo.test");
    await reporterPage.goto(`${base}/publicaciones/${post.id}`);
    await reporterPage.getByRole("link", { name: "Reportar publicación", exact: true }).click();
    await reporterPage.getByLabel("Motivo", { exact: true }).selectOption("Derechos de autor");
    await reporterPage.getByLabel("Detalle", { exact: true }).fill("Caso ficticio para la defensa: falta aclarar la autoría del material. Solicito revisar sus créditos.");
    await capture(reporterPage, "formulario-reporte");
    await submit(reporterPage, "Enviar solicitud");
    reportId = Number(reporterPage.url().split("/").pop()); assert.ok(reportId > 0);
    await capture(reporterPage, "mi-reporte");
    assert.equal((await anonymous.request.get(`${base}/publicaciones/${post.id}`)).status(), 200);
    await login(page, "admin@trazo.test");
    await page.getByRole("link", { name: "Moderar reportes ↗" }).click();
    await page.getByLabel("Buscar por motivo o título disponible").fill("Estudio de movimiento");
    await submit(page, "Buscar");
    await capture(page, "bandeja");
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await capture(page, "bandeja-movil");
    await page.setViewportSize(options.viewport);
    await page.goto(`${base}/administracion/reportes/${reportId}`);
    await page.getByLabel("Decisión", { exact: true }).selectOption("1");
    await page.getByLabel("Resolución", { exact: true }).fill("Demostración: se oculta temporalmente la publicación hasta que se aclare la autoría y se actualicen los créditos.");
    await capture(page, "decision");
    await submit(page, "Registrar resolución");
    assert.equal((await anonymous.request.get(`${base}/publicaciones/${post.id}`)).status(), 404);
    await ownerPage.goto(`${base}/publicaciones/${post.id}`);
    assert.ok((await ownerPage.textContent("main")).includes("Oculta por moderación"));
    await capture(ownerPage, "autor-oculta");
    await ownerPage.getByRole("link", { name: "Solicitar revisión ↗" }).click();
    await ownerPage.getByLabel("Detalle", { exact: true }).fill("Solicitud ficticia: confirmo que es material propio del proyecto académico y pido revisar la decisión.");
    await submit(ownerPage, "Enviar solicitud");
    appealId = Number(ownerPage.url().split("/").pop()); assert.ok(appealId > reportId);
    await page.goto(`${base}/administracion/reportes/${appealId}`);
    await page.getByLabel("Decisión", { exact: true }).selectOption("2");
    await page.getByLabel("Resolución", { exact: true }).fill("Revisión ficticia aprobada: se aclaró la autoría y se restablece la visibilidad de esta demostración.");
    await capture(page, "revision");
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await capture(page, "revision-movil");
    await page.setViewportSize(options.viewport);
    await submit(page, "Registrar resolución");
    await capture(page, "resolucion");
    assert.equal((await anonymous.request.get(`${base}/publicaciones/${post.id}`)).status(), 200);
    assert.deepEqual(errors, []);
    console.log("Reportes verificados: denuncia, ocultamiento, revisión, restauración y móvil.");
} finally {
    // Una obra con reportes no se elimina: conserva el historial y se archiva fuera de Explorar.
    let archived = false;
    if (post && csrf) {
        const current = await author.request.get(`${base}/panel-json/publicaciones/${post.id}`);
        if (current.ok()) {
            const latest = await current.json();
            const result = await author.request.put(`${base}/panel-json/publicaciones/${post.id}`, {
                headers: { "X-CSRF-TOKEN": csrf }, data: { ...latest, estadoEditorial: 2, etiquetaIds: [] }
            });
            assert.equal(result.status(), 200); archived = true;
        }
    }
    await writeFile(path.join(output, "recorrido.json"), JSON.stringify({ fechaUtc: new Date().toISOString(), publicacionDemo: post?.id,
        reporte: reportId, revision: appealId, archivada: archived, erroresJavaScript: errors, peticiones: network }, null, 2));
    const video = page.video(); await admin.close();
    if (video) await video.saveAs(path.join(output, "moderacion.webm"));
    await author.close(); await viewer.close(); await anonymous.close(); await browser.close();
}
