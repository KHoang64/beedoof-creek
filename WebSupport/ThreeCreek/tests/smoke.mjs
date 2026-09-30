import { chromium } from "playwright-core";
import { mkdir } from "node:fs/promises";
import { fileURLToPath } from "node:url";
const out = fileURLToPath(
  new URL("../../../Docs/QA/ThreeCreek/", import.meta.url),
);
await mkdir(out, { recursive: true });
const browser = await chromium.launch({
  executablePath: "C:/Program Files/Google/Chrome/Application/chrome.exe",
  headless: true,
  args: [
    "--use-gl=angle",
    "--use-angle=swiftshader",
    "--enable-unsafe-swiftshader",
  ],
});
for (const [name, viewport, mobile] of [
  ["desktop", { width: 1280, height: 720 }, false],
  ["portrait", { width: 390, height: 844 }, true],
  ["landscape", { width: 844, height: 390 }, true],
]) {
  const page = await browser.newPage({
    viewport,
    isMobile: mobile,
    hasTouch: mobile,
    deviceScaleFactor: mobile ? 2 : 1,
  });
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => {
    if (message.type() === "error") errors.push(message.text());
  });
  page.on("response", (response) => {
    if (response.status() === 404) errors.push("404 " + response.url());
  });
  await page.goto(process.env.CREEK_URL || "http://127.0.0.1:5173/", {
    waitUntil: "domcontentloaded",
  });
  await page.waitForSelector("#menu:not([hidden])", { timeout: 30000 });
  await page.screenshot({ path: out + name + "-menu.png" });
  await page.locator("#explore").click();
  await page.waitForTimeout(1800);
  await page.screenshot({ path: out + name + "-explore.png" });
  await page.locator("#rain-toggle").click();
  if (
    (await page.locator("#rain-toggle").getAttribute("aria-pressed")) !==
    "false"
  )
    errors.push("Rain toggle did not update");
  await page.locator("#volume").fill("0.1");
  if ((await page.locator("#volume-value").textContent()) !== "10%")
    errors.push("Volume did not update");
  await page.locator("#menu-button").click();
  await page.locator("#watch").click();
  await page.waitForTimeout(350);
  if (await page.locator("#film-controls").isHidden())
    errors.push("Film controls not shown");
  console.log(
    JSON.stringify({
      name,
      viewport,
      errors,
      canvas: await page.locator("#scene canvas").count(),
    }),
  );
  await page.close();
}
await browser.close();
