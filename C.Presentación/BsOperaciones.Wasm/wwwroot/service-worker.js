// Service Worker de Desarrollo - BUSSERSA Operaciones
// En desarrollo, responde directamente desde la red para no interferir con cambios de código
self.addEventListener('install', async event => {
    console.log('Instalando Service Worker de desarrollo Busser Tablet...');
    self.skipWaiting();
});

self.addEventListener('fetch', event => {
    // Passthrough
});
