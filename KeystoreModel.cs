public class KeystoreModel
{
    public string FileName { get; set; } = "ExampleKey.keystore";
    public string Alias { get; set; } = "MyKey";
    public string StorePassword { get; set; } = "changeit";
    public string KeyPassword { get; set; } = "changeit";
    public string CommonName { get; set; } = "Example";
    public string OrganizationalUnit { get; set; } = "Dev";
    public string Organization { get; set; } = "Company";
    public string Locality { get; set; } = "City";
    public string State { get; set; } = "State";
    public string Country { get; set; } = "US";
    public int KeySize { get; set; } = 2048;
    public int ValidityDays { get; set; } = 10000;
}
