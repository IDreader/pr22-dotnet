using CommunityToolkit.Maui.Views;
using System.Collections.Generic;

namespace ReadDoc;

public partial class OptionsPage : Popup
{
    public OptionsPage()
    {
        InitializeComponent();

        OcrTask_List.ItemsSource = new List<Pr22.Processing.FieldSource>() { Pr22.Processing.FieldSource.Mrz, Pr22.Processing.FieldSource.Viz, Pr22.Processing.FieldSource.Barcode };
        CardFile_List.ItemsSource = Enum.GetValues(typeof(Pr22.ECardHandling.FileId));
    }

    public void SetLights(List<Pr22.Imaging.Light> lights)
    {
        Light_List.ItemsSource = lights;
    }

    public List<Pr22.Imaging.Light> SelectedLights
    {
        get
        {
            return Light_List.SelectedItems.Cast<Pr22.Imaging.Light>().ToList();
        }
        set
        {
            Light_List.SelectedItems = value.Cast<object>().ToList();
        }
    }

    public bool SelectedDocumentView
    {
        get
        {
            return Check_IsDocView.IsChecked;
        }
        set
        {
            Check_IsDocView.IsChecked = value;
        }
    }

    public List<Pr22.Processing.FieldSource> SelectedOcrReadings
    {
        get
        {
            return OcrTask_List.SelectedItems.Cast<Pr22.Processing.FieldSource>().ToList();
        }
        set
        {
            OcrTask_List.SelectedItems = value.Cast<object>().ToList();
        }
    }

    public void SetRfidReaders(List<Pr22.ECardReader> readers)
    {
        CardReader_List.ItemsSource = readers;
    }

    public List<Pr22.ECardReader> SelectedRfidReaders
    {
        get
        {
            return CardReader_List.SelectedItems.Cast<Pr22.ECardReader>().ToList();
        }
        set
        {
            CardReader_List.SelectedItems = value.Cast<object>().ToList();
        }
    }

    public List<Pr22.ECardHandling.FileId> SelectedRfidFiles
    {
        get
        {
            return CardFile_List.SelectedItems.Cast<Pr22.ECardHandling.FileId>().ToList();
        }
        set
        {
            CardFile_List.SelectedItems = value.Cast<object>().ToList();
        }
    }

    public Pr22.ECardHandling.AuthLevel SelectedAuthLevel
    {
        get
        {
            if (MinimumAuthLevel.IsChecked)
                return Pr22.ECardHandling.AuthLevel.Min;
            else if (OptimumAuthLevel.IsChecked)
                return Pr22.ECardHandling.AuthLevel.Opt;
            return Pr22.ECardHandling.AuthLevel.Max;
        }
        set
        {
            switch (value)
            {
                case Pr22.ECardHandling.AuthLevel.Min:
                    MinimumAuthLevel.IsChecked = true;
                    break;
                case Pr22.ECardHandling.AuthLevel.Opt:
                    OptimumAuthLevel.IsChecked = true;
                    break;
                case Pr22.ECardHandling.AuthLevel.Max:
                    MaximumAuthLevel.IsChecked = true;
                    break;
            }
        }

    }

    public bool SelectedGdsUpload
    {
        get
        {
            return Check_IsGdsUploadEnabled.IsChecked;
        }
        set
        {
            Check_IsGdsUploadEnabled.IsChecked = value;
        }
    }

    public bool SelectedGdsActionList
    {
        get
        {
            return Check_IsGdsActionListEnabled.IsChecked;
        }
        set
        {
            Check_IsGdsActionListEnabled.IsChecked = value;
        }
    }

    private void cmdOK_Clicked(object sender, EventArgs e)
    {
        CloseAsync(true);
    }

    private void cmdCancel_Clicked(object sender, EventArgs e)
    {
        CloseAsync(false);
    }
}