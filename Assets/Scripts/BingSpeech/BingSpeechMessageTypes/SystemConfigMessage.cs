public class SpeechConfigMessage : MessageBase
{
    public class SystemO
    {
        private string version = "2.0.12341";
        public string Version { get { return version; } set { version = value; } }
    }

    public class OsO
    {
        private string platform = "Linux";
        public string Platform { get { return platform; } set { platform = value; } }

        private string name = "Debian";
        public string Name { get { return name; } set { name = value; } }

        private string version = "2.14324324";
        public string Version { get { return version; } set { version = value; } }
    }
    public class DeviceO
    {
        private string manufacturer = "Contoso";
        public string Manufacturer { get { return manufacturer; } set { manufacturer = value; } }

        private string model = "Fabrikan";
        public string Model { get { return model; } set { model = value; } }

        private string version = "7.341";
        public string Version { get { return version; } set { version = value; } }
    }

    private SystemO system =  new SystemO();
    public SystemO System { get { return system; } set { system = value; } }

    private OsO os = new OsO();
    public OsO Os { get { return os; } set { os = value; } }

    DeviceO device = new DeviceO();
    public DeviceO Device { get { return device; } set { device = value; } }
}
