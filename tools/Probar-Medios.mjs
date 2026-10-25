// Prueba local con Edge y cuentas demo; crea y elimina su propia publicación.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import { execFileSync } from "node:child_process";
import path from "node:path";

const base = "https://localhost:7280";
const output = path.resolve("docs/evidencias/incremento-03");
const tmp = path.resolve("tmp/recorrido-medios");
await mkdir(output, { recursive: true });
await mkdir(tmp, { recursive: true });
const ffmpeg = process.env.TRAZO_FFMPEG || path.resolve("tools/ffmpeg/bin/ffmpeg.exe");
for (const extension of ["png", "gif", "mp4"]) {
    const args = ["-v", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=640x360:rate=10", "-threads", "1"];
    if (extension === "png") args.push("-frames:v", "1", "-update", "1");
    else args.push("-t", "1");
    if (extension === "mp4") args.push("-c:v", "libx264", "-pix_fmt", "yuv420p");
    args.push(path.join(tmp, "muestra." + extension));
    execFileSync(ffmpeg, args, { windowsHide: true });
}
const browser = await chromium.launch({ channel: "msedge", headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 }, recordVideo: { dir: tmp, size: { width: 1440, height: 1000 } } });
const page = await context.newPage();
page.setDefaultTimeout(20_000);
const errors = [], network = [];
let documents = 0, id, csrf, externalRequests = 0;
page.on("pageerror", e => errors.push(e.message));
page.on("console", msg => { if (msg.type() === "error") errors.push(msg.text()); });
page.on("request", req => { if (req.resourceType() === "document") documents++; });
page.on("response", response => {
    if (response.url().includes("/panel-json/")) network.push({ method: response.request().method(), path: new URL(response.url()).pathname, status: response.status() });
});
page.on("dialog", dialog => dialog.accept());
const ready = async () => {
    await page.locator('.recursos-editor[aria-busy="false"]').waitFor();
    await page.waitForFunction(() => !document.querySelector(".post-editor fieldset")?.disabled);
};
const mutation = async (action, suffix, method = "POST") => {
    const pending = page.waitForResponse(r => r.url().endsWith(suffix) && r.request().method() === method);
    await action();
    const response = await pending;
    assert.ok(response.ok(), await response.text());
    const data = await response.json();
    await ready();
    return data;
};
try {
    await page.goto(base + "/cuenta/ingresar");
    await page.getByLabel("Correo de acceso").fill("ana@trazo.test");
    await page.getByLabel("Contraseña", { exact: true }).fill("Trazo.Demo2026!");
    await page.getByRole("button", { name: "Ingresar →" }).click();
    await page.goto(base + "/panel/publicaciones");
    await page.locator(".post-row").first().waitFor();
    const initialDocuments = documents;
    await page.getByRole("button", { name: "Nueva publicación +" }).click();
    await page.getByLabel("Título", { exact: true }).fill("Laboratorio de color · muestra multimedia");
    await page.getByLabel("Texto", { exact: true }).fill("Patrones de prueba generados localmente para comprobar imágenes, movimiento y reproducción. Una obra reúne sus recursos, descripciones y créditos en una sola publicación.");
    await page.getByLabel("Tipo", { exact: true }).selectOption("0");
    const created = page.waitForResponse(r => r.url().endsWith("/panel-json/publicaciones") && r.request().method() === "POST");
    await page.getByRole("button", { name: "Guardar borrador →" }).click();
    const response = await created;
    assert.equal(response.status(), 201);
    id = (await response.json()).id;
    csrf = response.request().headers()["x-csrf-token"];
    await ready();
    let post;
    for (const extension of ["png", "gif", "mp4"]) {
        await page.getByLabel("Archivo", { exact: true }).setInputFiles(path.join(tmp, "muestra." + extension));
        await page.getByLabel("Descripción accesible del recurso").fill(`Patrón de color de prueba · ${extension.toUpperCase()}`);
        await page.getByLabel("Créditos (opcional)").fill("Patrón testsrc2 generado localmente con FFmpeg");
        post = await mutation(() => page.getByRole("button", { name: "Agregar recurso +" }).click(), "/recursos/archivo");
    }
    await page.getByLabel("Agregar medio", { exact: true }).selectOption("youtube");
    await page.getByLabel("Enlace HTTPS de YouTube").fill("https://youtu.be/abcdefghijk");
    await page.getByLabel("Descripción accesible del recurso").fill("Ejemplo de reproductor externo (identificador ficticio)");
    post = await mutation(() => page.getByRole("button", { name: "Agregar recurso +" }).click(), "/recursos/youtube");
    assert.equal(post.recursos.length, 4);
    post = await mutation(() => page.getByRole("button", { name: "Subir recurso 3", exact: true }).click(), "/recursos/orden", "PUT");
    assert.equal(post.recursos[1].tipo, 2);
    await page.getByRole("button", { name: "Descripción", exact: true }).first().click();
    await page.getByLabel("Editar descripción del recurso").fill("Composición de color: imagen de referencia");
    post = await mutation(() => page.getByRole("button", { name: "Guardar descripción" }).click(), `/recursos/${post.recursos[0].id}`, "PUT");
    await page.locator(".recursos-editor").screenshot({ path: path.join(output, "editor-medios.png") });
    await page.getByLabel("Estado editorial").selectOption("1");
    post = await mutation(() => page.getByRole("button", { name: "Guardar publicación →" }).click(), `/publicaciones/${id}`, "PUT");
    assert.equal(documents, initialDocuments, "La edición multimedia no debe recargar la página.");
    const editorDocuments = documents - initialDocuments;
    await page.setViewportSize({ width: 390, height: 844 });
    await page.locator(".recursos-editor").screenshot({ path: path.join(output, "editor-movil.png") });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await page.setViewportSize({ width: 1440, height: 1000 });
    const visitor = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1280, height: 1000 } });
    const detail = await visitor.newPage();
    detail.setDefaultTimeout(20_000);
    detail.on("pageerror", e => errors.push(e.message));
    // No contacta a YouTube durante la prueba: solo comprueba creación del iframe tras el clic.
    await detail.route("https://www.youtube-nocookie.com/**", route => { externalRequests++; return route.fulfill({ contentType: "text/html", body: "<p>Reproductor externo simulado por la prueba.</p>" }); });
    await detail.goto(`${base}/publicaciones/${id}`);
    assert.equal(externalRequests, 0);
    assert.equal(await detail.locator("iframe").count(), 0);
    await detail.screenshot({ path: path.join(output, "obra-publica.png"), fullPage: true });
    const gif = detail.getByRole("button", { name: "Reproducir GIF" });
    await gif.click();
    assert.equal(await detail.getByRole("button", { name: "Detener GIF" }).getAttribute("aria-pressed"), "true");
    await detail.getByRole("button", { name: "Detener GIF" }).click();
    const video = post.recursos.find(r => r.tipo === 2);
    const range = await visitor.request.get(base + video.archivoUrl, { headers: { Range: "bytes=0-99" } });
    assert.equal(range.status(), 206);
    assert.equal((await range.body()).length, 100);
    await detail.locator("video").evaluate(async video => { await video.play(); });
    await detail.waitForFunction(() => document.querySelector("video").ended);
    await detail.getByRole("button", { name: "Cargar video de YouTube" }).click();
    await detail.locator("iframe").waitFor();
    await detail.waitForFunction(() => !!document.querySelector("iframe")?.contentWindow);
    assert.equal(await detail.locator("iframe").getAttribute("src"), "https://www.youtube-nocookie.com/embed/abcdefghijk");
    await detail.goto(base + "/?medio=Video");
    await detail.screenshot({ path: path.join(output, "explorar-medios.png"), fullPage: true });
    assert.ok(await detail.locator(`a[href='/publicaciones/${id}'] img`).count());
    await page.getByLabel("Visibilidad", { exact: true }).selectOption("2");
    post = await mutation(() => page.getByRole("button", { name: "Guardar publicación →" }).click(), `/publicaciones/${id}`, "PUT");
    assert.equal((await visitor.request.get(`${base}/publicaciones/${id}`)).status(), 404);
    assert.equal((await visitor.request.get(base + video.archivoUrl, { headers: { Range: "bytes=0-99" } })).status(), 404);
    await visitor.close();
    await page.getByLabel("Estado editorial").selectOption("0");
    post = await mutation(() => page.getByRole("button", { name: "Guardar borrador →" }).click(), `/publicaciones/${id}`, "PUT");
    post = await mutation(() => page.getByRole("button", { name: "Quitar", exact: true }).last().click(), `/recursos/${post.recursos.at(-1).id}`, "DELETE");
    assert.equal(post.recursos.length, 3);
    assert.deepEqual(errors, []);
    await writeFile(path.join(output, "red.json"), JSON.stringify({ fechaUtc: new Date().toISOString(), recargasEditor: editorDocuments, comprobaciones: ["PNG/GIF/MP4 normalizados", "YouTube solo tras clic; iframe simulado, reproducción externa no evaluada", "orden y descripción", "publicación y filtros", "MP4 Range 206 y reproducción completa en Edge", "privacidad Range 404", "móvil sin desborde", "baja de recurso"], erroresJavaScript: errors, peticiones: network }, null, 2));
    console.log("Recorrido multimedia aprobado: archivos, edición, privacidad, rangos y móvil.");
} finally {
    if (id && csrf) {
        const current = await context.request.get(`${base}/panel-json/publicaciones/${id}`);
        if (current.ok()) {
            const data = await current.json();
            const deleted = await context.request.delete(`${base}/panel-json/publicaciones/${id}`, { headers: { "X-CSRF-TOKEN": csrf }, data: { version: data.version } });
            assert.equal(deleted.status(), 204, "Debe limpiar su publicación temporal.");
        }
    }
    const video = page.video();
    await context.close();
    if (video) await video.saveAs(path.join(output, "medios-abm.webm"));
    await browser.close();
}
