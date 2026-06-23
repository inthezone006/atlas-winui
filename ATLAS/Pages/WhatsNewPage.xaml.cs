using ATLAS.Models;
using Microsoft.UI.Xaml.Controls;

namespace ATLAS.Pages
{
    public sealed partial class WhatsNewPage : Page
    {
        public WhatsNewPage()
        {
            this.InitializeComponent();
            NotesList.ItemsSource = ReleaseNotesStore.GetNotes();
        }
    }
}