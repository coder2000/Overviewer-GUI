using System.Globalization;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace OverviewerGUI
{
    public class ConsoleRedirect : TextWriter
    {
        readonly TextBox _output;

        public ConsoleRedirect(TextBox output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            MethodInvoker action = () => _output.AppendText(value.ToString(CultureInfo.InvariantCulture));
            _output.BeginInvoke(action);
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
