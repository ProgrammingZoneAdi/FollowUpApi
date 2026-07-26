import { copyFile, mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const source = resolve(root, "node_modules/jquery/dist/jquery.min.js");
const destination = resolve(root, "www/vendor/jquery/jquery.min.js");

await mkdir(dirname(destination), { recursive: true });
await copyFile(source, destination);
console.log("Prepared www/vendor/jquery/jquery.min.js");
