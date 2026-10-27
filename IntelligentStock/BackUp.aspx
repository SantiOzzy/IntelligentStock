<%@ Page Title="Copia de seguridad" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BackUp.aspx.cs" Inherits="BackUp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Copia de seguridad de la base de datos</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-8">
            <div class="panel panel-default">
                <div class="panel-body">

                    <div class="form-group">
                        <asp:Label ID="lblCarpeta" runat="server" AssociatedControlID="txtCarpeta"
                            CssClass="control-label" Text="Carpeta de destino (*)"></asp:Label>
                        <asp:TextBox ID="txtCarpeta" runat="server" CssClass="form-control"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvCarpeta" runat="server" ControlToValidate="txtCarpeta"
                            ValidationGroup="BackUp" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="La carpeta de destino es obligatoria."></asp:RequiredFieldValidator>
                        <span class="help-block">
                            La escribe el motor de SQL Server, así que la cuenta del servicio
                            necesita permiso de escritura sobre esa carpeta.
                        </span>
                    </div>

                    <asp:Button ID="btnGenerar" runat="server" Text="Generar copia de seguridad"
                        CssClass="btn btn-primary" ValidationGroup="BackUp" OnClick="btnGenerar_Click" />

                    <asp:Button ID="btnDescargar" runat="server" Text="Descargar el último archivo generado"
                        CssClass="btn btn-default" OnClick="btnDescargar_Click"
                        CausesValidation="false" Visible="false" />

                </div>
            </div>
        </div>
    </div>

</asp:Content>
