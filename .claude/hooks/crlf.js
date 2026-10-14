// PostToolUse: arka uç çalışma kopyası CRLF'dir (core.autocrlf=true). Write/Edit ile yazılan .cs/.sql/.md
// dosyasında tek başına LF kalmışsa satır sonları CRLF'e çevrilir. Yalnız bu deponun dosyalarına dokunur.
const fs = require('fs');
const path = require('path');

let girdi = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (parca) => (girdi += parca));
process.stdin.on('end', () => {
  try {
    const olay = JSON.parse(girdi || '{}');
    const dosya = olay.tool_input?.file_path ?? olay.tool_response?.filePath;
    const kok = process.env.CLAUDE_PROJECT_DIR;
    if (!dosya || !kok) return;
    const tam = path.resolve(dosya).toLowerCase();
    const depo = path.resolve(kok).toLowerCase() + path.sep;
    if (!tam.startsWith(depo)) return;
    if (!/\.(cs|sql|md)$/i.test(tam) || /[\\/](bin|obj|node_modules)[\\/]/i.test(tam)) return;
    const icerik = fs.readFileSync(dosya, 'utf8');
    if (!/(^|[^\r])\n/.test(icerik)) return;
    fs.writeFileSync(dosya, icerik.replace(/\r\n/g, '\n').replace(/\n/g, '\r\n'));
  } catch {
    // Satır sonu düzeltmesi yardımcıdır; başarısız olursa araç sonucunu bozmaz.
  }
});
