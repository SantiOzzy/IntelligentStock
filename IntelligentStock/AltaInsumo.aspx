<%@ Page Title="Alta de insumo" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaInsumo.aspx.cs" Inherits="AltaInsumo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de insumo</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblDescripcion" runat="server" AssociatedControlID="txtDescripcion"
                    CssClass="control-label" Text="Descripción"></asp:Label>
                <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control"
                    TextMode="MultiLine" Rows="3" MaxLength="500"></asp:TextBox>
            </div>
            <div class="form-group">
                <asp:Label ID="lblUnidad" runat="server" AssociatedControlID="txtUnidad"
                    CssClass="control-label" Text="Unidad de medida (*)"></asp:Label>
                <asp:TextBox ID="txtUnidad" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvUnidad" runat="server" ControlToValidate="txtUnidad"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La unidad de medida es obligatoria."></asp:RequiredFieldValidator>
                <span class="help-block">Por ejemplo kg, g, lt, ml o unidad.</span>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblStock" runat="server" AssociatedControlID="txtStock"
                    CssClass="control-label" Text="Stock inicial (*)"></asp:Label>
                <asp:TextBox ID="txtStock" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvStock" runat="server" ControlToValidate="txtStock"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El stock inicial es obligatorio (puede ser 0)."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblStockMinimo" runat="server" AssociatedControlID="txtStockMinimo"
                    CssClass="control-label" Text="Stock mínimo (*)"></asp:Label>
                <asp:TextBox ID="txtStockMinimo" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvStockMinimo" runat="server" ControlToValidate="txtStockMinimo"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El stock mínimo es obligatorio."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblPrecio" runat="server" AssociatedControlID="txtPrecio"
                    CssClass="control-label" Text="Precio unitario ($) (*)"></asp:Label>
                <asp:TextBox ID="txtPrecio" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvPrecio" runat="server" ControlToValidate="txtPrecio"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El precio unitario es obligatorio."></asp:RequiredFieldValidator>
                <span class="help-block">Costo de una unidad de medida (por ejemplo, de 1 kg).</span>
            </div>
            <div class="form-group">
                <asp:Label ID="lblVidaUtil" runat="server" AssociatedControlID="txtVidaUtil"
                    CssClass="control-label" Text="Días de vida útil"></asp:Label>
                <asp:TextBox ID="txtVidaUtil" runat="server" CssClass="form-control" TextMode="Number" min="1" step="1"></asp:TextBox>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary"
        ValidationGroup="Alta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
