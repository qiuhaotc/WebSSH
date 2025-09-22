// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
// Use stale-while-revalidate for faster loads and background update
self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;
    event.respondWith(staleWhileRevalidate(event));
});

// Allow client to send {type:'SKIP_WAITING'} to activate new SW immediately
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function staleWhileRevalidate(event) {
    const request = event.request.mode === 'navigate' ? new Request('index.html') : event.request;
    const cache = await caches.open(cacheName);
    const cached = await cache.match(request);
    const fetchPromise = fetch(event.request)
        .then(async response => {
            try {
                if (response.ok && shouldCache(request)) {
                    await cache.put(request, response.clone());
                    // Notify clients that an update might be available
                    broadcastVersion();
                }
            } catch { }
            return response;
        })
        .catch(() => cached); // if offline use cached
    return cached || fetchPromise;
}

function shouldCache(request) {
    const url = typeof request === 'string' ? request : request.url;
    return offlineAssetsInclude.some(p => p.test(url)) && !offlineAssetsExclude.some(p => p.test(url));
}

async function broadcastVersion() {
    const allClients = await self.clients.matchAll({ includeUncontrolled: true });
    for (const client of allClients) {
        client.postMessage({ type: 'SW_VERSION', version: self.assetsManifest.version });
    }
}
