<%@ Page Title="Restaurar base de datos" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="Restore.aspx.cs" Inherits="Restore" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Restaurar base de datos</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-8">
            <div class="panel panel-default">
                <div class="panel-body">

                    <div class="form-group">
                        <asp:Label ID="lblArchivo" runat="server" CssClass="control-label"
                            Text="Archivo de restauración (.bak o .sql) (*)"></asp:Label>
                        <br />
                        <asp:FileUpload ID="fuArchivo" runat="server" CssClass="form-control" />
                        <span class="help-block">
                            Se admite una copia de seguridad de SQL Server (<code>.bak</code>)
                            o un script de recreación (<code>.sql</code>).
                        </span>
                    </div>

                    <asp:Button ID="btnRestaurar" runat="server" Text="Restaurar"
                        CssClass="btn btn-danger" OnClick="btnRestaurar_Click"
                        OnClientClick="return confirm('La restauración reemplaza los datos actuales y cierra la sesión. ¿Continuar?');" />

                    <asp:Button ID="btnVolver" runat="server" Text="Volver"
                        CssClass="btn btn-default" OnClick="btnVolver_Click" CausesValidation="false" />

                </div>
            </div>
        </div>
    </div>

</asp:Content>
