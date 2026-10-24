// Recorrido local con cuentas demo. Requiere la web y MySQL encendidos.
import { chromium } from "playwright";
import assert from "node:assert/strict";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

const base = "https://localhost:7280";
const output = path.resolve("docs/evidencias/incremento-02");
const temporary = path.resolve("tmp/video-editor");
await mkdir(output, { recursive: true });
await mkdir(temporary, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
const context = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 1000 },
    recordVideo: { dir: temporary, size: { width: 1440, height: 1000 } },
});
const page = await context.newPage();
const failures = [];
const network = [];
let documents = 0;
let id;
let csrf;
const title = `Estudio de movimiento · prueba ${Date.now()}`;
page.on("pageerror", (e) => failures.push(e.message));
page.on("console", (msg) => {
    if (msg.type() === "error") failures.push(msg.text());
});
page.on("request", (request) => {
    if (request.resourceType() === "document") documents++;
});
page.on("response", (response) => {
    if (response.url().includes("/panel-json/"))
        network.push({
            method: response.request().method(),
            url:
                new URL(response.url()).pathname +
                new URL(response.url()).search,
            status: response.status(),
        });
});
page.on("dialog", (dialog) => dialog.accept());
const capture = async (name) => {
    await page.locator('.publicaciones-list[aria-busy="false"]').waitFor();
    await page.waitForFunction(
        () => !document.querySelector(".post-editor fieldset")?.disabled,
    );
    await page.screenshot({
        path: path.join(output, name + ".png"),
        fullPage: true,
    });
};
const pause = () => page.waitForTimeout(650); // Legibilidad de la grabación, no sincronización de peticiones.
try {
    await page.goto(base + "/cuenta/ingresar");
    await page.getByLabel("Correo de acceso").fill("ana@trazo.test");
    await page
        .getByLabel("Contraseña", { exact: true })
        .fill("Trazo.Demo2026!");
    await page.getByRole("button", { name: "Ingresar →" }).click();
    await page
        .getByRole("link", { name: "Administrar mis publicaciones" })
        .click();
    await page.getByRole("heading", { name: "Mis publicaciones." }).waitFor();
    await page.locator(".post-row").first().waitFor();
    const initialDocuments = documents;
    await capture("listado");
    await page.getByRole("button", { name: "Nueva publicación +" }).click();
    await page.getByLabel("Título", { exact: true }).fill(title);
    await page
        .getByLabel("Texto", { exact: true })
        .fill(
            "Un estudio sobre ritmo y composición. Primero aparece como borrador; después lo compartimos como actualización.",
        );
    await page.getByRole("button", { name: "Buscar etiquetas +" }).click();
    await page.getByLabel("Buscar etiqueta", { exact: true }).fill("Anima");
    await page.getByRole("button", { name: "#Animación Agregar +" }).click();
    await capture("selector-etiquetas");
    await page.getByRole("button", { name: "Cerrar búsqueda" }).click();
    await pause();
    const createdResponse = page.waitForResponse(
        (r) =>
            r.url().endsWith("/panel-json/publicaciones") &&
            r.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Guardar borrador →" }).click();
    const created = await createdResponse;
    assert.equal(created.status(), 201);
    const createdData = await created.json();
    id = createdData.id;
    assert.equal(createdData.etiquetas.length, 1);
    await page
        .getByRole("status")
        .filter({ hasText: "Borrador guardado" })
        .waitFor();
    csrf = created.request().headers()["x-csrf-token"];
    await page.getByLabel("Estado editorial").selectOption("1");
    await page
        .getByLabel("Texto", { exact: true })
        .fill(
            "Versión publicada: exploramos ritmo, contraste y composición. Texto actualizado sin recargar la página.",
        );
    const updateResponse = page.waitForResponse(
        (r) =>
            r.url().endsWith(`/panel-json/publicaciones/${id}`) &&
            r.request().method() === "PUT",
    );
    await page.getByRole("button", { name: "Guardar publicación →" }).click();
    assert.equal((await updateResponse).status(), 200);
    await page
        .getByRole("status")
        .filter({ hasText: "Publicación guardada." })
        .waitFor();
    await capture("editor");
    await page.getByRole("button", { name: "Cerrar editor" }).click();
    // La publicación agregada lleva a Ana de 12 a 13 entradas: pedir la segunda página.
    const secondResponse = page.waitForResponse(
        (r) =>
            r.url().includes("/panel-json/publicaciones?") &&
            r.url().includes("pagina=2"),
    );
    await page
        .getByRole("button", { name: "Siguiente →", exact: true })
        .click();
    const second = await (await secondResponse).json();
    assert.ok(second.items.length <= 12);
    assert.equal(second.numero, 2);
    await capture("paginacion");
    await writeFile(
        path.join(output, "paginacion.json"),
        JSON.stringify(
            {
                numero: second.numero,
                tamano: second.tamano,
                total: second.total,
                registrosRecibidos: second.items.length,
                paginas: second.paginas,
            },
            null,
            2,
        ) + "\n",
    );
    await page.getByLabel("Buscar por título").fill(title);
    const filteredResponse = page.waitForResponse(
        (r) =>
            r.url().includes("/panel-json/publicaciones?") &&
            r.url().includes("q=Estudio"),
    );
    await page.getByRole("button", { name: "Buscar", exact: true }).click();
    assert.equal((await (await filteredResponse).json()).total, 1);
    await page
        .getByRole("button", { name: `Editar ${title}`, exact: true })
        .click();
    await page.getByRole("heading", { name: "Editar publicación" }).waitFor();
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(
        await page.evaluate(
            () => document.documentElement.scrollWidth > innerWidth,
        ),
        false,
    );
    await capture("editor-mobile");
    await pause();
    await page.getByRole("button", { name: "Cerrar editor" }).click();
    await page.setViewportSize({ width: 1440, height: 1000 });
    const deleteResponse = page.waitForResponse(
        (r) =>
            r.url().endsWith(`/panel-json/publicaciones/${id}`) &&
            r.request().method() === "DELETE",
    );
    await page
        .getByRole("button", { name: `Eliminar ${title}`, exact: true })
        .click();
    assert.equal((await deleteResponse).status(), 204);
    await page.getByRole("heading", { name: "Sin coincidencias" }).waitFor();
    await capture("baja");
    await pause();
    assert.equal(
        documents,
        initialDocuments,
        "El ABM no debe navegar ni recargar el documento.",
    );
    assert.deepEqual(
        failures,
        [],
        "No debe haber errores JavaScript, CSP ni respuestas fallidas en consola.",
    );
    await writeFile(
        path.join(output, "red.json"),
        JSON.stringify(
            {
                documentRequestsDuringCrud: documents - initialDocuments,
                requests: network,
            },
            null,
            2,
        ) + "\n",
    );
    console.log(
        "ABM, selector, paginación y móvil verificados. Cero recargas del documento durante el ABM.",
    );
} finally {
    if (id && csrf) {
        const response = await context.request.get(
            `${base}/panel-json/publicaciones/${id}`,
        );
        if (response.ok()) {
            const post = await response.json();
            await context.request.delete(
                `${base}/panel-json/publicaciones/${id}`,
                {
                    headers: { "X-CSRF-TOKEN": csrf },
                    data: { version: post.version },
                },
            );
        }
    }
    const video = page.video();
    await context.close();
    await video.saveAs(path.join(output, "editor-abm.webm"));
    await browser.close();
}
