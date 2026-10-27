<%@ Page Title="Baja de sensor IoT" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaSensor.aspx.cs" Inherits="BajaSensor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de sensor IoT</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La baja es lógica: el sensor deja de monitorear pero se conservan sus alertas. Un sensor con alertas sin resolver no se puede dar de baja.</p>


    <div class="checkbox">
        <asp:CheckBox ID="chkMostrarInactivos" runat="server" AutoPostBack="true"
            Text="Mostrar también los dados de baja (para reactivarlos)"
            OnCheckedChanged="chkMostrarInactivos_CheckedChanged" />
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdSensor,Activo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvDetalle_SelectedIndexChanged"
        EmptyDataText="No hay sensores activos para dar de baja.">
        <Columns>
            <asp:BoundField DataField="IdSensor" HeaderText="Código" />
            <asp:BoundField DataField="MacAddress" HeaderText="MAC" />
            <asp:BoundField DataField="ModeloTexto" HeaderText="Modelo" />
            <asp:BoundField DataField="TemperaturaMaximaTexto" HeaderText="Temp. máxima" />
            <asp:BoundField DataField="AlertasAbiertas" HeaderText="Alertas abiertas" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Dar de baja" CssClass="btn btn-danger"
        OnClick="btnAceptar_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma la baja del sensor seleccionado?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
