// Rasterize the supplied SVG for Windows executables; keep SVGs as the source.
const fs = require('fs');
const path = require('path');
const sharp = require(process.env.AWL_SHARP_PATH || 'sharp');
const assets = path.resolve(__dirname, '../assets');
(async () => {
  const source = path.join(assets, 'awl-app-icon.svg');
  const sizes = [16, 24, 32, 48, 64, 128, 256];
  const images = await Promise.all(sizes.map(size => sharp(source, {density: 384}).resize(size, size).png().toBuffer()));
  const header = Buffer.alloc(6 + 16 * sizes.length);
  header.writeUInt16LE(1, 2); header.writeUInt16LE(sizes.length, 4);
  let offset = header.length;
  images.forEach((image, i) => {
    const entry = 6 + i * 16;
    header[entry] = header[entry + 1] = sizes[i] === 256 ? 0 : sizes[i];
    header.writeUInt16LE(1, entry + 4); header.writeUInt16LE(32, entry + 6);
    header.writeUInt32LE(image.length, entry + 8); header.writeUInt32LE(offset, entry + 12);
    offset += image.length;
  });
  const ico = Buffer.concat([header, ...images]);
  for (const name of ['Awl.ico', 'Awl.DevMode.ico', 'Awl.WidgetBuilder.ico']) fs.writeFileSync(path.join(assets, name), ico);
  await sharp(source, {density: 384}).resize(256, 256).png().toFile(path.join(assets, 'awl-app-icon.png'));
  await sharp(path.join(assets, 'awl-mark.svg'), {density: 384}).resize(256, 256).png().toFile(path.join(assets, 'awl-mark.png'));
})().catch(error => { console.error(error); process.exitCode = 1; });
