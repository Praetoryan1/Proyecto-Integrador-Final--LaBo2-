// Usa únicamente las cuentas demo. Deja Bruno activo y conserva el historial de la prueba.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
const base = "https://localhost:7280", output = path.resolve("docs/evidencias/incremento-07"), videoDir = path.resolve("tmp/video-cuentas");
await mkdir(output, { recursive: true }); await mkdir(videoDir, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
const options = { ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } };
const admin = await browser.newContext({ ...options, recordVideo: { dir: videoDir, size: options.viewport } });
const owner = await browser.newContext(options), anonymous = await browser.newContext(options);
const page = await admin.newPage(), ownerPage = await owner.newPage();
const errors = [], network = [];
for (const p of [page, ownerPage]) {
    p.setDefaultTimeout(20_000);
    p.on("pageerror", e => errors.push(e.message));
    p.on("response", r => { const u = new URL(r.url()); if (u.pathname.startsWith("/administracion/cuentas")) network.push({ metodo: r.request().method(), ruta: u.pathname, estado: r.status() }); });
}
async function login(p, email) {
    await p.goto(base + "/cuenta/ingresar");
    await p.getByLabel("Correo de acceso").fill(email);
    await p.getByLabel("Contraseña", { exact: true }).fill("Trazo.Demo2026!");
    await p.getByRole("button", { name: "Ingresar →" }).click(); await p.waitForURL(base + "/panel");
}
async function submit(p, name) { await Promise.all([p.waitForNavigation(), p.getByRole("button", { name, exact: true }).click()]); }
const capture = name => page.screenshot({ path: path.join(output, name + ".png"), fullPage: true });
let detailUrl, suspended = false, completed = false;
try {
    await login(ownerPage, "bruno@trazo.test");
    await login(page, "admin@trazo.test");
    await page.getByRole("link", { name: "Administrar cuentas ↗" }).click();
    await capture("cuentas");
    await page.getByLabel("Buscar por nombre de usuario").fill("bruno"); await submit(page, "Buscar");
    await page.getByRole("link", { name: "@bruno", exact: true }).click(); detailUrl = page.url();
    assert.equal(await page.getByRole("button", { name: "Confirmar suspensión", exact: true }).count(), 1);
    await page.getByLabel("Motivo de la decisión").fill("Demostración académica: suspensión temporal para comprobar la revocación de sesiones y la privacidad del contenido.");
    await capture("confirmar-suspension");
    const tokenResponse = await anonymous.request.post(base + "/api/v1/auth/token", { data: { email: "bruno@trazo.test", password: "Trazo.Demo2026!" } });
    assert.equal(tokenResponse.status(), 200); const { accessToken } = await tokenResponse.json();
    await submit(page, "Confirmar suspensión"); suspended = true;
    await capture("cuenta-suspendida");
    assert.equal((await anonymous.request.get(base + "/p/bruno")).status(), 404);
    assert.equal((await anonymous.request.get(base + "/api/v1/publicaciones", { headers: { Authorization: "Bearer " + accessToken } })).status(), 401);
    await ownerPage.goto(base + "/panel"); assert.ok(ownerPage.url().includes("/cuenta/ingresar"));
    await page.getByLabel("Motivo de la decisión").fill("Demostración finalizada: se reactiva la cuenta. El creador debe volver a iniciar sesión y las credenciales anteriores quedan revocadas.");
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false); await capture("cuenta-movil");
    await page.setViewportSize(options.viewport);
    await submit(page, "Confirmar reactivación"); suspended = false;
    await capture("historial");
    assert.equal((await anonymous.request.get(base + "/p/bruno")).status(), 200);
    assert.equal((await anonymous.request.get(base + "/api/v1/publicaciones", { headers: { Authorization: "Bearer " + accessToken } })).status(), 401);
    await login(ownerPage, "bruno@trazo.test");
    await page.goto(base + "/administracion/cuentas?q=admin"); await page.getByRole("link", { name: "@admin", exact: true }).click();
    assert.equal(await page.getByRole("button", { name: "Confirmar suspensión", exact: true }).count(), 0); await capture("administrador-protegido");
    assert.deepEqual(errors, []); completed = true;
    console.log("Cuentas verificadas: suspensión, reactivación, revocación de cookie/JWT, historial y móvil.");
} finally {
    // Si el recorrido se interrumpe después de suspender, restablecer solamente esta cuenta demo.
    if (suspended && detailUrl) {
        await page.goto(detailUrl);
        if (await page.getByRole("button", { name: "Confirmar reactivación", exact: true }).count()) {
            await page.getByLabel("Motivo de la decisión").fill("Restauración de la cuenta demo tras interrumpirse el recorrido automático de evidencia.");
            await submit(page, "Confirmar reactivación"); suspended = false;
        }
    }
    await writeFile(path.join(output, "recorrido.json"), JSON.stringify({ fechaUtc: new Date().toISOString(), completado: completed,
        cuentaDemo: "bruno", permaneceSuspendida: suspended, erroresJavaScript: errors, peticiones: network }, null, 2));
    const video = page.video(); await admin.close(); if (video) await video.saveAs(path.join(output, "cuentas.webm"));
    await owner.close(); await anonymous.close(); await browser.close();
}
