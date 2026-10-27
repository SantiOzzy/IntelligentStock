<%@ Page Title="Modificación de receta" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ModificacionReceta.aspx.cs" Inherits="ModificacionReceta" %>
<%@ Register Src="~/Controles/BuscadorInsumo.ascx" TagPrefix="uc" TagName="BuscadorInsumo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Modificación de receta</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblReceta" runat="server" AssociatedControlID="ddlReceta"
                    CssClass="control-label" Text="Receta a modificar"></asp:Label>
                <asp:DropDownList ID="ddlReceta" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlReceta_SelectedIndexChanged">
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre de la receta (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Receta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre de la receta es obligatorio."></asp:RequiredFieldValidator>
            </div>
        </div>
    </div>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Insumos de la receta</strong></div>
        <div class="panel-body">
            <p class="help-block">Elija el insumo en la lista (puede escribir parte del nombre para filtrarla),
                indique cuánto lleva una porción y presione "Agregar a la receta".</p>
            <div class="row">
                <div class="col-md-6">
                    <div class="form-group">
                        <asp:Label ID="lblInsumo" runat="server" CssClass="control-label" Text="Insumo"></asp:Label>
                        <uc:BuscadorInsumo ID="ucBuscadorInsumo" runat="server" />
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-group">
                        <asp:Label ID="lblCantidad" runat="server" AssociatedControlID="txtCantidad"
                            CssClass="control-label" Text="Cantidad por porción"></asp:Label>
                        <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number" min="0.01" step="0.01"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvCantidad" runat="server" ControlToValidate="txtCantidad"
                            ValidationGroup="Detalle" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Ingrese la cantidad del insumo."></asp:RequiredFieldValidator>
                        <span class="help-block">En la unidad de medida del insumo (la que figura entre paréntesis en la lista).</span>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-group">
                        <label class="control-label">&nbsp;</label><br />
                        <asp:Button ID="btnAgregar" runat="server" Text="Agregar a la receta" CssClass="btn btn-default"
                            ValidationGroup="Detalle" OnClick="btnAgregar_Click" />
                    </div>
                </div>
            </div>

            <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
                CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                ShowHeaderWhenEmpty="true" OnRowCommand="gvDetalle_RowCommand"
                EmptyDataText="La receta no tiene insumos.">
                <Columns>
                    <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                    <asp:BoundField DataField="UnidadDeMedida" HeaderText="Unidad" />
                    <asp:BoundField DataField="CantidadTexto" HeaderText="Cantidad por porción" />
                    <asp:TemplateField ItemStyle-Width="80px">
                        <ItemTemplate>
                            <asp:Button ID="btnQuitar" runat="server" Text="Quitar" CssClass="btn btn-xs btn-default"
                                CommandName="Quitar" CommandArgument='<%# Eval("IdInsumo") %>' CausesValidation="false" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Guardar cambios" CssClass="btn btn-primary"
        ValidationGroup="Receta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
