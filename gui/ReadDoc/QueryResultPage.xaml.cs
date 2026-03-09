using CommunityToolkit.Maui.Views;
using Pr22.Processing;
using Pr22.Util;
namespace ReadDoc;

public partial class QueryResultPage : Popup
{
	public QueryResultPage()
	{
		InitializeComponent();
	}

	public void SetResult(Variant result)
	{
        var fs = new FormattedString();

        for (int ix = 0; ix < result.NItems; ++ix)
        {
            try
            {
                Variant it = result[ix];
                string name = it.Name;
                int status = it.GetChild(((int)VariantId.Checksum), 0).ToInt();
                var span = new Span
                {
                    Text = "Document is " + (it.ToInt() == 1 ? "found" : "not found")
                    + " on the list " + name + " Status: " + ((Status)status).ToString()
                };
                span.TextColor = status == 0 ? Colors.Green : status == 200 ? Colors.Red : Colors.Gray;
                fs.Spans.Add(span);
                fs.Spans.Add(new Span { Text = Environment.NewLine });
            }
            catch (Exception)
            {
                throw;
            }
        }

        QueryResultText.FormattedText = fs;
    }

    private void cmdClose_Clicked(object sender, EventArgs e)
    {
		Close();
    }
}