// PreToolUse koruması (CLAUDE.md kuralları):
//  1) Gizli değerler: 3K_API/appsettings.Secrets.json asla okunmaz, yazılmaz, kopyalanmaz.
//  2) Gerçek veritabanı: 3K_Db üzerinde yıkıcı/yazan SQL çalıştırılmaz; yalnız izole test sunucusu (55439/55440) serbest.
// Engellenen çağrıda stderr'e neden yazılır ve 2 koduyla çıkılır (Claude Code aracı çalıştırmaz).
const GIZLI = /appsettings\.Secrets\.json/i;

function engelle(neden) {
  process.stderr.write(`[3K koruma] ${neden}\n`);
  process.exit(2);
}

let girdi = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (parca) => (girdi += parca));
process.stdin.on('end', () => {
  let olay;
  try { olay = JSON.parse(girdi || '{}'); } catch { process.exit(0); }
  const arac = olay.tool_name ?? '';
  const giris = olay.tool_input ?? {};

  if (['Read', 'Edit', 'Write', 'NotebookEdit'].includes(arac)) {
    const yol = String(giris.file_path ?? giris.notebook_path ?? '');
    if (GIZLI.test(yol))
      engelle('appsettings.Secrets.json gizli değerler içerir; okunamaz/değiştirilemez. Gerekirse kullanıcıdan ilgili ayarın adını isteyin.');
    process.exit(0);
  }

  if (arac === 'Bash' || arac === 'PowerShell') {
    const komut = String(giris.command ?? '');

    if (GIZLI.test(komut)) {
      // Dosyayı açan/okuyan/kopyalayan komutlar; dosya adını yalnız arama deseni olarak geçen grep serbesttir.
      const okuyucu = /\b(cat|type|Get-Content|gc|more|less|head|tail|xxd|od|strings|base64|Copy-Item|cp|copy|move|mv|jq|sed|awk|Select-String|sls|notepad|code|certutil)\b[^|;&\n]*appsettings\.Secrets\.json/i;
      const aramaDosyasi = /\b(grep|rg|findstr)\b[^\n]*\s["']?[^\s"']*appsettings\.Secrets\.json["']?\s*($|[|;&>)])/i;
      const betikOkuma = /(readFile(Sync)?|ReadAllText|ReadAllLines|open)\s*\([^)]*appsettings\.Secrets\.json/i;
      if (okuyucu.test(komut) || aramaDosyasi.test(komut) || betikOkuma.test(komut))
        engelle('appsettings.Secrets.json içeriği okunamaz, yazdırılamaz veya kopyalanamaz.');
    }

    const yazanSql = /\b(DROP|TRUNCATE|DELETE\s+FROM|UPDATE\s+\S+\s+SET|INSERT\s+INTO|ALTER\s+(TABLE|DATABASE|ROLE|SEQUENCE)|CREATE\s+(TABLE|INDEX|ROLE|DATABASE|EXTENSION)|GRANT|REVOKE)\b/i;
    const testSunucusu = /\b5543[90]\b/;
    const veritabaniAraci = /\bpsql\b|\bq\.ps1\b|\bNpgsqlConnection\b/i;
    if (veritabaniAraci.test(komut) && !testSunucusu.test(komut)) {
      if (yazanSql.test(komut))
        engelle('Gerçek veritabanında (3K_Db) yalnız SELECT çalıştırılabilir. Yazan/yıkıcı SQL yalnız izole test sunucusunda (port 55439) denenir; gerçek DB betiğini kullanıcı çalıştırır.');
      if (/\bpsql\b[^\n]*\s-f\s/i.test(komut))
        engelle('SQL betik dosyaları gerçek veritabanında çalıştırılmaz; betiği kullanıcı pgAdmin/psql ile kendisi çalıştırır.');
    }
  }
  process.exit(0);
});
