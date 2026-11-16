// Local audit bridge for a human-verified CUA publication. No browser or network access.
// Usage: node manual-receipt.ts submitting POST_ID - PRE_SUBMIT_SCREENSHOT
// Keep that process running; after CUA verifies the remote post, in another process:
// node manual-receipt.ts published POST_ID FULL_REMOTE_URL VERIFIED_SCREENSHOT
// For an uncertain final click: node manual-receipt.ts unknown POST_ID - EVIDENCE
import { readFile, stat, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const studioRoot = 'file:///C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/src/';
const [stage, postId, remoteUrl, evidenceArg, ...extra] = process.argv.slice(2);
if (!['submitting', 'published', 'unknown'].includes(stage ?? '') ||
    !/^post_[a-f0-9-]+$/.test(postId ?? '') || !remoteUrl || !evidenceArg || extra.length) {
  throw new Error('Usage: node manual-receipt.ts submitting|published|unknown POST_ID URL_OR_DASH ABSOLUTE_EVIDENCE_PATH');
}
if (!path.isAbsolute(evidenceArg)) throw new Error('Evidence path must be absolute');
const evidence = path.resolve(evidenceArg);
const evidenceInfo = await stat(evidence);
if (!evidenceInfo.isFile() || evidenceInfo.size === 0) throw new Error('Evidence must be a nonempty existing file');
const [{ loadConfig }, { StudioDb }, { StudioService }, util] = await Promise.all([
  import(`${studioRoot}config.ts`), import(`${studioRoot}db.ts`),
  import(`${studioRoot}service.ts`), import(`${studioRoot}util.ts`),
]);
const config = loadConfig();
const db = new StudioDb(config.databasePath);
const disabled = async () => { throw new Error('Browser/network actions are disabled in this receipt helper'); };
const service = new StudioService(db, {
  connect: disabled, inspect: disabled, refresh: disabled,
  publish: disabled, reconcile: disabled, close: async () => {},
}, config);
const evidenceSha256 = await util.fileHash(evidence);
const planFile = path.join(here, 'post-plan.json');
const plan = JSON.parse(await readFile(planFile, 'utf8'));
const timestamp = util.now();
const receiptDir = path.join(here, 'receipts');

try {
  const initial = service.post(postId);
  if (stage === 'submitting' && initial.status !== 'approved') throw new Error(`Expected approved, found ${initial.status}; do not submit again`);
  if (stage !== 'submitting' && initial.status !== 'submitting') throw new Error(`Expected submitting, found ${initial.status}; reconcile before changing a receipt`);
  if (initial.scheduledAt || initial.visibility !== 'public') throw new Error('This helper only supports approved immediate public posts');
  const { post, clip } = await service.verifyApproval(postId);
  const expected = plan.plans.find((entry: any) => entry.platform === post.platform);
  if (!expected || util.normalizeHandle(expected.account) !== util.normalizeHandle(post.accountHandle) ||
      expected.title !== post.title || expected.visibility !== post.visibility || expected.timezone !== post.timezone ||
      path.resolve(expected.video).toLowerCase() !== path.resolve(clip.filePath).toLowerCase() ||
      await readFile(expected.captionFile, 'utf8') !== post.caption) {
    throw new Error('Post does not match this trailer’s local account/video/title/caption/visibility plan');
  }
  const target = stage === 'published' ? util.publicationUrl(remoteUrl) : null;
  if (stage !== 'published' && remoteUrl !== '-') throw new Error('Use - for URL until a publication is verified');
  if (target && target.platform !== post.platform) throw new Error('Remote URL platform does not match the approved post');
  if (target?.platform === 'tiktok' && util.normalizeHandle(new URL(target.url).pathname.split('/')[1]) !== util.normalizeHandle(post.accountHandle)) {
    throw new Error('TikTok URL account does not match the approved account');
  }
  // Instagram/YouTube URLs do not encode the account. The operator must verify it in CUA.
  if (target && db.publications().some((other: any) => other.id !== postId && other.platform === target.platform && other.remoteId === target.remoteId)) {
    throw new Error('Remote publication is already tracked by another post; no change made');
  }
  await mkdir(receiptDir, { recursive: true });
  const receiptPath = path.join(receiptDir, `${postId}-${stage}-${timestamp.replace(/[:.]/g, '-')}.json`);
  const detail = {
    stage, publicationId: postId, platform: post.platform, accountHandle: post.accountHandle,
    approvedHash: post.approvedHash, clipId: clip.id, videoSha256: clip.sha256,
    url: target?.url ?? null, remoteId: target?.remoteId ?? null,
    evidence, evidenceSha256, at: timestamp,
    verification: stage === 'published'
      ? 'Operator verified exact remote URL, account, video and caption in CUA before recording this receipt'
      : stage === 'submitting' ? 'CUA final submit is prepared; no retry is permitted after an uncertain click'
      : 'Final submit outcome is uncertain; inspect/reconcile before any new submission',
  };
  await writeFile(receiptPath, `${JSON.stringify(detail, null, 2)}\n`, { flag: 'wx' });
  db.transaction(() => {
    const current = service.post(postId);
    if (current.approvedHash !== post.approvedHash || current.status !== initial.status) throw new Error('Publication changed during receipt preparation; no DB change made');
    if (stage === 'submitting') {
      if (!db.claimPublication(postId, ['approved'], post.approvedHash) ||
          !db.markPublicationSubmitting(postId, post.approvedHash)) throw new Error('Another process claimed or changed this publication');
    } else {
      if (!db.updatePublicationIfStatus(postId, ['submitting'], {
        status: stage,
        ...(target ? { remoteId: target.remoteId, url: target.url } : {}),
        message: `${detail.verification}; evidence ${receiptPath}`,
        updatedAt: timestamp,
      })) throw new Error('Publication is no longer submitting');
    }
    db.attempt(postId, stage, JSON.stringify({ receiptPath, ...detail }));
  });
  process.stdout.write(`${JSON.stringify({ status: stage, id: postId, url: target?.url ?? null, receiptPath, ...(stage === 'submitting' ? { ready: true, ownerPid: process.pid } : {}) })}\n`);
  if (stage === 'submitting') {
    // An active claim owner keeps the scheduler from treating a CUA upload as interrupted.
    // A separate published/unknown invocation records the terminal result and releases it.
    await new Promise<void>((resolve) => {
      const poll = setInterval(() => {
        const current = db.publication(postId);
        if (!current || current.status !== 'submitting') {
          clearInterval(poll);
          process.stdout.write(`${JSON.stringify({ ownerReleased: true, id: postId, status: current?.status ?? 'missing' })}\n`);
          resolve();
        }
      }, 1000);
    });
  }
} finally {
  db.close();
}
