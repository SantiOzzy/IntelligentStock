<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ViewSwitcher.ascx.cs" Inherits="ViewSwitcher" %>

<div class="view-switcher ui-bar-a">
    <%: CurrentView %>&nbsp;view&nbsp;|&nbsp;<a href="<%: SwitchUrl %>" data-ajax="false"><%: AlternateView %>&nbsp;view</a>
</div>
