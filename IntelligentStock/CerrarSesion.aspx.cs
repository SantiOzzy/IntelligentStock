﻿using System;
using System.Web;
using BE;
using BLL;
using Servicios;

// CU-001-002 Cerrar sesion.
// Antes de cerrar se pide confirmacion, tal como esta descripto en el caso de uso.
public partial class CerrarSesion : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    // Cerrar sesión no exige una sesión iniciada: si ya no la hay (venció, o
    // se tocó dos veces Aceptar y el segundo pedido llegó con la sesión ya
    // cerrada), se vuelve al inicio de sesión sin el aviso de sesión vencida.
    protected override bool RequiereSesion
    {
        get { return false; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack)
        {
            return;
        }

        BEUsuario usuario = UsuarioActual;
        if (usuario == null)
        {
            Response.Redirect("~/IniciarSesion.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        litConfirmacion.Text = "¿Confirma que desea cerrar la sesión de <strong>" +
                               HttpUtility.HtmlEncode(usuario.NombreUsuario) + "</strong>?";
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        // El Logout deja el evento en la bitacora y despues limpia la sesion.
        BEUsuario usuario = UsuarioActual;
        if (usuario != null)
        {
            bllUsuario.Logout(usuario);
        }

        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
