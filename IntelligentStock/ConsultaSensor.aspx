<%@ Page Title="Consulta de sensores IoT" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultaSensor.aspx.cs" Inherits="ConsultaSensor" %>
<%@ Register Src="~/Controles/EstadoSensorIoT.ascx" TagPrefix="uc" TagName="EstadoSensorIoT" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de sensores IoT</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblBusqueda" runat="server" AssociatedControlID="txtBusqueda"
                    CssClass="control-label" Text="Código, MAC o modelo"></asp:Label>
                <asp:TextBox ID="txtBusqueda" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado"
                    CssClass="control-label" Text="Estado"></asp:Label>
                <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(todos)" Value="" />
                    <asp:ListItem Text="Activo" Value="Activo" />
                    <asp:ListItem Text="Inactivo" Value="Inactivo" />
                </asp:DropDownList>
            </div>
        </div>
    </div>

    <p><asp:Literal ID="litResumen" runat="server"></asp:Literal></p>

    <%-- Ficha del sensor elegido con "Ver" en la grilla. --%>
    <uc:EstadoSensorIoT ID="ucEstadoSensor" runat="server" Visible="false" />

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdSensor"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvDetalle_SelectedIndexChanged"
        EmptyDataText="No hay registros para mostrar.">
        <Columns>
            <asp:BoundField DataField="IdSensor" HeaderText="Código" />
            <asp:BoundField DataField="MacAddress" HeaderText="MAC" />
            <asp:BoundField DataField="ModeloTexto" HeaderText="Modelo" />
            <asp:BoundField DataField="TemperaturaMaximaTexto" HeaderText="Temp. máxima" />
            <asp:BoundField DataField="UltimaAlertaTexto" HeaderText="Última alerta" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Ver" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="60px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Buscar" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
