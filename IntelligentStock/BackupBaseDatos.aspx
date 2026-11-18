<%@ Page Title="Copia de seguridad" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BackupBaseDatos.aspx.cs" Inherits="BackupBaseDatos" %>

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
                        <div class="input-group">
                            <asp:TextBox ID="txtCarpeta" runat="server" CssClass="form-control" MaxLength="260"></asp:TextBox>
                            <span class="input-group-btn">
                                <asp:Button ID="btnExaminar" runat="server" Text="Examinar…" CssClass="btn btn-default"
                                    OnClick="btnExaminar_Click" CausesValidation="false" />
                            </span>
                        </div>
                        <asp:RequiredFieldValidator ID="rfvCarpeta" runat="server" ControlToValidate="txtCarpeta"
                            ValidationGroup="BackUp" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="La carpeta de destino es obligatoria."></asp:RequiredFieldValidator>
                        <span class="help-block">
                            Escriba la ruta o elíjala con Examinar. El archivo lo escribe SQL Server. Se recomienda dejar la carpeta propuesta:
                            el sistema le da permiso al servicio de SQL Server y después permite descargar la copia.
                        </span>
                    </div>

                    <asp:Panel ID="pnlExplorador" runat="server" Visible="false" CssClass="panel panel-info">
                        <div class="panel-heading">
                            <strong>Elegir la carpeta de destino</strong>
                            <span class="text-muted">(carpetas del equipo donde corre el sistema)</span>
                        </div>
                        <div class="panel-body">
                            <p>
                                <strong>Carpeta actual:</strong>
                                <asp:Label ID="lblRutaExplorador" runat="server"></asp:Label>
                            </p>
                            <asp:Button ID="btnSubirCarpeta" runat="server" Text="↑ Subir un nivel" CssClass="btn btn-default btn-sm"
                                OnClick="btnSubirCarpeta_Click" CausesValidation="false" />
                            <asp:Button ID="btnInicioExplorador" runat="server" Text="Discos y accesos" CssClass="btn btn-default btn-sm"
                                OnClick="btnInicioExplorador_Click" CausesValidation="false" />

                            <div class="list-group" style="max-height: 300px; overflow-y: auto; margin-top: 10px;">
                                <asp:Repeater ID="rptCarpetas" runat="server" OnItemCommand="rptCarpetas_ItemCommand">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkCarpeta" runat="server" CssClass="list-group-item" CommandName="Abrir"
                                            CommandArgument='<%# Eval("Ruta") %>' CausesValidation="false">
                                            <span class="glyphicon glyphicon-folder-close"></span>&nbsp; <%# HttpUtility.HtmlEncode(Eval("Nombre")) %>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                            <asp:Label ID="lblExplorador" runat="server" CssClass="help-block" Visible="false"></asp:Label>

                            <asp:Button ID="btnUsarCarpeta" runat="server" Text="Usar esta carpeta" CssClass="btn btn-primary btn-sm"
                                OnClick="btnUsarCarpeta_Click" CausesValidation="false" />
                            <asp:Button ID="btnCerrarExplorador" runat="server" Text="Cancelar" CssClass="btn btn-default btn-sm"
                                OnClick="btnCerrarExplorador_Click" CausesValidation="false" />
                        </div>
                    </asp:Panel>

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
