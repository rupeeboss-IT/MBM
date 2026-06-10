import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const eventDir = path.join(__dirname, '..', 'public', 'event');

if (!fs.existsSync(eventDir)) {
  fs.mkdirSync(eventDir, { recursive: true });
}

const images = fs
  .readdirSync(eventDir)
  .filter((file) => /\.(jpe?g|png|webp|gif)$/i.test(file))
  .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }));

const manifest = { images, generatedAt: new Date().toISOString() };
fs.writeFileSync(path.join(eventDir, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
console.log(`Generated event manifest with ${images.length} image(s): ${images.join(', ') || '(none)'}`);
