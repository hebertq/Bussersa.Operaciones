window.global = {
    openModal: function (popupId) {
        popupId = "#" + popupId;
        $(popupId).modal("show");
    },
    closeModal: function (popupId) {
        popupId = "#" + popupId;
        $(popupId).modal("hide");
    },
    closeModalNoty: function (popupId, notifica) {
        popupId = "#" + popupId;
        $(popupId).modal("hide");

        console.log(popupId);
        if (notifica != null)
            if (notifica.isNotifica)
                init_error(notifica.errorType, notifica.mensaje);
    }
};

window.scrollToFragment = (fragmentId) => {
    const element = document.getElementById(fragmentId);
    if (element) {
        element.scrollIntoView({ behavior: "smooth", block: "start" });
    }
};

function init_error(tipo, mensaje)
{
    var tipomsj = '';
    if (tipo === 2)
        tipomsj = 'error';
    else if (tipo === 3)
        tipomsj = 'warn';
    else
        tipomsj = 'info';
    Lobibox.notify(tipomsj, {
        msg: mensaje
    });
}

function init_datatable(table, searching, titlefile, Colarray) {
    let colDtArray = Colarray;
    $(document).ready(function () {
        $(table).DataTable({      
            dom: 'l<"sep">Bfrtip', 
            autoWidth: false,
            buttons: [           
                {
                    extend: 'collection',
                    autoClose: 'true',
                    text: '<i class="fa fa-download"></i>',
                    className: 'btn btn-secondary',
                    buttons: [
                        {
                            extend: 'excelHtml5',
                            text: '<i class="fa fa-file-excel-o">  Excel</i>',
                            title: 'Detalle de ' + titlefile,
                            titleAttr: 'Excel',
                            className: 'btn btn-secondary',
                            messageTop: 'La información de esta tabla es propiedad de Bussersa.',
                            customize: function (xlsx) {
                                var sheet = xlsx.xl.worksheets['sheet1.xml'];
                                $('row c', sheet).attr('s', '25');
                                $('row:first c', sheet).attr('s', '47');

                                // Loop over the cells in column `J`
                                $('row c[r^="Z"]', sheet).each(function (key, value) {
                                    if (key != 0 && $('is t', this).text() == 'Failed') {
                                        $(this).attr('s', '11');
                                    }
                                });

                                $('row c[r^="Z"]', sheet).each(function (key, value) {
                                    // Get the value     
                                    $(this).attr('s', '777');
                                });
                            },
                            exportOptions: {
                                format: {
                                    body: function (data, row, column, node) {
                                        data = $('<p>' + data + '</p>').text();
                                        if (colDtArray != null) {
                                            var found = colDtArray.find(x => x = column);
                                            if (found == column) {
                                                let spli = data.split(' ');
                                                let engl = spli[0] + ' ' + (spli[1].length == 7 ? '0' + spli[1] : spli[1]);
                                                return engl;
                                            }   
                                        }                                                     
                                        return $.isNumeric(data.replace(',', '.')) ? data.replace(',', '.') : data;
                                    }
                                }
                            }
                        },
                        {
                            extend: 'csvHtml5',
                            text: '<i class="fa fa-file-text-o">  CSV</i>',
                            title: 'Detalle de ' + titlefile,
                            className: 'btn btn-secondary',
                            titleAttr: 'CSV',
                            messageTop: 'La información de esta tabla es propiedad de Bussersa.'
                        },
                        {
                            extend: 'pdfHtml5',
                            text: '<i class="fa fa-file-pdf-o">  PDF</i>',
                            title: 'Detalle de ' + titlefile,
                            titleAttr: 'PDF',
                            className: 'btn btn-secondary',
                            messageBottom: null
                        },
                        {
                            extend: 'print',
                            text: '<i class="fa fa-desktop">  Imprimir</i>',
                            title: 'Detalle de ' + titlefile,
                            className: 'btn btn-secondary',
                            titleAttr: 'print',
                            messageBottom: null
                        }
                    ]
                }
            ],
            "lengthMenu": [[10, 25, 50, 100, -1], ["10", "25", "50", "100", "Todos"]],
            "pageLength": 10,
            "searching": searching,
            "language": {
                "sProcessing": "Procesando...",
                "bCopy": "Copiar",
                "sLengthMenu": "Mostrar _MENU_ registros",
                "spageLength": " _MENU_  filas",
                "sZeroRecords": "No se encontraron resultados",
                "sEmptyTable": "Ningún dato disponible en esta tabla",
                "sInfo": "Registros del _START_ al _END_ de un total de _TOTAL_ ",
                "sInfoEmpty": "Registros del 0 al 0 de un total de 0 registros",
                "sInfoFiltered": "(filtrado de un total de _MAX_ registros)",
                "sInfoPostFix": "",
                "sSearch": "Buscar:",
                "sUrl": "",
                "sInfoThousands": ",",
                "sLoadingRecords": "Cargando...",
                "oPaginate": {
                    "sFirst": "Primero",
                    "sLast": "Último",
                    "sNext": "Siguiente",
                    "sPrevious": "Anterior"
                },
                "oAria": {
                    "sSortAscending": ": Activar para ordenar la columna de manera ascendente",
                    "sSortDescending": ": Activar para ordenar la columna de manera descendente"
                }         
            },
            order: [[0, 'desc'], [1, 'desc'], [2, 'desc']],
            scrollY: 400,
            scrollX: true,
            scrollCollapse: true,
            responsive: true,
            fixedHeader: true,
            fixedColumns: true,
            destroy: true
        });
    });
}


function limpiaForm(miForm) {
    // recorremos todos los campos que tiene el formulario
    $(':input', miForm).each(function () {
        var type = this.type;
        var tag = this.tagName.toLowerCase();

        //limpiamos los valores de los campos…
        if (type === 'text' && (this.id === "Id" || this.id === "id")) {
            this.value = '0';
        }           
        else if (type === 'text' || type === 'password' || tag === 'textarea')
            this.value = '';
        // excepto de los checkboxes y radios, le quitamos el checked
        // pero su valor no debe ser cambiado
        else if (type === 'checkbox' || type === 'radio')
            this.checked = false;
        // los selects le ponesmos el indice a -
        else if (tag === 'select')
            this.selectedIndex = -1;
        else if (type === 'hidden')           
        {
            console.log(this);
            this.value = '0';
        }         
    });
};

function DataTablesRemove(table) {
    $(document).ready(function () {
        table = "#" + table;
        console.log(Hola);
        $(table).DataTable().destroy();
    });
}

function downloadFile(contentType, base64Data, fileName) {
    const linkSource = `data:${contentType};base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    try {
        downloadLink.href     = linkSource;
        downloadLink.download = fileName;
        downloadLink.click();
    } catch (err) {
        console.error(err);
    }   
}

