using System.Windows;

namespace Lab1
{
    /// <summary>
    /// Logika interakcji dla klasy WindowCircle.xaml
    /// </summary>
    public partial class WindowCircle : Window
    {
        public WindowCircle()
        {
            InitializeComponent();
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
