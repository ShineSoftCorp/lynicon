﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<string>" %>
<%@ Import Namespace="Lynicon.Utility" %>
<% if (!ViewData.ModelMetadata.HideSurroundingHtml)
   { %>
<div class='_L24Html'>
<%= string.IsNullOrEmpty(ViewData.TemplateInfo.FormattedModelValue as string)
    ? "&lt;Empty&gt;"
        : ViewData.TemplateInfo.FormattedModelValue %>
</div>
<% } %>
<%= Html.Hidden("", ViewData.TemplateInfo.FormattedModelValue) %>
<%= Html.RegisterHtmlBlock("lyn_rte", "<div id='_L24RTEContainer' style='display:none'><textarea id='_L24RTE'>abcdef</textarea></div>") %>
<%= Html.RegisterScript("lyn_rte_script", @"javascript:$(document).ready(function () {
	$('textarea#_L24RTE').tinymce({
	    // Location of TinyMCE script
	    script_url: '/Areas/Lynicon/scripts/tiny_mce/tiny_mce.js',
	
	    // General options
	    theme: ""advanced"",
        relative_urls: false,
	    plugins: ""pagebreak,style,layer,table,save,advhr,advimage,advlink,emotions,iespell,inlinepopups,insertdatetime,preview,media,searchreplace,print,contextmenu,paste,directionality,fullscreen,noneditable,visualchars,nonbreaking,xhtmlxtras,template,advlist"",
	
	    // Theme options
	    theme_advanced_buttons1: ""save,newdocument,|,bold,italic,underline,strikethrough,|,justifyleft,justifycenter,justifyright,justifyfull,styleselect,formatselect,fontselect,fontsizeselect"",
	    theme_advanced_buttons2: ""cut,copy,paste,pastetext,pasteword,|,search,replace,|,bullist,numlist,|,outdent,indent,blockquote,|,undo,redo,|,link,unlink,anchor,image,cleanup,help,code,|,insertdate,inserttime,preview,|,forecolor,backcolor"",
	    theme_advanced_buttons3: ""tablecontrols,|,hr,removeformat,visualaid,|,sub,sup,|,charmap,emotions,iespell,media,advhr,|,print,|,ltr,rtl,|,fullscreen"",
	    theme_advanced_buttons4: ""insertlayer,moveforward,movebackward,absolute,|,styleprops,|,cite,abbr,acronym,del,ins,attribs,|,visualchars,nonbreaking,template,pagebreak"",
	    theme_advanced_toolbar_location: ""top"",
	    theme_advanced_toolbar_align: ""left"",
	    theme_advanced_statusbar_location: ""bottom"",
	    theme_advanced_resizing: true,
	
	    // Example content CSS (should be your site CSS)
	    content_css: ""/Content/site.css"",
	
	    // Drop lists for link/image/media/template dialogs
	    template_external_list_url: ""lists/template_list.js"",
	    external_link_list_url: ""lists/link_list.js"",
	    external_image_list_url: ""lists/image_list.js"",
	    media_external_list_url: ""lists/media_list.js"",
	
	    // Replace values for the template plugin
	    template_replace_values: {
	        username: ""Some User"",
	        staffid: ""991234""
	    }
	});
});

function showHtml(contents, updateHtml) {
    var $rte = $('#_L24RTEContainer').css('display', 'block').find('#_L24RTE_tbl');
    $(""<div id='modalPlaceholder' style='background-color: #eoeoff;'></div>"")
        .width($rte.width()).height($rte.height())
        .modal({
            overlayClose: true,
            onClose: function(dialog) {
                $('#_L24RTEContainer').css('display', 'none');
                updateHtml($('textarea#_L24RTE').html());
                $.modal.getContainer().unbind('move.modal');
                $.modal.close();
            }
        });

    $('#_L24RTEContainer').css({ 'z-index': '1010', position: 'fixed' });
    var isRTEResizeDrag = false;
    $('#_L24RTE_resize')
        .mousedown(function () { isRTEResizeDrag = true; });
    $('body').mouseup(function () {
        if (isRTEResizeDrag) {
            isRTEResizeDrag = false;
            $('#modalPlaceholder').width($rte.width()).height($rte.height());
            $.modal.update($rte.height(), $rte.width());
        }
    });
    $('.simplemodal-close').css({
        'z-index': '1003', position: 'fixed', display: 'block',
        'background-image': 'url(/lynicon/embedded/Content/Images/close-white.png/)',
        width: '16px', height: '16px'});
    positionTool('#_L24RTEContainer');
    $.modal.getContainer().bind('move.modal', function() { positionTool('#_L24RTEContainer'); });
            
    $('textarea#_L24RTE').html(contents);
}", new List<string> { "tinymce-script" })%>
