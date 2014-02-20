using System.Configuration;

namespace OverviewerGUI
{
    public class OverviewerConfig : ConfigurationElement
    {
        public OverviewerConfig(string mapDir, string outputDir)
        {
            MapDir = mapDir;
            OutputDir = outputDir;
        }

        [ConfigurationProperty("mapDir", IsRequired = true)]
        public string MapDir
        {
            get { return (string)this["mapDir"]; }
            set { this["mapDir"] = value; }
        }

        [ConfigurationProperty("outputDir", IsRequired = true)]
        public string OutputDir
        {
            get { return (string) this["outputDir"]; }
            set { this["outputDir"] = value; }
        }
    }
}
