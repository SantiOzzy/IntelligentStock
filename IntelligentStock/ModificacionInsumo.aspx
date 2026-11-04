<%@ Page Title="Modificación de insumo" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ModificacionInsumo.aspx.cs" Inherits="ModificacionInsumo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Modificación de insumo</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblInsumo" runat="server" AssociatedControlID="ddlInsumo"
                    CssClass="control-label" Text="Insumo a modificar"></asp:Label>
                <asp:DropDownList ID="ddlInsumo" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlInsumo_SelectedIndexChanged">
                </asp:DropDownList>
                <span class="help-block">El stock no se modifica desde acá: se corrige con un ajuste de stock.</span>
            </div>
            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblDescripcion" runat="server" AssociatedControlID="txtDescripcion"
                    CssClass="control-label" Text="Descripción"></asp:Label>
                <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control"
                    TextMode="MultiLine" Rows="3" MaxLength="500"></asp:TextBox>
                <asp:RegularExpressionValidator ID="revLargoDescripcion" runat="server" ControlToValidate="txtDescripcion"
                    ValidationExpression="^[\s\S]{0,500}$" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="La descripción no puede superar los 500 caracteres."></asp:RegularExpressionValidator>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblUnidad" runat="server" AssociatedControlID="txtUnidad"
                    CssClass="control-label" Text="Unidad de medida (*)"></asp:Label>
                <asp:TextBox ID="txtUnidad" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvUnidad" runat="server" ControlToValidate="txtUnidad"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La unidad de medida es obligatoria."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblStockMinimo" runat="server" AssociatedControlID="txtStockMinimo"
                    CssClass="control-label" Text="Stock mínimo (*)"></asp:Label>
                <asp:TextBox ID="txtStockMinimo" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01" max="99999999.99"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvStockMinimo" runat="server" ControlToValidate="txtStockMinimo"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El stock mínimo es obligatorio."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvStockMinimo" runat="server" ControlToValidate="txtStockMinimo"
                    Type="Double" MinimumValue="0" MaximumValue="99999999.99" CultureInvariantValues="true"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El stock mínimo tiene que estar entre 0 y 99.999.999,99."></asp:RangeValidator>
                <asp:RegularExpressionValidator ID="revDecimalesStockMinimo" runat="server" ControlToValidate="txtStockMinimo"
                    ValidationExpression="^\d+([.,]\d{1,2})?$" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="El stock mínimo admite hasta dos decimales."></asp:RegularExpressionValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblPrecio" runat="server" AssociatedControlID="txtPrecio"
                    CssClass="control-label" Text="Precio unitario ($) (*)"></asp:Label>
                <asp:TextBox ID="txtPrecio" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01" max="99999999.99"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvPrecio" runat="server" ControlToValidate="txtPrecio"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El precio unitario es obligatorio."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvPrecio" runat="server" ControlToValidate="txtPrecio"
                    Type="Double" MinimumValue="0" MaximumValue="99999999.99" CultureInvariantValues="true"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El precio unitario tiene que estar entre 0 y 99.999.999,99."></asp:RangeValidator>
                <asp:RegularExpressionValidator ID="revDecimalesPrecio" runat="server" ControlToValidate="txtPrecio"
                    ValidationExpression="^\d+([.,]\d{1,2})?$" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="El precio unitario admite hasta dos decimales."></asp:RegularExpressionValidator>
                <span class="help-block">Costo de una unidad de medida (por ejemplo, de 1 kg).</span>
            </div>
            <div class="form-group">
                <asp:Label ID="lblVidaUtil" runat="server" AssociatedControlID="txtVidaUtil"
                    CssClass="control-label" Text="Días de vida útil"></asp:Label>
                <asp:TextBox ID="txtVidaUtil" runat="server" CssClass="form-control" TextMode="Number" min="1" step="1" max="36500"></asp:TextBox>
                <asp:RangeValidator ID="rvVidaUtil" runat="server" ControlToValidate="txtVidaUtil"
                    Type="Integer" MinimumValue="1" MaximumValue="36500" ValidationGroup="Modificacion"
                    Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Los días de vida útil tienen que estar entre 1 y 36.500."></asp:RangeValidator>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Guardar cambios" CssClass="btn btn-primary"
        ValidationGroup="Modificacion" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
