<%@ Page Language="C#" AutoEventWireup="true" Inherits="System.Web.UI.Page" %>

<%@ Import Namespace="Sitecore.ContentSearch" %>
<%@ Import Namespace="Sitecore.ContentSearch.Linq" %>
<%@ Import Namespace="Sitecore.ContentSearch.SearchTypes" %>
<%@ Import Namespace="Sitecore.ContentSearch.Security" %>

<script type="text/C#" runat="server">

    protected void Page_Load(object sender, EventArgs e)
    {
        var index = ContentSearchManager.GetIndex("sitecore_master_index");

        using (var context = index.CreateSearchContext())
        {
            var results = context.GetQueryable<SearchResultItem>()
                               .GetResults();

            Response.Write("Hits: " + results.Hits.Count() + "<br />");
        }
    }

</script>
