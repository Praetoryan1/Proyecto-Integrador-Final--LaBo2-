// Compone el DER actual a partir de las dos vistas preliminares y la extensión de cuentas.
import { chromium } from "playwright";
import { readFile } from "node:fs/promises";
import path from "node:path";
const browser = await chromium.launch({ channel: "msedge", headless: true });
try {
    const page = await browser.newPage({ viewport: { width: 1160, height: 900 }, deviceScaleFactor: 1 });
    const image = async (file, mime) => `data:${mime};base64,${(await readFile(path.resolve("docs", file))).toString("base64")}`;
    const core = await image("der-core.png", "image/png"), social = await image("der-social.png", "image/png"), accounts = await image("der-cuentas.svg", "image/svg+xml");
    await page.setContent(`<html lang="es"><style>body{margin:0;padding:30px;background:white;color:#172639;font-family:Arial}h1{font-size:28px}p{font-size:17px}img{display:block;width:1100px;margin:25px 0}</style><h1>Trazo · Modelo de datos actual</h1><p>7 entidades principales, asociaciones e historial administrativo de cuentas. Actualizado: 25/10/2026.</p><img src="${core}"><img src="${social}"><img id="cuentas" src="${accounts}"></html>`);
    await page.locator("img").evaluateAll(images => Promise.all(images.map(i => i.decode())));
    await page.locator("#cuentas").screenshot({ path: "docs/der-cuentas.png" });
    await page.screenshot({ path: "docs/der.png", fullPage: true });
    console.log("DER actual y extensión de cuentas generados.");
} finally { await browser.close(); }
