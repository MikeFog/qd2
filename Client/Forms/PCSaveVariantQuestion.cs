using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class PCSaveVariantQuestion : Form
    {
        public RadioButton OverrideRadioButton => rbClone;

        public PCSaveVariantQuestion()
        {
            InitializeComponent();
        }
    }
}
