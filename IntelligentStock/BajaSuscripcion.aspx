<%@ Page Title="Baja de suscripción" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaSuscripcion.aspx.cs" Inherits="BajaSuscripcion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de suscripción</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La baja detiene los cobros futuros. El servicio sigue habilitado hasta el cierre del período ya pagado. Las suscripciones dadas de baja quedan en la lista con la fecha en que termina (o terminó) el servicio.</p>


    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdSuscripcion"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvDetalle_SelectedIndexChanged"
        EmptyDataText="No hay suscripciones registradas.">
        <Columns>
            <asp:BoundField DataField="IdSuscripcion" HeaderText="Código" />
            <asp:BoundField DataField="TipoPlan" HeaderText="Plan" />
            <asp:BoundField DataField="AbonoTexto" HeaderText="Abono mensual" />
            <asp:BoundField DataField="TokenEnmascarado" HeaderText="Token de pago" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:BoundField DataField="FechaInicioTexto" HeaderText="Fecha de inicio" />
            <asp:BoundField DataField="VigenciaTexto" HeaderText="Vigencia" />
            <asp:TemplateField ItemStyle-Width="100px">
                <ItemTemplate>
                    <%-- Solo las activas se pueden dar de baja. --%>
                    <asp:Button ID="btnSeleccionar" runat="server" Text="Seleccionar" CommandName="Select"
                        CssClass="btn btn-xs btn-default" CausesValidation="false"
                        Visible='<%# (bool)Eval("Activa") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Dar de baja" CssClass="btn btn-danger"
        OnClick="btnAceptar_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma la baja de la suscripción seleccionada? Se detendrán los cobros.');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
