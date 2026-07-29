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
