<%@ Page Title="Procesar alerta térmica IoT" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ProcesarAlertaTermica.aspx.cs" Inherits="ProcesarAlertaTermica" %>
<%@ Register Src="~/Controles/EstadoSensorIoT.ascx" TagPrefix="uc" TagName="EstadoSensorIoT" %>
<%@ Register Src="~/Controles/ImpactoEconomico.ascx" TagPrefix="uc" TagName="ImpactoEconomico" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Procesar alerta térmica IoT</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Las alertas las genera el sistema externo de telemetría; acá se resuelven.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblAlerta" runat="server" AssociatedControlID="ddlAlerta"
                    CssClass="control-label" Text="Alerta a procesar (*)"></asp:Label>
                <asp:DropDownList ID="ddlAlerta" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlAlerta_SelectedIndexChanged">
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvAlerta" runat="server" ControlToValidate="ddlAlerta"
                    InitialValue="0" ValidationGroup="Procesar" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Elija la alerta a procesar."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado"
                    CssClass="control-label" Text="Nuevo estado (*)"></asp:Label>
                <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                    <asp:ListItem Text="Pendiente" Value="Pendiente" />
                    <asp:ListItem Text="En revisión" Value="En revisión" />
                    <asp:ListItem Text="Resuelta" Value="Resuelta" />
                    <asp:ListItem Text="Descartada" Value="Descartada" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblObservaciones" runat="server" AssociatedControlID="txtObservaciones"
                    CssClass="control-label" Text="Observaciones (*)"></asp:Label>
                <asp:TextBox ID="txtObservaciones" runat="server" CssClass="form-control"
                    TextMode="MultiLine" Rows="3" MaxLength="500"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvObservaciones" runat="server" ControlToValidate="txtObservaciones"
                    ValidationGroup="Procesar" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Indique la acción correctiva realizada."></asp:RequiredFieldValidator>
            </div>
        </div>
    </div>

    <%-- Datos del sensor de la alerta elegida y lo que vale la mercadería en riesgo. --%>
    <asp:Panel ID="pnlAlerta" runat="server" Visible="false" CssClass="row">
        <div class="col-md-6">
            <uc:EstadoSensorIoT ID="ucEstadoSensor" runat="server" />
        </div>
        <div class="col-md-6">
            <uc:ImpactoEconomico ID="ucImpacto" runat="server" />
        </div>
    </asp:Panel>

    <p><strong>Alertas registradas</strong></p>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="No hay registros para mostrar.">
        <Columns>
            <asp:BoundField DataField="IdAlerta" HeaderText="Código" />
            <asp:BoundField DataField="SensorTexto" HeaderText="Sensor" />
            <asp:BoundField DataField="FechaHoraTexto" HeaderText="Fecha y hora" />
            <asp:BoundField DataField="TemperaturaTexto" HeaderText="Temperatura alcanzada" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Procesar alerta" CssClass="btn btn-primary"
        ValidationGroup="Procesar" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnExportar" runat="server" Text="Exportar alertas (XML)" CssClass="btn btn-default"
        OnClick="btnExportar_Click" CausesValidation="false" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
