using System;
using System.Collections.Generic;
using System.Linq;
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
            tableList.DataSource = _font.ListTablesKeys().Select(MapTableDescription).ToList();


            //tableProperties.SelectedObject = fontToShow.ReadGlyph('$');
        }

        private string MapTableDescription(string arg)
        {
            switch (arg)
            {
                case "FFTM": return arg+" - FontForge timestamp";
                case "GDEF": return arg + " - Glyph Definitions";
                case "OS/2": return arg + " - Metrics";
                case "cmap": return arg + " - Character to Glyph Index Mapping";
                case "cvt ": return arg + " - Control Values";
                case "fpgm": return arg + " - Font Program (hinting?)";
                case "gasp": return arg + " - Grid-fitting and Scan-conversion Procedures";
                case "glyf": return arg + " - Glyph Data";
                case "head": return arg + " - Font Header";
                case "hhea": return arg + " - Horizontal Header";
                case "hmtx": return arg + " - Horizontal Metrics";
                case "loca": return arg + " - Index to glyph offset";
                case "maxp": return arg + " - Maximum profile (memory use)";
                case "name": return arg + " - Naming table";
                case "post": return arg + " - PostScript printer data";
                case "prep": return arg + " - Control Value program";

                default: return arg;
            }
        }


        private void tableList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: load and show the data in the selected font table.
            var tableData = _font.GetTable(tableList.Text.Substring(0, 4));
            tableProperties.SelectedObject = tableData;
        }
    }
}