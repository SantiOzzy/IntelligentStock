<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BuscadorInsumo.ascx.cs" Inherits="Controles_BuscadorInsumo" %>
<asp:TextBox ID="txtNombreInsumo" runat="server" CssClass="form-control" MaxLength="100"
    placeholder="Escriba parte del nombre para filtrar la lista..." autocomplete="off"></asp:TextBox>
<asp:ListBox ID="lstInsumos" runat="server" CssClass="form-control" Rows="6" SelectionMode="Single"
    style="margin-top: 4px;"></asp:ListBox>
<asp:Label ID="lblResultado" runat="server"></asp:Label>
<script type="text/javascript">
    // Filtra la lista mientras se escribe, sin ir al servidor. No distingue
    // mayúsculas ni acentos. Si queda un solo insumo, se elige solo; con Enter
    // se elige el primero que quedó visible (y no se envía el formulario).
    (function () {
        var texto = document.getElementById('<%= txtNombreInsumo.ClientID %>');
        var lista = document.getElementById('<%= lstInsumos.ClientID %>');
        if (!texto || !lista) {
            return;
        }

        function normalizar(valor) {
            valor = (valor || '').toLowerCase();
            return valor.normalize ? valor.normalize('NFD').replace(/[̀-ͯ]/g, '') : valor;
        }

        function filtrar(elegirSiQuedaUno) {
            var buscado = normalizar(texto.value);
            var visibles = [];
            for (var i = 0; i < lista.options.length; i++) {
                var opcion = lista.options[i];
                var coincide = buscado.length === 0 || normalizar(opcion.text).indexOf(buscado) >= 0;
                opcion.hidden = !coincide;
                opcion.style.display = coincide ? '' : 'none';
                if (coincide) {
                    visibles.push(opcion);
                } else if (opcion.selected) {
                    // Un insumo que quedó oculto no puede seguir elegido: si no,
                    // se mandaría al servidor sin que se vea en pantalla.
                    opcion.selected = false;
                }
            }
            if (elegirSiQuedaUno && visibles.length === 1) {
                visibles[0].selected = true;
            }
            return visibles;
        }

        texto.addEventListener('input', function () { filtrar(true); });
        texto.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.keyCode === 13) {
                e.preventDefault();
                var visibles = filtrar(false);
                if (visibles.length > 0) {
                    visibles[0].selected = true;
                }
            }
        });

        // Después de un postback el texto sigue escrito: se vuelve a aplicar el filtro.
        filtrar(false);
    })();
</script>
