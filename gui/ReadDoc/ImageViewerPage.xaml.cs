using CommunityToolkit.Maui.Views;

namespace ReadDoc;

public partial class ImageViewerPage : Popup
{
	public ImageViewerPage()
	{
		InitializeComponent();
	}

    public void SetImage(ImageSource image)
	{
        Popup_Image.Source = image;
    }

    private void cmdClose_Clicked(object sender, EventArgs e)
    {
        Close();
    }    
}