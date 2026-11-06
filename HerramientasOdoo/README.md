# ⚡ Calculador Automático de Nómina Odoo (1 a 1 con Temporizador)

Herramienta diseñada para el equipo de Nómina y Operaciones de **Bussersa**, resolviendo el problema de calcular la nómina trabajador por trabajador en Odoo (`admin.bussersa.com`).

---

## 🎯 ¿Por qué esta herramienta?

1. **Ahorro de Tiempo y Esfuerzo:** Evita tener que abrir cada uno de los 50 o 100 trabajadores, presionar "Calcular Hoja" y regresar al lote.
2. **Uso de Recursos Locales (Tu Computadora):** La orquestación del proceso se ejecuta en el navegador de tu computadora, liberando de carga al servidor de Odoo.
3. **Protección del Contenedor de Odoo:** Al calcular **1 a 1 de forma automática** con un **temporizador** (pausa de 800 ms entre cada uno), el contenedor del servidor nunca se sobrecarga, nunca se queda sin memoria RAM y nunca da el error `504 Gateway Time-out`.
4. **Visibilidad en Vivo:** Barra de progreso con porcentaje, tiempo restante estimado, nombre del colaborador que se está calculando y conteo de aprobados/errores.

---

## 🚀 Métodos de Uso

### Opción 1: Extensión de Google Chrome (Recomendada - Permanente)
1. En Google Chrome, abre una pestaña y entra a: `chrome://extensions`
2. En la esquina superior derecha, activa el switch **"Modo de desarrollador"**.
3. Haz clic en el botón **"Cargar descomprimida"**.
4. Selecciona la carpeta:
   `D:\Bussersa\Bussersa\src\Bussersa.Operaciones\HerramientasOdoo`
5. ¡Listo! Cada vez que entres a cualquier lote de nómina en Odoo (ej. `admin.bussersa.com/odoo/action-540/11`), verás automáticamente:
   - Un botón en la barra superior: **⚡ Calcular Nómina en Lote**.
   - Un botón flotante táctico en la esquina inferior derecha: **⚡ Calcular Nómina (1 a 1)**.

---

### Opción 2: Marcador en Chrome (Bookmarklet - ¡Sin instalar nada!)
1. Abre en tu navegador el archivo:
   `D:\Bussersa\Bussersa\src\Bussersa.Operaciones\HerramientasOdoo\instalador_bookmarklet.html`
2. Arrastra el botón naranja **"⚡ Calcular Nómina Bussersa"** hacia tu barra de marcadores de Chrome (presiona `Ctrl + Shift + B` si la barra de marcadores no está visible).
3. Cuando estés en Odoo dentro de cualquier lote de nómina, simplemente haz clic en el marcador guardado y la ventana de cálculo interactivo aparecerá al instante.

---

### Opción 3: Script para Tampermonkey / Violentmonkey
Si utilizas el gestor de scripts **Tampermonkey** o **Violentmonkey**:
1. Abre el archivo `bussersa-odoo-payroll-calculator.user.js`.
2. Cópialo y pégalo en un nuevo script de Tampermonkey.
3. Se activará automáticamente en `https://admin.bussersa.com/*`.
