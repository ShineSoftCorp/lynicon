﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Lynicon.Models" %>
<%@ Import Namespace="Lynicon.Map" %>
<%@ Import Namespace="Lynicon.Collation" %>
<%@ Import Namespace="System.Text.RegularExpressions" %>
<%
    var enumElementReplacer = new Regex("\\{.*\\}");
    //var summaries = Collator.Instance.Get<Summary, object>(iq => iq).ToList();
    //var summaryDict = summaries
    //    .GroupBy(s => s.Type)
    //    .ToDictionary(sg => sg.Key, sg => sg.OrderBy(s => s.Title));
     %>
<div id='lyn-item-selector' <%= ViewData.ContainsKey("popup") ? "style='display:none'" : "" %>>
    <div id="lyn-item-selector-inner">
    <% foreach (var type in ContentTypeHierarchy.AllContentTypes.OrderBy(t => t.Name)) {
           var patts = ContentMap.Instance.GetUrlPatterns(type);
           if (!patts.Any())
               continue;%>
        <h2>
            <% if (ViewData.ContainsKey("UrlPermission") && (bool)ViewData["UrlPermission"]) { %>
                <span class="new-item">+</span>
                <ul class="new-item-url-patterns">
                    <% foreach (string patt in patts) { %>
                    <li><%= enumElementReplacer.Replace(patt
                        .Replace("{?}", "<span class='pathel'>___</span>")
                        .Replace("{*}", "<span class='pathelopt'>___</span>")
                        .Replace("{/}", "<span class='subpath'>___</span>"),
                        "<span class='pathelenum'>___</span>")
                        %>
                        <input type="hidden" class="url-pattern" value="<%= patt %>" />
                    </li>
                    <% } %>
                </ul>
            <% } %>
            <span class="type-name"><%= BaseContent.ContentClassDisplayName(type) %></span><%= Html.Hidden("typeName", type.FullName, new { @class = "type-name" })%>
        </h2>
        <div class="lyn-type-items">

        </div>
    <% } %>
        <input type="hidden" id="lyn-item-selected" />
    </div>
    <div style="clear:both"></div>
</div>
<script type="text/javascript">
    var $shownDD = null;
    var $newEntry = null;
    function addNew($ul, pattern) {
        if ($newEntry) {
            $newEntry.hide('fast', function () {
                $newEntry.remove();
                $newEntry = null;
                addNew($ul, pattern);
            });
            return;
        }
        var addHtml = urlEntryHtml(pattern, 'add-link', 'Add', 'add-url-link');
        $newEntry = $(addHtml).hide();
        $ul.closest('h2').append($newEntry);
        $newEntry.show('fast');
    }

    function urlEntryHtml(pattern, mainClass, action, actionClass) {
        var addHtml = "<div class='" + mainClass + "'><span>" + pattern +
        "</span><a class='" + actionClass + " cmd-link'>" + action + "</a><a class='cancel-link cmd-link'>Cancel</a></div>";
        var addHtmlSplit = addHtml.split('{');
        if (addHtmlSplit.length > 1) {
            for (var i = 1; i < addHtmlSplit.length; i++) {
                var split2 = addHtmlSplit[i].split('}');
                var inpDetails = split2[0];
                if (inpDetails[0] == '*')
                    addHtmlSplit[i] = "</span><input class='pathelopt-input' value='" + inpDetails.substr(1) + "'/><span>";
                else if (inpDetails[0] == '?')
                    addHtmlSplit[i] = "</span><input class='pathel-input' value='" + inpDetails.substr(1) + "'/><span>";
                else if (inpDetails[0] == '/')
                    addHtmlSplit[i] = "</span><input class='subpath-input' value='" + inpDetails.substr(1) + "'/><span>";
                else {
                    addHtmlSplit[i] = "</span><select class='pathel-select'><option>"
                        + inpDetails.split('|').join('</option><option>') + '</option></select><span>';
                    addHtmlSplit[i] = addHtmlSplit[i].replace('<option>??</option>', '<option>&lt;blank&gt;</option>');
                }
                addHtmlSplit[i] += split2[1];
            }
            addHtml = addHtmlSplit.join('');
        }
        return addHtml;
    }

    function readUrlEntry($container) {
        var url = '';
        var error = null;
        $container.children('span, input, select').each(function () {
            if (!$(this).hasClass('subpath-input') && $(this).val().indexOf('/') >= 0)
                error = "Please remove '/' character in '" + $(this).val() + "'";
            if ($(this).hasClass('pathel-input') && !$(this).val())
                error = "Please remove blank entry not allowed in that position";
            url += $(this).val() || $(this).text();
        });
        url = url.replace('<blank>', '');
        while (url.slice(-1) == '/')
            url = url.slice(0, -1);
        if (url.indexOf('//') >= 0)
            error = "Please ensure url does not contain two consecutive '/'s";
        if (error) {
            alert(error);
            return null;
        } else
            return url;
    }



    $('#lyn-item-selector-inner').accordion({
        heightStyle: 'content',
        beforeActivate: function (ev, ui) {
            if (ui.newPanel && ui.newPanel.length) {
                var $panel = ui.newPanel;
                $panel.load('/lynicon/items/getpage?$top=15&$orderby=Title&datatype='
                    + ui.newHeader.find('input.type-name').val());
            }
        }
    });

    $('#lyn-item-selector').on('click', 'a.paging-link', function (ev) {
        var $panel = $(this).closest('.lyn-type-items');
        $panel.load($(this).prop('href'));
        ev.preventDefault();
    }).on('click', 'a.move-link', function (ev) {
        ev.preventDefault();
        var $mover = $(this);
        $.get($mover.prop('href'), function (patt) {
            if (patt.indexOf('{') < 0) {
                alert('This url cannot be changed, no part of it is variable');
                return;
            }
            var $cont = $mover.closest('.lyn-item-entry');
            var $item = $cont.find('.item-link');
            var edHtml = urlEntryHtml(patt, 'edit-link', 'Save', 'save-url-link');
            $item.after($(edHtml));
            $mover.hide();
            $item.closest('.lyn-item-entry').find('.edit-link input:first').focus();
        });
    }).on('click', 'a.save-url-link', function (ev) {
        ev.preventDefault();
        var $entry = $(this).closest('.lyn-item-entry');
        var $editLink = $entry.find('.edit-link');
        var url = readUrlEntry($editLink);
        if (url == null)
            return;
        var id = $entry.find('.item-link').prop('title');
        initAjax();
        $.post('/' + url + '?$urlset=' + id, function (res) {
            if (res == "OK") alert('url has been changed');
            else if (res == "Already Exists") {
                alert("url has not been changed as the new url already exists");
            }
            var datatype = $entry.closest('.lyn-type-items').prev('h2').find('input.type-name').val()
            $.get("<%= Url.Action("GetItem", "Items", new { area = "Lynicon" })%>", { id: id, datatype: datatype }, function (res) {
                $entry.replaceWith($(res));
                endAjax();
            });
        });
    }).on('click', '.edit-link a.cancel-link', function (ev) {
        ev.preventDefault();
        var $entry = $(this).closest('.lyn-item-entry');
        $entry.find('.edit-link').remove();
        $entry.find('.move-link').show();
    }).on('click', '.add-link a.cancel-link', function (ev) {
        $newEntry.hide('fast', function () {
            $newEntry.remove();
            $newEntry = null;
        });
    }).on('click', 'a.del-link', function (ev) {
        ev.preventDefault();
        var $entry = $(this).closest('.lyn-item-entry');
        var url = $entry.find('.item-link').prop('href');
        var resp = confirm("Are you sure you want to delete " + url + " ?");
        if (!resp) return;
        $.post(url + "?$urldelete=true", function (res) {
            if (res == "OK") {
                alert('url deleted');
                $entry.remove();
            }
        });
    }).on('click', '.new-item', function (ev) {
        var $ul = $(this).closest('h2').find('ul');
        if ($ul.find('li').length > 1) {
            if ($shownDD) $shownDD.hide();
            $shownDD = $ul.show();
            ev.stopPropagation();
        } else {
            addNew($(this).closest('h2'), $ul.find('li input').val());
        }
    }).on('click', '.new-item-url-patterns li', function (ev) {
        addNew($(this).closest('h2'), $(this).find('input').val());
    }).on('click', '.add-url-link', function (ev) {
        var $addLink = $(this).closest('.add-link');
        var url = readUrlEntry($addLink);
        if (url == null)
            return;
        $newEntry.hide('fast', function () {
            $newEntry.remove();
            $newEntry = null;
        });
        window.open("/" + url);
    });

    $('body').click(function () {
        if ($shownDD) $shownDD.hide();
    });
</script>
