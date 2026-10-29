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
        const cache = await caches.open(cacheName);

        // Para navegaciones (modo PWA o recargas de ruta como /operaciones/registro-movimientos-supervisor)
        if (event.request.mode === 'navigate') {
            try {
                // Intentar cargar de la red primero
                const networkResponse = await fetch(event.request);
                if (networkResponse && (networkResponse.ok || networkResponse.type === 'opaqueredirect')) {
                    return networkResponse;
                }
            } catch (err) {
                // Silenciar error de red para no disparar uncaught rejection en consola
            }

            // Fallback a index.html desde caché para SPA Blazor WASM
            const indexResponse = await cache.match('index.html');
            if (indexResponse) {
                return indexResponse;
            }
        } else {
            // Para activos estáticos (dll, wasm, css, js, imágenes)
            const relativeUrl = event.request.url.substring(self.location.origin.length + 1);
            const isAsset = self.assetsManifest.assets.some(asset => asset.url === relativeUrl);

            if (isAsset) {
                cachedResponse = await cache.match(event.request);
                if (cachedResponse) {
                    return cachedResponse;
                }
            }
        }
    }

    try {
        return await fetch(event.request);
    } catch (err) {
        if (cachedResponse) {
            return cachedResponse;
        }
        return new Response('', { status: 408, statusText: 'Network request failed' });
    }
}
