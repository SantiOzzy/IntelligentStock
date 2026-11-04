<%@ Page Title="Exportar reporte gerencial" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ExportarReporteGerencial.aspx.cs" Inherits="ExportarReporteGerencial" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Exportar reporte gerencial</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Elija el reporte, el período (hasta un año) y el formato. El archivo se genera en el servidor (carpeta App_Data\Reportes) y se descarga al terminar.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblReporte" runat="server" AssociatedControlID="ddlReporte"
                    CssClass="control-label" Text="Tipo de reporte (*)"></asp:Label>
                <asp:DropDownList ID="ddlReporte" runat="server" CssClass="form-control">
                    <asp:ListItem Text="Consumo por insumo" Value="Consumo por insumo" />
                    <asp:ListItem Text="Ventas por receta" Value="Ventas por receta" />
                    <asp:ListItem Text="Órdenes por proveedor" Value="Órdenes por proveedor" />
                    <asp:ListItem Text="Alertas térmicas" Value="Alertas térmicas" />
                    <asp:ListItem Text="Ajustes de stock" Value="Ajustes de stock" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFormato" runat="server" AssociatedControlID="ddlFormato"
                    CssClass="control-label" Text="Formato (*)"></asp:Label>
                <asp:DropDownList ID="ddlFormato" runat="server" CssClass="form-control">
                    <asp:ListItem Text="PDF" Value="PDF" />
                    <asp:ListItem Text="Excel" Value="Excel" />
                    <asp:ListItem Text="CSV" Value="CSV" />
                </asp:DropDownList>
                <span class="campo-ayuda">PDF para imprimir, Excel para trabajar los números y CSV para otros sistemas.</span>
            </div>
        </div>
    </div>
    <div class="row">
        <div class="col-sm-6">
            <div class="form-group">
                <asp:Label ID="lblDesde" runat="server" AssociatedControlID="txtDesde"
                    CssClass="control-label" Text="Fecha desde (*)"></asp:Label>
                <asp:TextBox ID="txtDesde" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvDesde" runat="server" ControlToValidate="txtDesde"
                    ValidationGroup="Reporte" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la fecha desde."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvDesde" runat="server" ControlToValidate="txtDesde"
                    Type="Date" ValidationGroup="Reporte" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha desde no puede ser posterior a hoy."></asp:RangeValidator>
            </div>
        </div>
        <div class="col-sm-6">
            <div class="form-group">
                <asp:Label ID="lblHasta" runat="server" AssociatedControlID="txtHasta"
                    CssClass="control-label" Text="Fecha hasta (*)"></asp:Label>
                <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvHasta" runat="server" ControlToValidate="txtHasta"
                    ValidationGroup="Reporte" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la fecha hasta."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvHasta" runat="server" ControlToValidate="txtHasta"
                    Type="Date" ValidationGroup="Reporte" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha hasta no puede ser posterior a hoy."></asp:RangeValidator>
                <asp:CompareValidator ID="cvHasta" runat="server" ControlToValidate="txtHasta"
                    ControlToCompare="txtDesde" Operator="GreaterThanEqual" Type="Date"
                    ValidationGroup="Reporte" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha hasta no puede ser anterior a la fecha desde."></asp:CompareValidator>
            </div>
        </div>
    </div>

    <h3 class="seccion-titulo">Qué incluye cada reporte</h3>
    <dl class="lista-reportes">
        <dt>Consumo por insumo</dt>
        <dd>Lo que consumieron las ventas de cada insumo, el promedio diario, el valorizado y el stock actual.</dd>
        <dt>Ventas por receta</dt>
        <dd>Unidades vendidas y cantidad de operaciones de venta de cada receta.</dd>
        <dt>Órdenes por proveedor</dt>
        <dd>Órdenes pendientes, recibidas y anuladas de cada proveedor, con el importe recibido.</dd>
        <dt>Alertas térmicas</dt>
        <dd>Las alertas de temperatura de los sensores en el período.</dd>
        <dt>Ajustes de stock</dt>
        <dd>Los ajustes manuales de stock registrados, con el usuario y el motivo.</dd>
    </dl>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Exportar" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click" ValidationGroup="Reporte" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
