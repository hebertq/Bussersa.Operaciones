// Service Worker de Producción - BUSSERSA Operaciones PWA
// Caching inteligente con manifiesto de activos de Blazor WebAssembly

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.woff2$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

async function onInstall(event) {
    console.info('Service Worker Busser Tablet: Instalando activos...');
    self.skipWaiting();
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service Worker Busser Tablet: Activado.');
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // En navegaciones, verificar si coincide con activos en caché
        const shouldServeFromCache = event.request.mode === 'navigate'
            ? false // Las navegaciones van a la red primero para asegurar datos actualizados
            : self.assetsManifest.assets.some(asset => asset.url === event.request.url.substring(self.location.origin.length + 1));

        if (shouldServeFromCache) {
            const cache = await caches.open(cacheName);
            cachedResponse = await cache.match(event.request);
        }
    }

    return cachedResponse || fetch(event.request);
}
