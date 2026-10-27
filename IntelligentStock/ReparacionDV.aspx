<%@ Page Title="Reparación del dígito verificador" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ReparacionDV.aspx.cs" Inherits="ReparacionDV" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2 class="text-danger">Inconsistencia en la base de datos</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <asp:Panel ID="pnlDetalle" runat="server">

        <p>
            Se detectó una diferencia entre los dígitos verificadores almacenados y los
            recalculados. A continuación se indica la ubicación exacta de cada inconsistencia.
        </p>

        <asp:GridView ID="gvInconsistencias" runat="server" AutoGenerateColumns="false"
            CssClass="table table-bordered table-condensed table-striped" GridLines="None"
            EmptyDataText="No se detectaron inconsistencias.">
            <Columns>
                <asp:BoundField DataField="Tabla" HeaderText="Tabla" />
                <asp:BoundField DataField="FilaTexto" HeaderText="Fila" />
                <asp:BoundField DataField="ClaveFila" HeaderText="Clave del registro" />
                <asp:BoundField DataField="ColumnaTexto" HeaderText="Columna" />
                <asp:BoundField DataField="TipoTexto" HeaderText="Tipo" />
                <asp:BoundField DataField="Descripcion" HeaderText="Detalle" />
            </Columns>
        </asp:GridView>

        <hr />

        <p><strong>Opciones disponibles:</strong></p>

        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar y volver al inicio de sesión"
            CssClass="btn btn-default" OnClick="btnCancelar_Click" CausesValidation="false" />

        <asp:Button ID="btnRecalcular" runat="server" Text="Recalcular dígito verificador"
            CssClass="btn btn-warning" OnClick="btnRecalcular_Click" CausesValidation="false"
            OnClientClick="return confirm('Se van a recalcular los dígitos verificadores ignorando la inconsistencia detectada. ¿Continuar?');" />

        <asp:Button ID="btnRestore" runat="server" Text="Restaurar la base de datos"
            CssClass="btn btn-danger" OnClick="btnRestore_Click" CausesValidation="false" />

        <div class="well well-sm" style="margin-top: 15px">
            <small>
                <strong>Cancelar:</strong> cierra la sesión y vuelve al inicio de sesión sin tocar nada.<br />
                <strong>Recalcular:</strong> vuelve a calcular los dígitos sobre los datos actuales. El sistema
                sigue funcionando con normalidad, pero la alteración queda aceptada como válida.<br />
                <strong>Restaurar:</strong> lleva la base a un estado anterior a partir de un archivo .bak o de un script .sql.<br />
                <strong>Base recién creada:</strong> todas sus tablas aparecen como "Sin dígitos verificadores".
                En ese caso corresponde Recalcular, para calcularlos por primera vez. Si aparecen solo algunas
                tablas, alguien borró sus dígitos: conviene revisar los datos antes de recalcular.
            </small>
        </div>

    </asp:Panel>

</asp:Content>
