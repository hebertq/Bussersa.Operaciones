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
    try {
        const assetsRequests = self.assetsManifest.assets
            .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
            .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
            .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
        const cache = await caches.open(cacheName);
        await cache.addAll(assetsRequests);
    } catch (err) {
        console.warn('Aviso: pre-caching parcial completado en Service Worker:', err);
    }
}

async function onActivate(event) {
    console.info('Service Worker Busser Tablet: Activado.');
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
    await self.clients.claim();
}

async function onFetch(event) {
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    // 1. Manejo para NAVEGACIONES (rutas de la SPA: /login, /operaciones/..., /admon/...)
    if (event.request.mode === 'navigate') {
        // Red primero: intentar obtener la página más reciente
        try {
            const networkResponse = await fetch(event.request);
            if (networkResponse && (networkResponse.ok || networkResponse.type === 'opaqueredirect')) {
                return networkResponse;
            }
        } catch (err) {
            // Error de red, continuar a fallback
        }

        // Fallback a index.html desde caché o red
        try {
            const cache = await caches.open(cacheName);
            const indexCached = await cache.match('index.html') || await caches.match('index.html');
            if (indexCached) {
                return indexCached;
            }
        } catch (err) { }

        try {
            const indexNetwork = await fetch('index.html');
            if (indexNetwork && indexNetwork.ok) {
                return indexNetwork;
            }
        } catch (err) { }

        // Último recurso para navegación: devolver index.html de cualquier caché existente
        try {
            const anyIndex = await caches.match('index.html') || await caches.match('/index.html');
            if (anyIndex) return anyIndex;
        } catch (err) { }

        try {
            return await fetch(event.request);
        } catch (err) {
            // Si la red falló completamente durante una navegación, intentar servir index.html de caché
            const fallbackIndex = await caches.match('index.html');
            if (fallbackIndex) return fallbackIndex;
            return new Response('<html><body><h2>Modo sin conexión</h2><p>Por favor reconecte a internet y recargue.</p></body></html>', {
                status: 200,
                headers: { 'Content-Type': 'text/html; charset=utf-8' }
            });
        }
    }

    // 2. Manejo para ACTIVOS ESTÁTICOS (dll, wasm, css, js, json, fuentes, imágenes)
    const relativeUrl = event.request.url.substring(self.location.origin.length + 1);
    const isAsset = self.assetsManifest.assets.some(asset => asset.url === relativeUrl);

    if (isAsset) {
        try {
            const cache = await caches.open(cacheName);
            const cachedResponse = await cache.match(event.request);
            if (cachedResponse) {
                return cachedResponse;
            }
        } catch (err) { }
    }

    // 3. Fallback general a la red
    try {
        return await fetch(event.request);
    } catch (err) {
        const fallback = await caches.match(event.request);
        if (fallback) {
            return fallback;
        }
        return new Response('Recurso no disponible temporalmente', { status: 503, statusText: 'Service Unavailable' });
    }
}
