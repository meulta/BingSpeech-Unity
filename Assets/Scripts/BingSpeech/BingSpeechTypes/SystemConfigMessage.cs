
public class SpeechConfigMessage : MessageBase
{
    public class SystemO
    {
        public string Version { get; set; } = "2.0.12341";
    }

    public class OsO
    {
        public string Platform { get; set; } = "Linux";
        public string Name { get; set; } = "Debian";
        public string Version { get; set; } = "2.14324324";
    }

    public class DeviceO
    {
        public string Manufacturer { get; set; } = "Contoso";
        public string Model { get; set; } = "Fabrikan";
        public string Version { get; set; } = "7.341";
    }

    public SystemO System { get; set; } = new SystemO();
    public OsO Os { get; set; } = new OsO();
    public DeviceO Device { get; set; } = new DeviceO();
}
