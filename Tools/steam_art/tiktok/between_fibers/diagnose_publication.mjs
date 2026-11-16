// Read-only diagnostics through Social Studio's existing account-aware browser gateway.
import { loadConfig } from 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/config.ts';
import { BrowserGateway } from 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/browser.ts';
const [platform, targetUrl, tab] = process.argv.slice(2);
if (!['youtube', 'instagram', 'tiktok'].includes(platform)) throw new Error('Choose a known platform');
const config = loadConfig({ home: 'C:/Users/denis/AppData/Local/GameSocialStudio' });
const browser = new BrowserGateway(config);
try {
  const result = await browser.diagnose(platform, targetUrl || undefined, false, tab || undefined);
  process.stdout.write(JSON.stringify(result, null, 2) + '\n');
} finally {
  await browser.close();
}
