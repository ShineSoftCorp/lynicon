﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<div id="container">
<% object list = ViewData["list"]; %>
<%= Html.EditorFor(m => list, null, ViewData["propertyPath"] as string) %>
</div>

