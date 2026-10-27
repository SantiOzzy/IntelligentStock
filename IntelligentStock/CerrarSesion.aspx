<%@ Page Title="Cerrar sesión" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="CerrarSesion.aspx.cs" Inherits="CerrarSesion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="row">
        <div class="col-md-6 col-md-offset-3">
            <div class="panel panel-default">
                <div class="panel-heading">
                    <h3 class="panel-title">Cerrar sesión</h3>
                </div>
                <div class="panel-body">
                    <p>
                        <asp:Literal ID="litConfirmacion" runat="server"></asp:Literal>
                    </p>
                    <asp:Button ID="btnAceptar" runat="server" Text="Aceptar"
                        CssClass="btn btn-primary" OnClick="btnAceptar_Click" />
                    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar"
                        CssClass="btn btn-default" OnClick="btnCancelar_Click"
                        CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>

</asp:Content>
