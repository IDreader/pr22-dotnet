using CommunityToolkit.Maui.Views;

namespace ReadDoc;

public partial class DevicesPage : Popup
{
    string? selected = null;

    public DevicesPage()
    {
        InitializeComponent();
    }

    public void SetDeviceList(List<String> devices)
    {
        DevicesListBox.ItemsSource = devices;
        if (devices != null && devices.Count > 0)
        {
            DevicesListBox.SelectedItem = devices[0];
        }
    }

    public string? Selected { get { return selected; } }

    private void cmdOpen_Clicked(object sender, EventArgs e)
    {
        selected = (string?)DevicesListBox.SelectedItem;
        CloseAsync(selected);
    }

    private void cmdCancel_Clicked(object sender, EventArgs e)
    {
        selected = null;
        CloseAsync(selected);
    }
}