// Use the normal hash-checked publication workflow, with enough time for video processing.
import { loadConfig } from 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/config.ts';
import { BrowserGateway } from 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/browser.ts';
import { StudioDb } from 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/db.ts';
import { StudioService } from 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/service.ts';
const [postId] = process.argv.slice(2);
if (!postId?.startsWith('post_')) throw new Error('An existing approved post ID is required');
const config = loadConfig({ home: 'C:/Users/denis/AppData/Local/GameSocialStudio', browserTimeoutMs: 180000 });
const db = new StudioDb(config.databasePath);
const browser = new BrowserGateway(config);
const studio = new StudioService(db, browser, config);
try {
  const result = await studio.publish(postId);
  process.stdout.write(JSON.stringify(result, null, 2) + '\n');
} finally {
  await browser.close();
  db.close();
}
