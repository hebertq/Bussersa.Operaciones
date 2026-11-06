// ==UserScript==
// @name         Bussersa - Calculador Automático de Nómina Odoo (1 a 1)
// @namespace    https://admin.bussersa.com/
// @version      1.0.0
// @description  Calcula nóminas por lotes (hr.payslip.run) en Odoo de forma automática y secuencial (1 a 1 con temporizador) usando los recursos de la máquina cliente para evitar sobrecargar el contenedor del servidor.
// @author       Bussersa IT
// @match        https://admin.bussersa.com/*
// @grant        none
// @run-at       document-idle
// ==/UserScript==

(function () {
    'use strict';

    console.log('[Bussersa Payroll] Script inicializado en admin.bussersa.com');

    // Estado del proceso de cálculo
    let isRunning = false;
    let isPaused = false;
    let cancelRequested = false;
    let currentBatchId = null;
    let currentBatchName = '';

    // Utilidad de espera (temporizador entre recibos para no asfixiar el contenedor)
    const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    // Función para invocar el API JSON-RPC de Odoo utilizando la sesión activa del usuario
    async function callOdooRpc(model, method, args = [], kwargs = {}) {
        const response = await fetch(`/web/dataset/call_kw/${model}/${method}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            credentials: 'include',
            body: JSON.stringify({
                jsonrpc: '2.0',
                method: 'call',
                params: {
                    model: model,
                    method: method,
                    args: args,
                    kwargs: kwargs
                },
                id: Math.floor(Math.random() * 1000000)
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP Error: ${response.status} ${response.statusText}`);
        }

        const data = await response.json();
        if (data.error) {
            const msg = data.error.data?.message || data.error.message || JSON.stringify(data.error);
            throw new Error(msg);
        }

        return data.result;
    }

    // Detectar si estamos en un lote de nóminas y obtener su ID
    function detectBatchFromUrlOrDom() {
        // Opción 1: Extraer de la URL (ej: /odoo/action-540/11)
        const match = window.location.pathname.match(/action-540\/(\d+)/);
        if (match && match[1]) {
            return parseInt(match[1]);
        }

        // Opción 2: Buscar en la barra de navegación o breadcrumbs de Odoo
        const breadcrumbItems = document.querySelectorAll('.breadcrumb-item, .o_breadcrumb .active, .o_last_breadcrumb_item');
        for (const item of breadcrumbItems) {
            const text = item.textContent || '';
            if (text.toLowerCase().includes('nomina') || text.toLowerCase().includes('vacaciones')) {
                // Hay un lote activo
            }
        }

        return null;
    }

    // Inyectar Estilos CSS Modernos
    function injectStyles() {
        if (document.getElementById('bussersa-payroll-styles')) return;

        const style = document.createElement('style');
        style.id = 'bussersa-payroll-styles';
        style.textContent = `
            /* Botón Flotante y Botón de Cabecera */
            .bs-calc-btn {
                background: linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%);
                color: #ffffff !important;
                border: 1px solid rgba(255, 255, 255, 0.2);
                border-radius: 8px;
                padding: 6px 14px;
                font-weight: 600;
                font-size: 13px;
                cursor: pointer;
                display: inline-flex;
                align-items: center;
                gap: 8px;
                box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.25), 0 2px 4px -2px rgba(37, 99, 235, 0.25);
                transition: all 0.2s ease;
                z-index: 100;
            }
            .bs-calc-btn:hover {
                background: linear-gradient(135deg, #1e40af 0%, #1d4ed8 100%);
                box-shadow: 0 6px 12px -2px rgba(37, 99, 235, 0.35);
                transform: translateY(-1px);
            }
            .bs-calc-btn:active {
                transform: translateY(0);
            }

            /* Botón Flotante Táctico (Esquina Inferior Derecha) */
            .bs-floating-action {
                position: fixed;
                bottom: 24px;
                right: 24px;
                z-index: 9999;
                background: linear-gradient(135deg, #f97316 0%, #ea580c 100%);
                color: white;
                border: 2px solid rgba(255, 255, 255, 0.3);
                border-radius: 50px;
                padding: 12px 22px;
                font-size: 14px;
                font-weight: 700;
                box-shadow: 0 10px 25px -3px rgba(234, 88, 12, 0.4);
                cursor: pointer;
                display: flex;
                align-items: center;
                gap: 10px;
                transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
            }
            .bs-floating-action:hover {
                transform: scale(1.04) translateY(-2px);
                box-shadow: 0 14px 28px -4px rgba(234, 88, 12, 0.5);
            }

            /* Modal Overlay */
            .bs-modal-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100vw;
                height: 100vh;
                background: rgba(15, 23, 42, 0.75);
                backdrop-filter: blur(6px);
                z-index: 999999;
                display: flex;
                align-items: center;
                justify-content: center;
                animation: bsFadeIn 0.2s ease-out;
            }

            /* Modal Box */
            .bs-modal-box {
                background: #ffffff;
                width: 90%;
                max-width: 680px;
                border-radius: 16px;
                box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.35);
                overflow: hidden;
                display: flex;
                flex-direction: column;
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                animation: bsScaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
            }

            @keyframes bsFadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
            @keyframes bsScaleUp {
                from { transform: scale(0.95); opacity: 0; }
                to { transform: scale(1); opacity: 1; }
            }

            .bs-modal-header {
                background: linear-gradient(135deg, #1e3a8a 0%, #172554 100%);
                color: white;
                padding: 18px 24px;
                display: flex;
                align-items: center;
                justify-content: space-between;
                border-bottom: 3px solid #f97316;
            }
            .bs-modal-header h3 {
                margin: 0;
                font-size: 1.15rem;
                font-weight: 700;
                display: flex;
                align-items: center;
                gap: 8px;
            }
            .bs-modal-header .bs-close-btn {
                background: transparent;
                border: none;
                color: rgba(255, 255, 255, 0.7);
                font-size: 24px;
                cursor: pointer;
                line-height: 1;
                transition: color 0.15s;
            }
            .bs-modal-header .bs-close-btn:hover {
                color: #ffffff;
            }

            .bs-modal-body {
                padding: 24px;
                max-height: 75vh;
                overflow-y: auto;
            }

            /* Tarjetas de Estadísticas */
            .bs-stats-grid {
                display: grid;
                grid-template-columns: repeat(3, 1fr);
                gap: 12px;
                margin-bottom: 20px;
            }
            .bs-stat-card {
                background: #f8fafc;
                border: 1px solid #e2e8f0;
                border-radius: 10px;
                padding: 12px;
                text-align: center;
            }
            .bs-stat-val {
                font-size: 1.5rem;
                font-weight: 800;
                color: #1e293b;
            }
            .bs-stat-lbl {
                font-size: 0.75rem;
                font-weight: 600;
                text-transform: uppercase;
                color: #64748b;
                margin-top: 2px;
            }

            /* Controles y Temporizador */
            .bs-config-card {
                background: #eff6ff;
                border: 1px solid #bfdbfe;
                border-radius: 10px;
                padding: 14px 18px;
                margin-bottom: 20px;
            }
            .bs-config-row {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 12px;
                margin-bottom: 10px;
            }
            .bs-config-row:last-child {
                margin-bottom: 0;
            }

            /* Barra de Progreso */
            .bs-progress-section {
                margin-bottom: 20px;
            }
            .bs-progress-bar-bg {
                background: #e2e8f0;
                border-radius: 12px;
                height: 20px;
                overflow: hidden;
                position: relative;
                box-shadow: inset 0 2px 4px rgba(0,0,0,0.06);
            }
            .bs-progress-bar-fill {
                background: linear-gradient(90deg, #10b981 0%, #059669 100%);
                height: 100%;
                width: 0%;
                transition: width 0.3s ease;
                display: flex;
                align-items: center;
                justify-content: center;
                color: white;
                font-size: 11px;
                font-weight: 700;
            }

            /* Consola de Log */
            .bs-log-console {
                background: #0f172a;
                color: #f8fafc;
                border-radius: 10px;
                padding: 12px;
                font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
                font-size: 12px;
                height: 160px;
                overflow-y: auto;
                border: 1px solid #334155;
            }
            .bs-log-line {
                margin-bottom: 4px;
                display: flex;
                gap: 6px;
            }
            .bs-log-time { color: #94a3b8; }
            .bs-log-ok { color: #34d399; }
            .bs-log-err { color: #f87171; }
            .bs-log-info { color: #38bdf8; }

            .bs-modal-footer {
                padding: 16px 24px;
                background: #f8fafc;
                border-top: 1px solid #e2e8f0;
                display: flex;
                align-items: center;
                justify-content: flex-end;
                gap: 10px;
            }
            .bs-btn-primary {
                background: #2563eb;
                color: white;
                border: none;
                border-radius: 8px;
                padding: 10px 20px;
                font-weight: 700;
                font-size: 14px;
                cursor: pointer;
                transition: background 0.2s;
            }
            .bs-btn-primary:hover:not(:disabled) {
                background: #1d4ed8;
            }
            .bs-btn-primary:disabled {
                opacity: 0.6;
                cursor: not-allowed;
            }
            .bs-btn-secondary {
                background: #e2e8f0;
                color: #334155;
                border: none;
                border-radius: 8px;
                padding: 10px 18px;
                font-weight: 600;
                font-size: 14px;
                cursor: pointer;
            }
            .bs-btn-secondary:hover {
                background: #cbd5e1;
            }
            .bs-btn-warning {
                background: #f59e0b;
                color: white;
                border: none;
                border-radius: 8px;
                padding: 10px 16px;
                font-weight: 600;
                font-size: 14px;
                cursor: pointer;
            }
            .bs-btn-danger {
                background: #ef4444;
                color: white;
                border: none;
                border-radius: 8px;
                padding: 10px 16px;
                font-weight: 600;
                font-size: 14px;
                cursor: pointer;
            }
        `;
        document.head.appendChild(style);
    }

    // Modal UI y Controlador
    function openPayrollModal(batchId) {
        currentBatchId = batchId;
        injectStyles();

        // Evitar duplicados
        const existing = document.getElementById('bussersa-payroll-modal');
        if (existing) existing.remove();

        const overlay = document.createElement('div');
        overlay.id = 'bussersa-payroll-modal';
        overlay.className = 'bs-modal-overlay';

        overlay.innerHTML = `
            <div class="bs-modal-box">
                <div class="bs-modal-header">
                    <h3>
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                            <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
                        </svg>
                        Cálculo Automático de Nómina (Lote #${batchId})
                    </h3>
                    <button class="bs-close-btn" id="bs-btn-close-modal">&times;</button>
                </div>
                <div class="bs-modal-body">
                    <!-- Resumen del Lote -->
                    <div class="bs-stats-grid">
                        <div class="bs-stat-card">
                            <div class="bs-stat-val" id="bs-stat-total">...</div>
                            <div class="bs-stat-lbl">Total Recibos</div>
                        </div>
                        <div class="bs-stat-card">
                            <div class="bs-stat-val" style="color: #f97316;" id="bs-stat-draft">...</div>
                            <div class="bs-stat-lbl">En Borrador (Pendientes)</div>
                        </div>
                        <div class="bs-stat-card">
                            <div class="bs-stat-val" style="color: #10b981;" id="bs-stat-done">...</div>
                            <div class="bs-stat-lbl">Ya Calculados / Hechos</div>
                        </div>
                    </div>

                    <!-- Configuración y Temporizador -->
                    <div class="bs-config-card">
                        <div class="bs-config-row">
                            <label style="font-weight: 600; color: #1e3a8a; font-size: 13px;">
                                ⏱ Temporizador entre cálculos (Pausa por trabajador):
                            </label>
                            <select id="bs-select-delay" style="padding: 4px 8px; border-radius: 6px; border: 1px solid #93c5fd; font-weight: 600;">
                                <option value="400">400 ms (Rápido)</option>
                                <option value="800" selected>800 ms (Recomendado - Óptimo)</option>
                                <option value="1200">1.2 seg (Seguro)</option>
                                <option value="2000">2.0 seg (Baja Carga)</option>
                            </select>
                        </div>
                        <div class="bs-config-row">
                            <label style="display: flex; align-items: center; gap: 8px; font-size: 13px; color: #334155; cursor: pointer;">
                                <input type="checkbox" id="bs-chk-only-draft" checked style="width: 16px; height: 16px; cursor: pointer;">
                                <span>Calcular <b>únicamente recibos en borrador</b> (Recomendado)</span>
                            </label>
                        </div>
                        <div style="font-size: 11px; color: #64748b; margin-top: 6px; line-height: 1.4;">
                            💡 <b>Modo Cliente Activo:</b> Tu navegador calcula un empleado a la vez liberando la memoria del contenedor Odoo. Evita caídas del servidor y errores 504.
                        </div>
                    </div>

                    <!-- Progreso -->
                    <div class="bs-progress-section" id="bs-progress-container" style="display: none;">
                        <div style="display: flex; justify-content: space-between; font-size: 13px; font-weight: 700; margin-bottom: 6px;">
                            <span id="bs-progress-text">Progreso: 0 de 0 (0%)</span>
                            <span id="bs-progress-eta" style="color: #64748b; font-weight: 600;">Tiempo restante: --</span>
                        </div>
                        <div class="bs-progress-bar-bg">
                            <div class="bs-progress-bar-fill" id="bs-progress-fill">0%</div>
                        </div>
                        <div id="bs-current-worker" style="font-size: 12px; color: #1e3a8a; font-weight: 600; margin-top: 6px; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;">
                            Esperando inicio...
                        </div>
                    </div>

                    <!-- Consola Log -->
                    <div class="bs-log-console" id="bs-log-console">
                        <div class="bs-log-line">
                            <span class="bs-log-time">[${new Date().toLocaleTimeString()}]</span>
                            <span class="bs-log-info">Iniciando consulta de recibos del lote #${batchId}...</span>
                        </div>
                    </div>
                </div>

                <div class="bs-modal-footer">
                    <button class="bs-btn-secondary" id="bs-btn-cancel-close">Cerrar</button>
                    <button class="bs-btn-warning" id="bs-btn-pause" style="display: none;">⏸ Pausar</button>
                    <button class="bs-btn-danger" id="bs-btn-stop" style="display: none;">⏹ Detener</button>
                    <button class="bs-btn-primary" id="bs-btn-start" style="background: linear-gradient(135deg, #10b981 0%, #059669 100%);">
                        ▶ Iniciar Cálculo Automático (1 a 1)
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(overlay);

        // Referencias del DOM del modal
        const btnClose = overlay.querySelector('#bs-btn-close-modal');
        const btnCancelClose = overlay.querySelector('#bs-btn-cancel-close');
        const btnStart = overlay.querySelector('#bs-btn-start');
        const btnPause = overlay.querySelector('#bs-btn-pause');
        const btnStop = overlay.querySelector('#bs-btn-stop');
        const logConsole = overlay.querySelector('#bs-log-console');
        const progressContainer = overlay.querySelector('#bs-progress-container');
        const progressFill = overlay.querySelector('#bs-progress-fill');
        const progressText = overlay.querySelector('#bs-progress-text');
        const progressEta = overlay.querySelector('#bs-progress-eta');
        const currentWorkerEl = overlay.querySelector('#bs-current-worker');

        const statTotal = overlay.querySelector('#bs-stat-total');
        const statDraft = overlay.querySelector('#bs-stat-draft');
        const statDone = overlay.querySelector('#bs-stat-done');

        function appendLog(msg, type = 'info') {
            const line = document.createElement('div');
            line.className = 'bs-log-line';
            const time = new Date().toLocaleTimeString();
            let typeClass = 'bs-log-info';
            if (type === 'ok') typeClass = 'bs-log-ok';
            if (type === 'err') typeClass = 'bs-log-err';

            line.innerHTML = `<span class="bs-log-time">[${time}]</span> <span class="${typeClass}">${msg}</span>`;
            logConsole.appendChild(line);
            logConsole.scrollTop = logConsole.scrollHeight;
        }

        // Cargar datos iniciales del lote
        let allSlips = [];
        async function loadBatchInfo() {
            try {
                appendLog('Consultando recibos asociados en Odoo...', 'info');
                // Buscar recibos del lote
                allSlips = await callOdooRpc('hr.payslip', 'search_read', [
                    [['payslip_run_id', '=', batchId]]
                ], {
                    fields: ['id', 'name', 'number', 'employee_id', 'state', 'net_wage'],
                    order: 'id asc'
                });

                const total = allSlips.length;
                const draft = allSlips.filter(s => s.state === 'draft').length;
                const done = allSlips.filter(s => s.state !== 'draft').length;

                statTotal.textContent = total;
                statDraft.textContent = draft;
                statDone.textContent = done;

                appendLog(`Lote cargado: ${total} recibos en total (${draft} en borrador, ${done} calculados/cerrados).`, 'ok');

                if (draft === 0 && total > 0) {
                    appendLog('Aviso: Todos los recibos ya están calculados. Si deseas recalcularlos, desmarca la casilla "únicamente en borrador".', 'info');
                } else if (total === 0) {
                    appendLog('Advertencia: No se encontraron recibos en este lote. Genera los recibos primero con "Generate Payslips".', 'err');
                    btnStart.disabled = true;
                }
            } catch (err) {
                appendLog(`Error al consultar lote: ${err.message}`, 'err');
            }
        }

        loadBatchInfo();

        // Cerrar modal
        function closeModal() {
            if (isRunning) {
                if (!confirm('El cálculo de la nómina está en progreso. ¿Deseas detenerlo y cerrar?')) {
                    return;
                }
                cancelRequested = true;
            }
            overlay.remove();
        }

        btnClose.onclick = closeModal;
        btnCancelClose.onclick = closeModal;

        // Pausar / Reanudar
        btnPause.onclick = () => {
            if (!isRunning) return;
            isPaused = !isPaused;
            if (isPaused) {
                btnPause.textContent = '▶ Continuar';
                btnPause.style.background = '#10b981';
                appendLog('⏸ Proceso pausado por el usuario.', 'info');
            } else {
                btnPause.textContent = '⏸ Pausar';
                btnPause.style.background = '#f59e0b';
                appendLog('▶ Proceso reanudado.', 'info');
            }
        };

        // Detener
        btnStop.onclick = () => {
            if (!isRunning) return;
            if (confirm('¿Estás seguro de detener el cálculo? Los recibos ya calculados se conservarán.')) {
                cancelRequested = true;
                appendLog('⏹ Cancelación solicitada por el usuario. Finalizando...', 'err');
            }
        };

        // Iniciar Cálculo Secuencial 1 a 1
        btnStart.onclick = async () => {
            if (isRunning) return;

            const onlyDraft = overlay.querySelector('#bs-chk-only-draft').checked;
            const delayMs = parseInt(overlay.querySelector('#bs-select-delay').value) || 800;

            const targetSlips = onlyDraft ? allSlips.filter(s => s.state === 'draft') : allSlips;

            if (targetSlips.length === 0) {
                alert('No hay recibos seleccionados para calcular.');
                return;
            }

            // Preparar UI
            isRunning = true;
            isPaused = false;
            cancelRequested = false;

            btnStart.style.display = 'none';
            btnPause.style.display = 'inline-block';
            btnStop.style.display = 'inline-block';
            btnCancelClose.disabled = true;
            btnClose.style.display = 'none';
            overlay.querySelector('#bs-select-delay').disabled = true;
            overlay.querySelector('#bs-chk-only-draft').disabled = true;

            progressContainer.style.display = 'block';

            const total = targetSlips.length;
            let successCount = 0;
            let errorCount = 0;
            const startTime = Date.now();

            appendLog(`🚀 INICIANDO CÁLCULO SECUENCIAL: ${total} recibos seleccionados.`, 'info');
            appendLog(`Temporizador configurado a ${delayMs} ms entre cada trabajador.`, 'info');

            for (let i = 0; i < total; i++) {
                if (cancelRequested) {
                    appendLog('⏹ Cálculo cancelado por el usuario.', 'err');
                    break;
                }

                // Si está pausado, esperar
                while (isPaused && !cancelRequested) {
                    await sleep(300);
                }

                const slip = targetSlips[i];
                const empName = Array.isArray(slip.employee_id) ? slip.employee_id[1] : (slip.name || `ID #${slip.id}`);
                const slipNum = slip.number || `ID ${slip.id}`;
                const idx = i + 1;

                // Actualizar UI de Progreso
                const pct = Math.round((idx / total) * 100);
                progressFill.style.width = `${pct}%`;
                progressFill.textContent = `${pct}%`;
                progressText.textContent = `Progreso: ${idx} de ${total} (${pct}%)`;
                currentWorkerEl.textContent = `⚙️ Calculando: [${slipNum}] ${empName}...`;

                // Calcular ETA (Tiempo Restante Estimado)
                const elapsedSec = (Date.now() - startTime) / 1000;
                const avgSecPerSlip = elapsedSec / idx;
                const remainingSec = Math.round(avgSecPerSlip * (total - idx));
                const mins = Math.floor(remainingSec / 60);
                const secs = remainingSec % 60;
                progressEta.textContent = `Tiempo restante: ${mins > 0 ? `${mins}m ` : ''}${secs}s`;

                // Ejecutar compute_sheet
                try {
                    const slipStart = Date.now();
                    await callOdooRpc('hr.payslip', 'compute_sheet', [[slip.id]]);
                    const durationSec = ((Date.now() - slipStart) / 1000).toFixed(1);

                    successCount++;
                    appendLog(`[${idx}/${total}] ✅ ${empName} (${slipNum}) - OK en ${durationSec}s`, 'ok');
                } catch (calcErr) {
                    errorCount++;
                    appendLog(`[${idx}/${total}] ❌ ERROR en ${empName}: ${calcErr.message}`, 'err');
                }

                // Actualizar contadores
                statDraft.textContent = total - successCount;
                statDone.textContent = parseInt(statDone.textContent || '0') + 1;

                // Pausa / Temporizador para dar respiro al contenedor del servidor
                if (i < total - 1 && !cancelRequested) {
                    await sleep(delayMs);
                }
            }

            // Finalización del proceso
            isRunning = false;
            btnPause.style.display = 'none';
            btnStop.style.display = 'none';
            btnClose.style.display = 'inline-block';
            btnCancelClose.disabled = false;
            btnCancelClose.textContent = 'Cerrar Ventana';

            const totalTimeSec = Math.round((Date.now() - startTime) / 1000);
            const totalMins = Math.floor(totalTimeSec / 60);
            const totalSecs = totalTimeSec % 60;

            currentWorkerEl.textContent = `✨ Proceso completado en ${totalMins > 0 ? `${totalMins}m ` : ''}${totalSecs}s. Exitosos: ${successCount} | Fallidos: ${errorCount}`;

            if (errorCount === 0 && !cancelRequested) {
                appendLog(`🎉 ¡ÉXITO TOTAL! Los ${successCount} recibos fueron calculados correctamente sin sobrecargar el servidor.`, 'ok');
            } else {
                appendLog(`Terminado con ${successCount} exitosos y ${errorCount} errores.`, errorCount > 0 ? 'err' : 'info');
            }

            // Agregar botón para recargar la página y ver los resultados en Odoo
            const btnReload = document.createElement('button');
            btnReload.className = 'bs-btn-primary';
            btnReload.innerHTML = '🔄 Recargar Pantalla de Odoo';
            btnReload.onclick = () => window.location.reload();
            overlay.querySelector('.bs-modal-footer').appendChild(btnReload);
        };
    }

    // Inyector de Botón en la Interfaz de Odoo
    function checkAndInjectButtons() {
        const batchId = detectBatchFromUrlOrDom();
        if (!batchId) {
            // Si no estamos en un lote, quitar botón flotante si existiera
            const oldFloating = document.getElementById('bs-floating-calc-btn');
            if (oldFloating) oldFloating.remove();
            return;
        }

        injectStyles();

        // 1. Inyectar botón flotante táctico (si no existe ya)
        if (!document.getElementById('bs-floating-calc-btn')) {
            const floatBtn = document.createElement('button');
            floatBtn.id = 'bs-floating-calc-btn';
            floatBtn.className = 'bs-floating-action';
            floatBtn.innerHTML = `
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
                </svg>
                <span>⚡ Calcular Nómina (1 a 1)</span>
            `;
            floatBtn.onclick = () => openPayrollModal(batchId);
            document.body.appendChild(floatBtn);
        }

        // 2. Inyectar botón en la barra superior de acciones (ControlPanel de Odoo)
        const controlPanel = document.querySelector('.o_control_panel_actions, .o_cp_buttons, .o_form_statusbar, .o_statusbar_buttons');
        if (controlPanel && !document.getElementById('bs-header-calc-btn')) {
            const headerBtn = document.createElement('button');
            headerBtn.id = 'bs-header-calc-btn';
            headerBtn.type = 'button';
            headerBtn.className = 'btn btn-primary bs-calc-btn me-2';
            headerBtn.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
                </svg>
                <span>⚡ Calcular Nómina en Lote</span>
            `;
            headerBtn.onclick = () => openPayrollModal(batchId);
            controlPanel.prepend(headerBtn);
        }
    }

    // Observar cambios en el DOM (debido a que Odoo es una Single Page Application con OWL)
    const observer = new MutationObserver(() => {
        checkAndInjectButtons();
    });

    observer.observe(document.body, { childList: true, subtree: true });

    // Ejecución inicial
    window.addEventListener('load', checkAndInjectButtons);
    checkAndInjectButtons();

    // Exponer función global para abrir modal por consola o bookmarklet
    window.BussersaCalcularNomina = openPayrollModal;
})();
