// Geometría original de demostración. Requiere Edge/Playwright y FFmpeg; no se ejecuta al instalar.
import { chromium } from "playwright";
import { mkdir, writeFile, readFile, readdir } from "node:fs/promises";
import { createHash } from "node:crypto";
import { execFileSync } from "node:child_process";
import path from "node:path";
const root = path.resolve("src/Trazo.Web/DemoAssets"), frames = path.resolve("tmp/frames-demo");
await mkdir(path.join(root, "avatars"), { recursive: true }); await mkdir(path.join(root, "medios"), { recursive: true }); await mkdir(frames, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: true });
try {
    const page = await browser.newPage(); await page.setContent('<canvas width="960" height="640"></canvas>');
    async function draw(index, frame, size = [960, 640]) {
        return Buffer.from(await page.evaluate(({ index, frame, size }) => {
            const c = document.querySelector("canvas"); [c.width, c.height] = size; const g = c.getContext("2d");
            const palettes = [["#c4b5fd", "#33264b", "#c4f566"], ["#f2ac87", "#73394f", "#fff1cf"], ["#7dd3fc", "#174455", "#f2da7e"]];
            const [bg, ink, accent] = palettes[index % 3]; g.fillStyle = bg; g.fillRect(0, 0, c.width, c.height);
            g.save(); g.translate(c.width / 2, c.height / 2); g.rotate((frame / 12) * Math.PI * 2);
            g.strokeStyle = ink; g.lineWidth = c.width / 16; g.beginPath(); g.arc(0, 0, c.height * .29, .25, Math.PI * 1.75); g.stroke();
            g.fillStyle = accent; g.fillRect(-c.width * .34, -c.height * .07, c.width * .68, c.height * .14);
            g.fillStyle = ink; g.beginPath(); g.arc(c.width * .22, -c.height * .25, c.height * .09, 0, Math.PI * 2); g.fill(); g.restore();
            return c.toDataURL("image/png").split(",")[1];
        }, { index, frame, size }), "base64");
    }
    for (let i = 1; i <= 3; i++) await writeFile(path.join(root, "avatars", "a".repeat(31) + i + ".png"), await draw(i - 1, 0, [512, 512]));
    for (const i of [1, 2, 5, 6, 7]) {
        await writeFile(path.join(root, "medios", "b".repeat(31) + i + ".png"), await draw(i - 1, 0));
        await writeFile(path.join(root, "medios", "b".repeat(31) + i + ".preview.png"), await draw(i - 1, 0, [600, 400]));
    }
    for (let i = 0; i < 12; i++) await writeFile(path.join(frames, String(i).padStart(2, "0") + ".png"), await draw(2, i));
    const ffmpeg = process.env.TRAZO_FFMPEG || path.resolve("tools/ffmpeg/bin/ffmpeg.exe");
    for (const [key, extension] of [[3, "gif"], [4, "mp4"]]) {
        const args = ["-v", "error", "-y", "-framerate", "12", "-i", path.join(frames, "%02d.png"), "-threads", "1"];
        if (extension === "mp4") args.push("-c:v", "libx264", "-pix_fmt", "yuv420p", "-movflags", "+faststart");
        else args.push("-loop", "0");
        execFileSync(ffmpeg, [...args, path.join(root, "medios", "b".repeat(31) + key + "." + extension)], { windowsHide: true });
        await writeFile(path.join(root, "medios", "b".repeat(31) + key + ".preview.png"), await draw(2, 0, [600, 400]));
    }
    const manifest = [];
    for (const folder of ["avatars", "medios"]) for (const name of (await readdir(path.join(root, folder))).sort()) {
        const bytes = await readFile(path.join(root, folder, name));
        manifest.push({ archivo: folder + "/" + name, bytes: bytes.length, sha256: createHash("sha256").update(bytes).digest("hex") });
    }
    await writeFile(path.join(root, "manifest.json"), JSON.stringify(manifest, null, 2));
    console.log(`${manifest.length} archivos ficticios generados con manifiesto SHA-256.`);
} finally { await browser.close(); }
