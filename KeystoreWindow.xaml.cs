using System.Windows;

namespace UE57AndroidManager
{
    public partial class KeystoreWindow : Window
    {
        public KeystoreModel Model { get; } = new KeystoreModel();
        public KeystoreWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Model.FileName = FileNameBox.Text;
            Model.Alias = AliasBox.Text;
            Model.StorePassword = StorePasswordBox.Password;
            Model.KeyPassword = KeyPasswordBox.Password;
            Model.CommonName = CNBox.Text;
            Model.OrganizationalUnit = OUBox.Text;
            Model.Organization = OBox.Text;
            Model.Locality = LBox.Text;
            Model.State = SBox.Text;
            Model.Country = CBox.Text;
            int ks;
            if (int.TryParse(KeySizeBox.Text, out ks)) Model.KeySize = ks;
            int vd;
            if (int.TryParse(ValidityBox.Text, out vd)) Model.ValidityDays = vd;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
