function saveMessage(firstName, lastName) {
    document.getElementById('divServerValidations').innerText = firstName + ' ' + lastName + ' has been saved successfully!';
}

function setFocusOnElement(element) {
    element.focus();
}

function getCities() {    
    var cities = ['New York', 'Los Angeles', 'Chicago', 'Houston', 'Phoenix', 'Philadelphia', 'San Antonio',
        'San Diego', 'Dallas', 'San Jose', 'Austin', 'Jacksonville', 'Fort Worth', 'Columbus', 'San Francisco',
        'Charlotte', 'Indianapolis', 'Seattle', 'Denver', 'Washington'];
    return cities;
}

window.firmaHelpers = {
    copiarHtml: async function (htmlContent) {
        try {
            if (navigator.clipboard && window.ClipboardItem) {
                const blobHtml = new Blob([htmlContent], { type: "text/html" });
                const blobText = new Blob([htmlContent], { type: "text/plain" });
                const data = [new ClipboardItem({ "text/html": blobHtml, "text/plain": blobText })];
                await navigator.clipboard.write(data);
                return true;
            } else {
                await navigator.clipboard.writeText(htmlContent);
                return true;
            }
        } catch (err) {
            console.error("Error al copiar firma:", err);
            try {
                const textarea = document.createElement("textarea");
                textarea.value = htmlContent;
                document.body.appendChild(textarea);
                textarea.select();
                document.execCommand("copy");
                document.body.removeChild(textarea);
                return true;
            } catch (e) {
                return false;
            }
        }
    },
    descargarImagenFirma: function (elementId, filename) {
        return new Promise((resolve, reject) => {
            const element = document.getElementById(elementId);
            if (!element) {
                reject("Elemento no encontrado");
                return;
            }
            
            if (typeof html2canvas === 'function') {
                html2canvas(element, { scale: 3, useCORS: true, allowTaint: true, backgroundColor: "#ffffff" }).then(canvas => {
                    const link = document.createElement('a');
                    link.download = filename || 'Firma_BUSSERSA.png';
                    link.href = canvas.toDataURL('image/png');
                    link.click();
                    resolve(true);
                }).catch(err => {
                    console.error("Error al generar canvas:", err);
                    reject(err);
                });
            } else {
                console.warn("html2canvas no está cargado");
                reject("html2canvas no cargado");
            }
        });
    }
};

// Lienzo táctil de firma digital (Tablet Touch + Stylus + Mouse) e Impresión de Recibos Oficiales
window.signaturePadInterop = {
    pads: {},
    init: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return false;

        if (this.pads[canvasId] && this.pads[canvasId].cleanup) {
            this.pads[canvasId].cleanup();
        }

        const ctx = canvas.getContext('2d');
        const dpr = Math.max(window.devicePixelRatio || 1, 1);
        const rect = canvas.getBoundingClientRect();
        const width = rect.width > 20 ? rect.width : 420;
        const height = rect.height > 20 ? rect.height : 155;

        canvas.width = Math.round(width * dpr);
        canvas.height = Math.round(height * dpr);
        ctx.scale(dpr, dpr);

        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, width, height);
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        ctx.strokeStyle = '#0f172a';
        ctx.lineWidth = 2.4;

        const state = {
            canvas,
            ctx,
            width,
            height,
            drawing: false,
            hasStrokes: false,
            lastX: 0,
            lastY: 0
        };

        const getPos = (e) => {
            const r = canvas.getBoundingClientRect();
            const scaleX = width / (r.width || width);
            const scaleY = height / (r.height || height);
            let clientX = e.clientX;
            let clientY = e.clientY;
            if (e.touches && e.touches.length > 0) {
                clientX = e.touches[0].clientX;
                clientY = e.touches[0].clientY;
            }
            return {
                x: (clientX - r.left) * scaleX,
                y: (clientY - r.top) * scaleY
            };
        };

        const onDown = (e) => {
            e.preventDefault();
            if (canvas.setPointerCapture && e.pointerId !== undefined) {
                try { canvas.setPointerCapture(e.pointerId); } catch (_) { }
            }
            const pos = getPos(e);
            state.drawing = true;
            state.hasStrokes = true;
            state.lastX = pos.x;
            state.lastY = pos.y;
            ctx.beginPath();
            ctx.arc(pos.x, pos.y, 1.1, 0, Math.PI * 2);
            ctx.fillStyle = '#0f172a';
            ctx.fill();
        };

        const onMove = (e) => {
            if (!state.drawing) return;
            e.preventDefault();
            const pos = getPos(e);
            ctx.beginPath();
            ctx.moveTo(state.lastX, state.lastY);
            ctx.lineTo(pos.x, pos.y);
            ctx.strokeStyle = '#0f172a';
            ctx.lineWidth = 2.4;
            ctx.stroke();
            state.lastX = pos.x;
            state.lastY = pos.y;
        };

        const onUp = (e) => {
            if (!state.drawing) return;
            e.preventDefault();
            state.drawing = false;
        };

        canvas.style.touchAction = 'none';
        canvas.addEventListener('pointerdown', onDown);
        canvas.addEventListener('pointermove', onMove);
        canvas.addEventListener('pointerup', onUp);
        canvas.addEventListener('pointercancel', onUp);

        state.cleanup = () => {
            canvas.removeEventListener('pointerdown', onDown);
            canvas.removeEventListener('pointermove', onMove);
            canvas.removeEventListener('pointerup', onUp);
            canvas.removeEventListener('pointercancel', onUp);
        };

        this.pads[canvasId] = state;
        return true;
    },
    clear: function (canvasId) {
        const pad = this.pads[canvasId];
        if (!pad) {
            this.init(canvasId);
            return;
        }
        pad.ctx.fillStyle = '#ffffff';
        pad.ctx.fillRect(0, 0, pad.width, pad.height);
        pad.hasStrokes = false;
    },
    hasSignature: function (canvasId) {
        const pad = this.pads[canvasId];
        return !!(pad && pad.hasStrokes);
    },
    getDataUrl: function (canvasId) {
        const pad = this.pads[canvasId];
        if (!pad || !pad.hasStrokes) return '';
        return pad.canvas.toDataURL('image/png');
    },
    printHtmlWindow: function (title, htmlBody) {
        const win = window.open('', '_blank', 'width=920,height=780');
        if (!win) {
            window.print();
            return false;
        }
        win.document.open();
        win.document.write(`<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="utf-8" />
<title>${title || 'Boleta de Autorización de Horas Extras - BUSSERSA'}</title>
<style>
  @page { size: letter portrait; margin: 12mm; }
  * { box-sizing: border-box; font-family: 'Segoe UI', Arial, sans-serif; }
  body { margin: 0; padding: 16px; background: #f1f5f9; color: #0f172a; }
  .toolbar-no-print { display: flex; justify-content: flex-end; gap: 10px; max-width: 820px; margin: 0 auto 14px auto; }
  .btn-print { background: #013161; color: #fff; border: none; padding: 10px 18px; border-radius: 8px; font-weight: 700; cursor: pointer; font-size: 14px; }
  .btn-close { background: #64748b; color: #fff; border: none; padding: 10px 14px; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 14px; }
  .boleta-sheet { background: #fff; max-width: 820px; margin: 0 auto 18px auto; border: 1.5px solid #013161; border-radius: 10px; padding: 20px 24px; page-break-inside: avoid; }
  .boleta-break { page-break-after: always; }
  @media print {
    body { background: #fff; padding: 0; }
    .toolbar-no-print { display: none !important; }
    .boleta-sheet { box-shadow: none; margin: 0 auto 12mm auto; }
  }
</style>
</head>
<body>
  <div class="toolbar-no-print">
    <button class="btn-print" onclick="window.print()">🖨️ Imprimir / Guardar PDF</button>
    <button class="btn-close" onclick="window.close()">Cerrar</button>
  </div>
  ${htmlBody}
</body>
</html>`);
        win.document.close();
        setTimeout(() => { try { win.focus(); } catch (_) { } }, 250);
        return true;
    }
};
