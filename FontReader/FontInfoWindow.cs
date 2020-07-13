using System;
using System.Windows.Forms;
using FontReader.Read;

namespace FontReader
{
    public partial class FontInfoWindow : Form
    {
        private TrueTypeFont _font;

        public FontInfoWindow()
        {
            InitializeComponent();
        }

        public void SetFont(TrueTypeFont fontToShow)
        {
            _font = fontToShow;
            tableList.DataSource = _font.ListTablesKeys();
        }

        private void tableList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: load and show the data in the selected font table.
            var tableData = _font.GetTable(tableList.Text);
            tableProperties.SelectedObject = tableData;
        }
    }
}